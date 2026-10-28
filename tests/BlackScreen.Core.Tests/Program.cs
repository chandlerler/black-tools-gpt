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
Console.WriteLine($"{passed} tests passed.");
