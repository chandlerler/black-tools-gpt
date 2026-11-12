using System.Drawing.Drawing2D;
using BlackScreen.Core;
namespace BlackScreen;

internal static class PrismRenderer
{
    private static readonly Color[] Rainbow = [Color.FromArgb(242, 55, 64), Color.FromArgb(255, 143, 36), Color.FromArgb(255, 224, 57), Color.FromArgb(65, 193, 114), Color.FromArgb(41, 166, 218), Color.FromArgb(129, 91, 193)];
    public static void Draw(Graphics g, Size size, double seconds)
    {
        g.Clear(Color.Black);
        var f = PrismAnimation.At(seconds);
        if (f.Opacity <= 0 || size.Width <= 0 || size.Height <= 0) return;
        var state = g.Save();
        try
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(size.Width / 1000f, size.Height / 650f);
            g.TranslateTransform((size.Width - 1000 * scale) / 2, (size.Height - 650 * scale) / 2);
            g.ScaleTransform(scale, scale);
            PointF[] triangle = [new(500, 150), new(340, 430), new(660, 430)];
            PointF entry = new(423, 285), exitTop = new(553, 243), exitBottom = new(588, 304);
            int Alpha(float value) => (int)Math.Clamp(255 * value * f.Opacity, 0, 255);
            // Blue-white glass rim and a nearly black core, echoing the supplied prism.
            using (var fill = new LinearGradientBrush(new PointF(400, 150), new PointF(530, 430), Color.FromArgb(Alpha(.23f * f.Prism), 120, 191, 211), Color.FromArgb(Alpha(.035f * f.Prism), 40, 110, 135)))
                g.FillPolygon(fill, triangle);
            for (int width = 14; width >= 2; width -= 2)
            {
                using var rim = new Pen(Color.FromArgb(Alpha(f.Prism * (width == 2 ? .8f : .035f)), 191, 224, 233), width) { LineJoin = LineJoin.Round };
                g.DrawPolygon(rim, triangle);
            }
            if (f.Incoming > 0)
            {
                PointF start = new(-30, 410);
                PointF head = Lerp(start, entry, f.Incoming);
                using var halo = new Pen(Color.FromArgb(Alpha(.14f), Color.White), 7);
                using var beam = new Pen(Color.FromArgb(Alpha(1), Color.White), 2.5f);
                g.DrawLine(halo, start, head); g.DrawLine(beam, start, head);
            }
            if (f.Spectrum > 0)
            {
                using var internalLight = new LinearGradientBrush(entry, exitTop,
                    Color.FromArgb(Alpha(.85f * f.Spectrum), Color.White), Color.FromArgb(Alpha(.24f * f.Spectrum), 190, 224, 235));
                g.FillPolygon(internalLight, [entry, exitTop, exitBottom]);
                for (int i = 0; i < Rainbow.Length; i++)
                {
                    float a = i / 6f, b = (i + 1) / 6f;
                    PointF nearA = Lerp(exitTop, exitBottom, a), nearB = Lerp(exitTop, exitBottom, b);
                    PointF farA = new(1030, 326 + 105 * a), farB = new(1030, 326 + 105 * b);
                    using var band = new SolidBrush(Color.FromArgb(Alpha(.96f), Rainbow[i]));
                    g.FillPolygon(band, [nearA, Lerp(nearA, farA, f.Spectrum), Lerp(nearB, farB, f.Spectrum), nearB]);
                }
            }
        }
        finally { g.Restore(state); }
    }
    private static PointF Lerp(PointF a, PointF b, float t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
}
