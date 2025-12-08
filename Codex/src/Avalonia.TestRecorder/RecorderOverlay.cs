using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.TestRecorder;

internal sealed class RecorderOverlay
{
    private readonly Control _overlayControl;
    private readonly TextBlock _statusBlock;
    private readonly TextBlock _stepsBlock;
    private readonly TextBlock _pathBlock;

    private RecorderOverlay(Control overlayControl, TextBlock status, TextBlock steps, TextBlock path)
    {
        _overlayControl = overlayControl;
        _statusBlock = status;
        _stepsBlock = steps;
        _pathBlock = path;
    }

    public static RecorderOverlay? TryAttach(Window window)
    {
        if (window.Content is not Control content)
        {
            return null;
        }

        Grid hostGrid;
        if (window.Content is Grid grid)
        {
            hostGrid = grid;
        }
        else
        {
            hostGrid = new Grid();
            window.Content = null;
            hostGrid.Children.Add(content);
            window.Content = hostGrid;
        }

        var status = new TextBlock { FontWeight = FontWeight.Bold };
        var steps = new TextBlock();
        var path = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxWidth = 320, FontSize = 11 };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Children = { status, steps, path }
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 18, 18, 18)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            Child = stack
        };

        hostGrid.Children.Add(border);

        return new RecorderOverlay(border, status, steps, path);
    }

    public void UpdateState(RecorderState state, int steps, string? outputPath)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statusBlock.Text = $"Recorder: {state}";
            _stepsBlock.Text = $"Steps: {steps}";
            _pathBlock.Text = outputPath is null ? string.Empty : $"Output: {outputPath}";
        });
    }

    public void UpdateSavedPath(string path)
    {
        Dispatcher.UIThread.Post(() => _pathBlock.Text = $"Saved: {path}");
    }

    public void Detach()
    {
        if (_overlayControl.Parent is Panel panel)
        {
            panel.Children.Remove(_overlayControl);
        }
    }
}
