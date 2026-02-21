using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Shape = Flowxel.Core.Geometry.Shapes.Shape;
using Vector = Flowxel.Core.Geometry.Primitives.Vector;

namespace Flowxel.UI.Controls.Drawing;

public partial class DrawingCanvasControl
{
    private sealed class ContextMenuInteractionCoordinator
    {
        private readonly DrawingCanvasControl _canvas;
        private readonly DrawingCanvasContextMenu _menu;
        private bool _openOnRightRelease;

        public ContextMenuInteractionCoordinator(DrawingCanvasControl canvas, DrawingCanvasContextMenu menu)
        {
            _canvas = canvas;
            _menu = menu;
        }

        public Shape? TargetShape { get; private set; }

        public void Attach()
            => _canvas.ContextRequested += OnContextRequested;

        public void Detach()
        {
            _canvas.ContextRequested -= OnContextRequested;
            _openOnRightRelease = false;
            TargetShape = null;
        }

        public void ClearTarget()
            => TargetShape = null;

        public void OnRightPressed(Vector world)
        {
            SetTarget(_canvas.FindHitShape(world));
            _openOnRightRelease = true;
        }

        public void OnRightReleased()
        {
            if (!_openOnRightRelease)
                return;

            _openOnRightRelease = false;
            Dispatcher.UIThread.Post(() => _menu.Open(_canvas), DispatcherPriority.Background);
        }

        private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (!e.TryGetPosition(_canvas, out var position))
                position = _canvas.CursorAvaloniaPosition;

            var world = _canvas.ScreenToWorld(position);
            SetTarget(_canvas.FindHitShape(world));
            _canvas.InvalidateScene();
        }

        private void SetTarget(Shape? shape)
        {
            TargetShape = shape;
            if (shape is not null)
            {
                _canvas._selectedShape = shape;
                _canvas._hoveredShape = shape;
            }

            ConfigureMenu(shape);
        }

        private void ConfigureMenu(Shape? shape)
        {
            if (shape is null)
            {
                _menu.ConfigureForCanvas();
                return;
            }

            if (_canvas.IsComputedShape(shape))
                _menu.ConfigureForComputedShape();
            else
                _menu.ConfigureForShape();
        }
    }
}
