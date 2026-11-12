namespace BlackScreen.Core;

public readonly record struct PrismFrame(float Prism, float Incoming, float Spectrum, float Opacity);
public static class PrismAnimation
{
    public const double DurationSeconds = 3;
    public static PrismFrame At(double seconds)
    {
        float Progress(double start, double end) => (float)Math.Clamp((seconds - start) / (end - start), 0, 1);
        return new(Progress(0, .45), Progress(.1, .85), Progress(.85, 1.65), 1 - Progress(1.9, DurationSeconds));
    }
    public static TimeSpan BlackElapsed(TimeSpan elapsed) => elapsed - TimeSpan.FromSeconds(DurationSeconds) is var black && black > TimeSpan.Zero ? black : TimeSpan.Zero;
}
