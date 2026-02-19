using System.Text;

namespace Flowxel.UI.Controls.Editors;

public class HexadecimalEditor : ByteArrayEditor
{
    protected override string FormatValue(byte[] value)
    {
        var sb = new StringBuilder(value.Length * 3);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(value[i].ToString("X2"));
        }
        return sb.ToString();
    }

    protected override bool TryParse(string? text, out byte[] result)
    {
        result = [];
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Strip all whitespace
        var hex = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (!IsHexChar(c)) return false;
            hex.Append(c);
        }

        if (hex.Length == 0 || hex.Length % 2 != 0) return false;

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var hi = HexValue(hex[i * 2]);
            var lo = HexValue(hex[i * 2 + 1]);
            bytes[i] = (byte)((hi << 4) | lo);
        }

        result = bytes;
        return true;
    }

    private static bool IsHexChar(char c) =>
        c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => 0
    };
}
