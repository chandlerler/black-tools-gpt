using System.ComponentModel;
using System.Drawing.Imaging;
using BlackScreen;
using Shortcut = BlackScreen.Core.Shortcut;

namespace BlackScreen.Windows.Tests;

internal static class Program
{
    private static int passed;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        string output = Path.Combine(AppContext.BaseDirectory, "visual-checks");
        Directory.CreateDirectory(output);
        long Brightness(double seconds, string file)
        {
            using var bitmap = new Bitmap(1000, 650);
            using var graphics = Graphics.FromImage(bitmap);
            PrismRenderer.Draw(graphics, bitmap.Size, seconds);
            bitmap.Save(Path.Combine(output, file), ImageFormat.Png);
            long sum = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++) { var c = bitmap.GetPixel(x, y); sum += c.R + c.G + c.B; }
            return sum;
        }
        Check(Brightness(0, "00-start.png") == 0, "animation starts on pure black");
        long beam = Brightness(.7, "01-white-light.png");
        long spectrum = Brightness(1.7, "02-spectrum.png");
        long fading = Brightness(2.7, "03-fading.png");
        Check(beam > 0 && spectrum > beam, "white beam becomes bright rainbow scene");
        Check(fading > 0 && fading < spectrum, "whole scene dims");
        Check(Brightness(3, "04-black.png") == 0, "every pixel black at three seconds");
        using (var portrait = new Bitmap(400, 1000))
        using (var g = Graphics.FromImage(portrait)) { PrismRenderer.Draw(g, portrait.Size, 1.7); Check(portrait.GetPixel(0, 0).ToArgb() == Color.Black.ToArgb(), "portrait display letterboxing stays black"); }
        using var form = new MainForm(() => { });
        Check(!form.Visible && !form.ShowInTaskbar, "startup stays hidden with no taskbar window");
        Check(form.Icon != null && form.Icon.Width > 0, "embedded prism icon loads");
        Shortcut.TryParse("Ctrl + Alt + F23", out var combination);
        using (var registered = new GlobalHotkey(form.Handle, combination))
        {
            bool conflict = false;
            // A second window cannot take another window's active binding.
            using var second = new Form();
            try { using var duplicate = new GlobalHotkey(second.Handle, combination); }
            catch (Win32Exception) { conflict = true; }
            Check(conflict, "hotkey conflicts are reported");
        }
        using (var restored = new GlobalHotkey(form.Handle, combination)) Check(restored.Id > 0, "disposed hotkey can be registered again");
        form.Show(); Application.DoEvents(); form.Close();
        Check(!form.Visible && !form.IsDisposed, "closing settings hides it without exiting");
        form.Show(); Application.DoEvents(); Check(form.Visible, "settings can reopen after close");
        Console.WriteLine($"{passed} Windows integration tests passed. Frames: {output}");
    }
}
