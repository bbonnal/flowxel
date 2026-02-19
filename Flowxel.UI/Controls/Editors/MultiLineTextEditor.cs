namespace Flowxel.UI.Controls.Editors;

public class MultiLineTextEditor : BaseEditor
{
    protected override Type StyleKeyOverride => typeof(MultiLineTextEditor);

    public MultiLineTextEditor()
    {
        AcceptsReturn = true;
        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;
    }
}
