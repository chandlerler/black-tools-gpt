using Shortcut = BlackScreen.Core.Shortcut;
using System.ComponentModel;
using BlackScreen.Core;
namespace BlackScreen;

internal sealed class MainForm : Form
{
    private readonly TextBox code = new() { MaxLength = 32, Width = 420 };
    private readonly NumericUpDown minutes = new() { Minimum = 0, Maximum = 1440, Width = 150 };
    private readonly TextBox hotkeyInput = new() { Width = 420, ReadOnly = true, ShortcutsEnabled = false };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.FromArgb(76, 96, 120), MaximumSize = new Size(420, 0) };
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MuHei", "settings.json");
    private readonly NotifyIcon tray;
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem blackItem;
    private readonly Action exit;
    private AppSettings saved = new();
    private BlackoutSession? session;
    private GlobalHotkey? hotkey;
    private bool exiting;
    private string? loadError;

    public MainForm(Action exit)
    {
        this.exit = exit;
        Text = "幕黑 · 设置";
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("BlackScreen.Prism.ico")!;
        Icon = new Icon(stream);
        ClientSize = new Size(520, 720); MinimumSize = new Size(540, 740);
        StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = false;
        BackColor = Color.FromArgb(245, 247, 251); Font = new Font("Microsoft YaHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(36, 28, 28, 24) };
        Controls.Add(layout);
        void Add(Control c, int after = 12) { c.Margin = new Padding(0, 0, 0, after); layout.Controls.Add(c); }
        Add(new Label { Text = "幕黑", AutoSize = true, Font = new Font(Font.FontFamily, 28, FontStyle.Bold), ForeColor = Color.FromArgb(25, 37, 58) }, 6);
        Add(new Label { Text = "让屏幕安静下来。", AutoSize = true, ForeColor = Color.SlateGray }, 22);
        Add(new Label { Text = "解锁字符", AutoSize = true }, 6); Add(code, 6);
        Add(new Label { Text = "1–32 位英文字母或数字，不区分大小写", AutoSize = true, ForeColor = Color.SlateGray }, 20);
        Add(new Label { Text = "快速息屏快捷键", AutoSize = true }, 6); Add(hotkeyInput, 6);
        Add(new Label { Text = "点击输入框后按组合键，如 Ctrl + Alt + B\n支持 Ctrl / Alt（至少一个）+ Shift（可选）+ 字母、数字或 F1–F24", AutoSize = true, MaximumSize = new Size(420, 0), ForeColor = Color.SlateGray }, 20);
        hotkeyInput.KeyDown += CaptureShortcut;
        Add(new Label { Text = "自动睡眠等待时间（分钟）", AutoSize = true }, 6); Add(minutes, 6);
        Add(new Label { Text = "默认 30 分钟 · 0 表示不自动睡眠\n息屏前播放 3 秒分光动画，纯黑后开始睡眠计时", AutoSize = true, ForeColor = Color.SlateGray }, 20);
        var start = new Button { Text = "保存并立即息屏", Width = 420, Height = 48, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 70, 130), ForeColor = Color.White, Cursor = Cursors.Hand };
        start.FlatAppearance.BorderSize = 0; start.Click += (_, _) => { if (SaveSettings()) ActivateBlackout(); }; Add(start);
        var save = new Button { Text = "保存设置", Width = 420, Height = 36, FlatStyle = FlatStyle.Flat }; save.Click += (_, _) => SaveSettings(); Add(save, 16);
        Add(new Label { Text = "关闭设置窗口后继续驻留托盘，右键图标可息屏或退出。\n紧急解除黑屏：Ctrl + Alt + Shift + F12", AutoSize = true, ForeColor = Color.SlateGray }); Add(status);
        trayMenu = new ContextMenuStrip();
        blackItem = new ToolStripMenuItem("立即息屏", null, (_, _) => ActivateBlackout());
        trayMenu.Items.Add(blackItem);
        trayMenu.Items.Add("设置…", null, (_, _) => ShowSettings());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出幕黑", null, (_, _) => { exiting = true; exit(); });
        tray = new NotifyIcon { Text = "幕黑", Icon = Icon, ContextMenuStrip = trayMenu };
        tray.DoubleClick += (_, _) => ShowSettings();
        FormClosing += (_, e) =>
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
        };
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized) Hide(); };
        try { saved = AppSettings.Load(settingsPath); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        { loadError = "设置无法读取，已使用默认值。请确认并保存设置后启用。"; }
        code.Text = saved.UnlockCode; minutes.Value = saved.SleepMinutes; hotkeyInput.Text = saved.Hotkey;
    }
    public void Start(bool immediate)
    {
        // A hidden top-level window receives WM_HOTKEY even while only the tray is visible.
        _ = Handle;
        try { Shortcut.TryParse(saved.Hotkey, out var shortcut); hotkey = new GlobalHotkey(Handle, shortcut); }
        catch (Win32Exception e) { loadError = e.Message; }
        tray.Visible = true; UpdateTray();
        BeginInvoke((Action)(() =>
        {
            if (loadError != null) { status.Text = loadError; ShowSettings(); }
            else if (immediate) ActivateBlackout();
            else tray.ShowBalloonTip(4000, "幕黑已驻留托盘", $"按 {saved.Hotkey} 息屏，或右键三棱镜图标打开菜单。", ToolTipIcon.Info);
        }));
    }
    private void CaptureShortcut(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab) return;
        e.SuppressKeyPress = true;
        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey) return;
        string name = e.KeyCode is >= Keys.A and <= Keys.Z ? ((char)e.KeyCode).ToString()
            : e.KeyCode is >= Keys.D0 and <= Keys.D9 ? ((char)e.KeyCode).ToString()
            : e.KeyCode.ToString();
        string value = (e.Control ? "Ctrl + " : "") + (e.Alt ? "Alt + " : "") + (e.Shift ? "Shift + " : "") + name;
        if (Shortcut.TryParse(value, out var shortcut)) { hotkeyInput.Text = shortcut.ToString(); status.Text = "点击保存后快捷键生效。"; }
        else status.Text = "请使用 Ctrl 或 Alt 加字母、数字或功能键；紧急退出组合不可用。";
    }
    private bool SaveSettings()
    {
        if (!AppSettings.ValidCode(code.Text) || !Shortcut.TryParse(hotkeyInput.Text, out var shortcut))
        { MessageBox.Show(this, "请检查解锁字符和快捷键格式。", "检查设置"); return false; }
        var settings = new AppSettings(code.Text, (int)minutes.Value, shortcut.ToString());
        GlobalHotkey? replacement = null;
        try
        {
            // Reserve the new combination first. A conflict or save failure keeps the old binding active.
            if (hotkey == null || hotkey.Shortcut != shortcut) replacement = new GlobalHotkey(Handle, shortcut);
            settings.Save(settingsPath);
            if (replacement != null) { hotkey?.Dispose(); hotkey = replacement; replacement = null; }
            saved = settings; status.Text = "设置已保存，快捷键已生效。"; UpdateTray(); return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception)
        { MessageBox.Show(this, e.Message, "保存失败"); return false; }
        finally { replacement?.Dispose(); }
    }
    private void UpdateTray()
    {
        tray.Text = "幕黑 · " + (hotkey == null ? "快捷键未启用" : saved.Hotkey);
        blackItem.Text = "立即息屏" + (hotkey == null ? "" : "（" + saved.Hotkey + "）");
    }
    private void ShowSettings()
    {
        if (session != null) return;
        Show(); WindowState = FormWindowState.Normal; Activate();
    }
    private void ActivateBlackout()
    {
        if (session != null || exiting) return;
        // Tray and global shortcuts use saved settings, not incomplete edits in the settings window.
        session = new BlackoutSession(this, saved, error =>
        {
            session = null; blackItem.Enabled = true; status.Text = "已解除黑屏。";
            if (error != null) { ShowSettings(); MessageBox.Show(this, error, "睡眠未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        });
        try { Hide(); blackItem.Enabled = false; session.Start(); }
        catch (Exception e)
        { session.Dispose(); session = null; blackItem.Enabled = true; ShowSettings(); MessageBox.Show(this, "无法启动黑屏：" + e.Message, "启动失败"); }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312 && hotkey != null && m.WParam == hotkey.Id)
        {
            // Do not activate while the user is editing a shortcut in settings.
            if (Visible && hotkeyInput.ContainsFocus)
            { hotkeyInput.Text = hotkey.Shortcut.ToString(); status.Text = "点击保存后快捷键生效。"; }
            else if (session == null) BeginInvoke((Action)ActivateBlackout);
            return;
        }
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            exiting = true; session?.Dispose(); session = null; hotkey?.Dispose(); hotkey = null;
            tray.Visible = false; tray.Dispose(); trayMenu.Dispose(); Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
