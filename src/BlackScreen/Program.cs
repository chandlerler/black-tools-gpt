namespace BlackScreen;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var singleton = new Mutex(true, @"Local\BlackScreen.MuHei", out bool first);
        if (!first) { MessageBox.Show("幕黑已在运行。", "幕黑"); return; }
        Application.Run(new MainForm(args.Contains("--black")));
    }
}
