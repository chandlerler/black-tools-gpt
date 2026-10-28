namespace BlackScreen.Core;

public sealed class UnlockMatcher
{
    private readonly string code;
    private string buffer = "";
    public UnlockMatcher(string code)
    {
        if (!AppSettings.ValidCode(code)) throw new ArgumentException("解锁字符必须为 1–32 位英文字母或数字。", nameof(code));
        this.code = code.ToLowerInvariant();
    }
    public bool Push(char character)
    {
        buffer += char.ToLowerInvariant(character);
        if (buffer.Length > code.Length) buffer = buffer[^code.Length..];
        return buffer == code;
    }
    public void Reset() => buffer = "";
}
