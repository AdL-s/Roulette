using System;

public static class LobbyCodeUtility
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const int CodeLength = 13; // fixed length, enough to represent any ulong in base-36

    public static string Encode(ulong value)
    {
        char[] buffer = new char[CodeLength];
        for (int i = CodeLength - 1; i >= 0; i--)
        {
            buffer[i] = Alphabet[(int)(value % 36)];
            value /= 36;
        }
        return new string(buffer);
    }

    public static bool TryDecode(string code, out ulong value)
    {
        value = 0;
        if (string.IsNullOrEmpty(code)) return false;

        code = code.Trim().ToUpperInvariant();
        if (code.Length != CodeLength) return false;

        foreach (char c in code)
        {
            int index = Alphabet.IndexOf(c);
            if (index < 0) return false;
            value = value * 36 + (ulong)index;
        }
        return true;
    }
}