namespace Flowxel.UI.Controls.Editors;

public class Base64Editor : ByteArrayEditor
{
    protected override string FormatValue(byte[] value) =>
        Convert.ToBase64String(value);

    protected override bool TryParse(string? text, out byte[] result)
    {
        result = [];
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Strip whitespace before parsing
        var cleaned = string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
        if (cleaned.Length == 0) return false;

        try
        {
            result = Convert.FromBase64String(cleaned);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
