using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BlackScreen.Core;
using Microsoft.Win32;
namespace BlackScreen;

internal sealed class BlackoutSession : IDisposable
{
    private readonly Form owner;
    private readonly Action<string?> finished;
    private readonly UnlockMatcher matcher;
    private readonly SleepDeadline deadline;
    private readonly Stopwatch clock = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 500 };
    private readonly List<Form> covers = [];
    private readonly HashSet<int> pressed = [];
    private readonly Native.KeyboardProc callback;
    private nint hook;
    private bool disposed, ending, cursorHidden;

    public BlackoutSession(Form owner, AppSettings settings, Action<string?> finished)
    {
        this.owner = owner; this.finished = finished;
        matcher = new(settings.UnlockCode); deadline = new(settings.SleepMinutes);
        callback = OnKeyboard;
        timer.Tick += Tick;
    }
    public void Start()
    {
        hook = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
        if (hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法启用键盘监听。");
        // Keep the machine awake until the explicitly configured deadline.
        if (Native.SetThreadExecutionState(0x80000003) == 0) throw new Win32Exception("无法设置电源保持状态。");
        SystemEvents.DisplaySettingsChanged += DisplaysChanged;
        RebuildCovers(); Cursor.Hide(); cursorHidden = true;
        clock.Start(); timer.Start();
    }
    private void RebuildCovers()
    {
        var old = covers.ToArray(); covers.Clear();
        foreach (var screen in Screen.AllScreens)
        {
            var cover = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual,
                Bounds = screen.Bounds, BackColor = Color.Black, TopMost = true, ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None, Text = "幕黑" };
            cover.FormClosing += (_, e) => { if (!disposed) e.Cancel = true; };
            covers.Add(cover); cover.Show();
        }
        foreach (var form in old) form.Dispose();
        covers.FirstOrDefault()?.Activate();
    }
    private void DisplaysChanged(object? sender, EventArgs e)
    {
        if (!disposed && owner.IsHandleCreated) owner.BeginInvoke((Action)(() => { if (!disposed) RebuildCovers(); }));
    }
    private nint OnKeyboard(int code, nint message, nint data)
    {
        if (code < 0 || disposed || ending) return Native.CallNextHookEx(hook, code, message, data);
        int key = Marshal.ReadInt32(data);
        bool down = message == 0x100 || message == 0x104;
        bool up = message == 0x101 || message == 0x105;
        if (up) pressed.Remove(key);
        if (down && pressed.Add(key))
        {
            bool ctrl = pressed.Contains(0xA2) || pressed.Contains(0xA3);
            bool alt = pressed.Contains(0xA4) || pressed.Contains(0xA5);
            bool shift = pressed.Contains(0xA0) || pressed.Contains(0xA1);
            if (key == 0x7B && ctrl && alt && shift) End(null);
            else if (key is >= 0x41 and <= 0x5A or >= 0x30 and <= 0x39)
            { if (matcher.Push((char)key)) End(null); }
            else if (key is >= 0x60 and <= 0x69)
            { if (matcher.Push((char)('0' + key - 0x60))) End(null); }
            else matcher.Reset();
        }
        return 1; // Prevent unlock text and accidental shortcuts reaching other applications.
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (disposed || ending || !deadline.TryFire(clock.Elapsed)) return;
        timer.Stop();
        Native.SetThreadExecutionState(0x80000000);
        // This call returns after resume; overlays remain until the unlock code is entered.
        if (!Native.SetSuspendState(false, false, false))
            End("Windows 未能进入睡眠。请检查系统电源设置及设备是否支持睡眠。错误码：" + Marshal.GetLastWin32Error());
    }
    private void End(string? error)
    {
        if (ending || disposed) return;
        ending = true;
        owner.BeginInvoke((Action)(() => { Dispose(); finished(error); }));
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop(); timer.Dispose(); clock.Stop();
        SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
        if (hook != 0) { Native.UnhookWindowsHookEx(hook); hook = 0; }
        Native.SetThreadExecutionState(0x80000000);
        foreach (var form in covers) form.Dispose();
        covers.Clear();
        if (cursorHidden) { Cursor.Show(); cursorHidden = false; }
    }
}
