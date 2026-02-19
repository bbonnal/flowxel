using System.Globalization;

namespace Flowxel.UI.Controls.Editors;

public class ULongEditor : BaseEditor<ulong>
{
    protected override bool TryParse(string? text, out ulong result)
    {
        return ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    protected override string FormatValue(ulong value)
    {
        if (!string.IsNullOrEmpty(FormatString))
            return value.ToString(FormatString, CultureInfo.InvariantCulture);

        return value.ToString(CultureInfo.InvariantCulture);
    }

    protected override bool IsValidInput(string input)
    {
        foreach (var c in input)
        {
            if (c is >= '0' and <= '9')
                continue;
            return false;
        }
        return true;
    }

    protected override ulong Clamp(ulong value)
    {
        if (Minimum.HasValue && value < Minimum.Value)
            value = Minimum.Value;
        if (Maximum.HasValue && value > Maximum.Value)
            value = Maximum.Value;
        return value;
    }
}
