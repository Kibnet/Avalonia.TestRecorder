using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.TestRecorder;

internal sealed class SelectorResolver
{
    private readonly SelectorOptions _options;

    public SelectorResolver(SelectorOptions options)
    {
        _options = options;
    }

    public TargetSelector Resolve(Control? control, Point? pointerPosition)
    {
        var preferred = FindStableAncestor(control);
        if (preferred is not null)
        {
            return preferred;
        }

        if (_options.IncludeTreePathFallback && control is Visual visual)
        {
            var path = BuildTreePath(visual);
            return new TargetSelector(path, SelectorKind.VisualTreePath, "WARNING: fallback selector (visual tree path)");
        }

        if (_options.IncludeCoordinateFallback && pointerPosition.HasValue)
        {
            var coords = $"{pointerPosition.Value.X.ToString("0.0", CultureInfo.InvariantCulture)},{pointerPosition.Value.Y.ToString("0.0", CultureInfo.InvariantCulture)}";
            return new TargetSelector(coords, SelectorKind.Coordinates, "WARNING: fallback selector (coordinates)");
        }

        return new TargetSelector("unknown", SelectorKind.Coordinates, "WARNING: fallback selector (unknown)");
    }

    private TargetSelector? FindStableAncestor(Control? control)
    {
        Control? bestNamed = null;

        var current = control;
        while (current is not null)
        {
            var automationId = AutomationProperties.GetAutomationId(current);
            if (!string.IsNullOrWhiteSpace(automationId))
            {
                return new TargetSelector(automationId, SelectorKind.AutomationId);
            }

            if (_options.IncludeNameFallback && IsPublicName(current.Name))
            {
                bestNamed ??= current;
            }

            current = current.GetVisualParent() as Control;
        }

        if (bestNamed is not null)
        {
            return new TargetSelector(bestNamed.Name, SelectorKind.Name, "WARNING: fallback selector (Name)");
        }

        return null;
    }

    private static bool IsPublicName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return !name.StartsWith("PART_", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTreePath(Visual visual)
    {
        var segments = new List<string>();
        var current = visual;
        while (current is not null)
        {
            var parent = current.GetVisualParent();
            var index = 0;
            if (parent != null)
            {
                var children = parent.GetVisualChildren().ToList();
                index = children.IndexOf(current);
            }

            segments.Add($"{current.GetType().Name}[{index}]");
            current = parent!;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }
}
