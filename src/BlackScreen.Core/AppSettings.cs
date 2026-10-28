using System.Text.Json;
namespace BlackScreen.Core;

public sealed record AppSettings(string UnlockCode = "zcz", int SleepMinutes = 30)
{
    public static bool ValidCode(string? code) => code is { Length: >= 1 and <= 32 } && code.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');
    public bool IsValid => ValidCode(UnlockCode) && SleepMinutes is >= 0 and <= 1440;
    public static AppSettings Load(string path)
    {
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
        if (value is null || !value.IsValid) throw new InvalidDataException("设置文件内容无效。");
        return value;
    }
    public void Save(string path)
    {
        if (!IsValid) throw new InvalidDataException("设置无效。");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, true);
    }
}

public sealed class SleepDeadline(int minutes)
{
    private bool fired;
    public bool TryFire(TimeSpan elapsed)
    {
        if (minutes <= 0 || fired || elapsed < TimeSpan.FromMinutes(minutes)) return false;
        fired = true;
        return true;
    }
}
