namespace BlackScreen.Core;

public readonly record struct Shortcut(uint Modifiers, uint Key)
{
    public static bool TryParse(string? text, out Shortcut shortcut)
    {
        shortcut = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        uint modifiers = 0;
        foreach (string part in parts[..^1])
        {
            uint flag = part.ToUpperInvariant() switch { "CTRL" => 2, "ALT" => 1, "SHIFT" => 4, _ => 0 };
            if (flag == 0 || (modifiers & flag) != 0) return false;
            modifiers |= flag;
        }
        if ((modifiers & 3) == 0) return false;
        string name = parts[^1].ToUpperInvariant();
        uint key;
        if (name.Length == 1 && name[0] is >= 'A' and <= 'Z' or >= '0' and <= '9') key = name[0];
        else if (name.StartsWith('F') && int.TryParse(name.AsSpan(1), out int f) && f is >= 1 and <= 24) key = (uint)(0x70 + f - 1);
        else return false;
        if (modifiers == 7 && key == 0x7B) return false; // Reserved emergency escape.
        shortcut = new(modifiers, key);
        return true;
    }
    public override string ToString()
    {
        var parts = new List<string>();
        if ((Modifiers & 2) != 0) parts.Add("Ctrl");
        if ((Modifiers & 1) != 0) parts.Add("Alt");
        if ((Modifiers & 4) != 0) parts.Add("Shift");
        parts.Add(Key is >= 0x70 and <= 0x87 ? "F" + (Key - 0x70 + 1) : ((char)Key).ToString());
        return string.Join(" + ", parts);
    }
}
