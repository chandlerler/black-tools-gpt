using BlackScreen.Core;
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
bool Matches(string code, string input) { var m = new UnlockMatcher(code); return input.Any(m.Push); }
Check(Matches("zcz", "zcz"), "exact unlock");
Check(!Matches("zcz", "zcx"), "wrong input stays locked");
Check(Matches("zcz", "xxxxzcz"), "recover after wrong input");
Check(Matches("zcz", "zzcz"), "overlapping prefix");
Check(Matches("zcz", "ZcZ"), "case insensitive");
Check(!Matches("zcz", "zc"), "partial input stays locked");
Check(Matches("123", "a123"), "digits");
var reset = new UnlockMatcher("zcz"); reset.Push('z'); reset.Push('c'); reset.Reset(); Check(!reset.Push('z'), "non-character interrupts sequence");
Check(!AppSettings.ValidCode("") && !AppSettings.ValidCode("中文") && !AppSettings.ValidCode("a b") && !AppSettings.ValidCode(new string('a',33)), "invalid codes rejected");
Check(new AppSettings("a", 0).IsValid && new AppSettings("a", 1440).IsValid && !new AppSettings("a", -1).IsValid && !new AppSettings("a",1441).IsValid, "sleep bounds");
var deadline = new SleepDeadline(30);
Check(!deadline.TryFire(TimeSpan.FromMinutes(29.99)), "no early sleep");
Check(deadline.TryFire(TimeSpan.FromMinutes(30)), "sleep at deadline");
Check(!deadline.TryFire(TimeSpan.FromMinutes(60)), "sleep only once");
Check(!new SleepDeadline(0).TryFire(TimeSpan.FromDays(2)), "disabled auto sleep");
var path = Path.Combine(Path.GetTempPath(), "muhei-test-" + Guid.NewGuid(), "settings.json");
try {
 Check(AppSettings.Load(path) == new AppSettings(), "default settings");
 new AppSettings("abc123",45).Save(path); Check(AppSettings.Load(path) == new AppSettings("abc123",45), "settings round trip");
 new AppSettings("xyz",0).Save(path); Check(AppSettings.Load(path) == new AppSettings("xyz",0), "settings overwrite");
 File.WriteAllText(path,"{\"UnlockCode\":\"\",\"SleepMinutes\":-1}");
 bool rejected=false; try { AppSettings.Load(path); } catch(InvalidDataException) { rejected=true; }
 Check(rejected,"corrupt settings rejected");
} finally { if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!,true); }

Check(Shortcut.TryParse("alt+CTRL+b", out var shortcut) && shortcut.ToString() == "Ctrl + Alt + B", "hotkey normalizes order and case");
Check(Shortcut.TryParse("Ctrl+Shift+F12", out _) && Shortcut.TryParse("Alt+9", out _) && Shortcut.TryParse("Ctrl+F24", out _), "supported shortcut combinations");
foreach (var invalid in new[] { "B", "Shift+B", "Ctrl+Ctrl+B", "Ctrl+", "Win+B", "Ctrl+Alt+Shift+F12", "Alt+F25", "Alt+Space", "Ctrl++B" })
    Check(!Shortcut.TryParse(invalid, out _), "reject shortcut: " + invalid);
Check(!new AppSettings("zcz", 30, "invalid").IsValid, "invalid hotkey rejected by settings");
var legacyPath = Path.Combine(Path.GetTempPath(), "muhei-legacy-" + Guid.NewGuid(), "settings.json");
try
{
    Directory.CreateDirectory(Path.GetDirectoryName(legacyPath)!);
    File.WriteAllText(legacyPath, "{\"UnlockCode\":\"old123\",\"SleepMinutes\":45}");
    var migrated = AppSettings.Load(legacyPath);
    Check(migrated == new AppSettings("old123", 45, "Ctrl + Alt + B"), "v1 settings retain code/time and receive default shortcut");
    var custom = new AppSettings("zcz", 0, "Ctrl + Shift + F8");
    custom.Save(legacyPath);
    Check(AppSettings.Load(legacyPath) == custom, "custom hotkey persists");
}
finally { Directory.Delete(Path.GetDirectoryName(legacyPath)!, true); }
var beginning = PrismAnimation.At(0);
Check(beginning.Incoming == 0 && beginning.Spectrum == 0, "animation starts without rays");
Check(PrismAnimation.At(.5).Incoming > 0 && PrismAnimation.At(.5).Spectrum == 0, "white light arrives before rainbow");
Check(PrismAnimation.At(1.7).Spectrum == 1 && PrismAnimation.At(1.7).Opacity == 1, "rainbow reaches full extension");
Check(PrismAnimation.At(2.5).Opacity is > 0 and < 1, "scene fades during final second");
Check(PrismAnimation.At(3).Opacity == 0 && PrismAnimation.At(100).Opacity == 0, "pure black at three seconds and thereafter");
Check(PrismAnimation.BlackElapsed(TimeSpan.FromSeconds(2)) == TimeSpan.Zero && PrismAnimation.BlackElapsed(TimeSpan.FromSeconds(63)) == TimeSpan.FromMinutes(1), "sleep countdown excludes intro animation");
Console.WriteLine($"{passed} tests passed.");
