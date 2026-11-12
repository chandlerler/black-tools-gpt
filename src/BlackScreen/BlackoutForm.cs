using System.Diagnostics;
using BlackScreen.Core;
namespace BlackScreen;

internal sealed class BlackoutForm : Form
{
    private readonly Stopwatch clock;
    private readonly System.Windows.Forms.Timer frames = new() { Interval = 16 };
    public BlackoutForm(Rectangle bounds, Stopwatch clock)
    {
        this.clock = clock;
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
        Bounds = bounds; BackColor = Color.Black; TopMost = true; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None; Text = "幕黑";
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        frames.Tick += (_, _) =>
        {
            Invalidate();
            if (clock.Elapsed.TotalSeconds >= PrismAnimation.DurationSeconds) frames.Stop();
        };
        if (clock.Elapsed.TotalSeconds < PrismAnimation.DurationSeconds) frames.Start();
    }
    protected override void OnPaint(PaintEventArgs e) => PrismRenderer.Draw(e.Graphics, ClientSize, clock.Elapsed.TotalSeconds);
    protected override void Dispose(bool disposing)
    {
        if (disposing) frames.Dispose();
        base.Dispose(disposing);
    }
}
