namespace BlackScreen;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var singleton = new Mutex(true, @"Local\BlackScreen.MuHei", out bool first);
        if (!first) { MessageBox.Show("幕黑已在运行，请在右下角托盘（或折叠菜单）中右键三棱镜图标。", "幕黑"); return; }
        using var context = new TrayContext(args.Contains("--black"));
        Application.Run(context);
    }
}
internal sealed class TrayContext : ApplicationContext
{
    private readonly MainForm settingsWindow;
    public TrayContext(bool immediate)
    {
        settingsWindow = new MainForm(ExitThread);
        settingsWindow.Start(immediate);
    }
    protected override void ExitThreadCore()
    {
        settingsWindow.Dispose();
        base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !settingsWindow.IsDisposed) settingsWindow.Dispose();
        base.Dispose(disposing);
    }
}
