using BlackScreen.Core;
namespace BlackScreen;
internal sealed class MainForm : Form
{
    private readonly TextBox code = new() { Text = "zcz", MaxLength = 32, Width = 390 };
    private readonly NumericUpDown minutes = new() { Minimum = 0, Maximum = 1440, Value = 30, Width = 150 };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.FromArgb(76, 96, 120), MaximumSize = new Size(420, 0) };
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MuHei", "settings.json");
    private BlackoutSession? session;
    private readonly bool immediate;
    private bool settingsLoaded = true;
    public MainForm(bool immediate)
    {
        this.immediate = immediate;
        Text = "幕黑 · 屏幕休息工具";
        ClientSize = new Size(520, 560); MinimumSize = new Size(540, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Microsoft YaHei UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(36, 28, 28, 24) };
        Controls.Add(layout);
        void Add(Control c, int after = 12) { c.Margin = new Padding(0, 0, 0, after); layout.Controls.Add(c); }
        Add(new Label { Text = "幕黑", AutoSize = true, Font = new Font(Font.FontFamily, 28, FontStyle.Bold), ForeColor = Color.FromArgb(25, 37, 58) }, 6);
        Add(new Label { Text = "让屏幕安静下来。", AutoSize = true, ForeColor = Color.SlateGray }, 28);
        Add(new Label { Text = "解锁字符", AutoSize = true }, 6); Add(code, 6);
        Add(new Label { Text = "1–32 位英文字母或数字，不区分大小写", AutoSize = true, ForeColor = Color.SlateGray }, 22);
        Add(new Label { Text = "自动睡眠等待时间（分钟）", AutoSize = true }, 6); Add(minutes, 6);
        Add(new Label { Text = "默认 30 分钟 · 设为 0 则保持黑屏，不自动睡眠", AutoSize = true, ForeColor = Color.SlateGray }, 22);
        var start = new Button { Text = "立即黑屏", Width = 420, Height = 50, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 70, 130), ForeColor = Color.White, Cursor = Cursors.Hand };
        start.FlatAppearance.BorderSize = 0; start.Click += (_, _) => ActivateBlackout(); Add(start);
        var save = new Button { Text = "保存设置", Width = 420, Height = 36, FlatStyle = FlatStyle.Flat }; save.Click += (_, _) => SaveSettings(); Add(save, 16);
        Add(new Label { Text = "黑屏时直接连续输入解锁字符即可恢复。\n紧急退出：Ctrl + Alt + Shift + F12\n此工具模拟息屏，不提供 Windows 锁屏保护。", AutoSize = true, ForeColor = Color.SlateGray }, 12);
        Add(status);
        Shown += (_, _) => { LoadSettings(); if (this.immediate && settingsLoaded) ActivateBlackout(); };
        FormClosing += (_, _) => { session?.Dispose(); session = null; };
    }
    private void LoadSettings()
    {
        try { var s = AppSettings.Load(settingsPath); code.Text = s.UnlockCode; minutes.Value = s.SleepMinutes; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        { settingsLoaded = false; status.Text = "设置无法读取，已使用默认值。请确认设置后启动。"; }
    }
    private AppSettings? SaveSettings()
    {
        var settings = new AppSettings(code.Text, (int)minutes.Value);
        if (!settings.IsValid) { MessageBox.Show(this, "解锁字符请输入 1–32 位英文字母或数字。", "检查设置"); return null; }
        try { settings.Save(settingsPath); status.Text = "设置已保存。"; return settings; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { MessageBox.Show(this, "无法保存设置：" + e.Message, "保存失败"); return null; }
    }
    private void ActivateBlackout()
    {
        if (session != null) return;
        var settings = SaveSettings(); if (settings == null) return;
        session = new BlackoutSession(this, settings, error =>
        {
            session = null; Show(); WindowState = FormWindowState.Normal; Activate();
            status.Text = "已解除黑屏。";
            if (error != null) MessageBox.Show(this, error, "睡眠未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
        try { Hide(); session.Start(); }
        catch (Exception e)
        { session.Dispose(); session = null; Show(); MessageBox.Show(this, "无法启动黑屏：" + e.Message, "启动失败"); }
    }
}
