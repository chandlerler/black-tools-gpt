using Shortcut = BlackScreen.Core.Shortcut;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BlackScreen.Core;
namespace BlackScreen;

internal sealed class GlobalHotkey : IDisposable
{
    private static int nextId;
    private readonly nint window;
    public int Id { get; }
    public Shortcut Shortcut { get; }
    private bool disposed;
    public GlobalHotkey(nint window, Shortcut shortcut)
    {
        this.window = window;
        Shortcut = shortcut;
        Id = ++nextId;
        if (!Native.RegisterHotKey(window, Id, shortcut.Modifiers | 0x4000, shortcut.Key))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "快捷键被其他程序占用或无法注册，请换一个组合。");
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Native.UnregisterHotKey(window, Id);
    }
}
