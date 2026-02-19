using System.Globalization;
using Avalonia;

namespace Flowxel.UI.Controls.Editors;

public class DoubleEditor : BaseEditor<double>
{
    public static readonly StyledProperty<int> DigitsProperty =
        AvaloniaProperty.Register<DoubleEditor, int>(nameof(Digits), defaultValue: -1);

    public int Digits
    {
        get => GetValue(DigitsProperty);
        set => SetValue(DigitsProperty, value);
    }

    protected override bool TryParse(string? text, out double result)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }

    protected override string FormatValue(double value)
    {
        if (!string.IsNullOrEmpty(FormatString))
            return value.ToString(FormatString, CultureInfo.InvariantCulture);

        if (Digits >= 0)
            return value.ToString($"F{Digits}", CultureInfo.InvariantCulture);

        return value.ToString(CultureInfo.InvariantCulture);
    }

    protected override bool IsValidInput(string input)
    {
        foreach (var c in input)
        {
            if (c is >= '0' and <= '9' or '.' or '-')
                continue;
            return false;
        }
        return true;
    }

    protected override double Clamp(double value)
    {
        if (Minimum.HasValue && value < Minimum.Value)
            value = Minimum.Value;
        if (Maximum.HasValue && value > Maximum.Value)
            value = Maximum.Value;
        return value;
    }
}
