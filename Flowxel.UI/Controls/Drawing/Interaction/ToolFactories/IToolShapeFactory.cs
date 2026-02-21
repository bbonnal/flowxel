using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal interface IToolShapeFactory
{
    DrawingTool Tool { get; }
    Shape? Build(Vector start, Vector end, double minShapeSize);
}
