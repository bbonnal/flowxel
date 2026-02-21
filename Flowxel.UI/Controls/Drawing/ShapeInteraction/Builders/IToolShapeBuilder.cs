using Flowxel.Core.Geometry.Primitives;
using Flowxel.Core.Geometry.Shapes;

namespace Flowxel.UI.Controls.Drawing;

internal interface IToolShapeBuilder
{
    DrawingTool Tool { get; }
    Shape? Build(Vector start, Vector end, double minShapeSize);
}
