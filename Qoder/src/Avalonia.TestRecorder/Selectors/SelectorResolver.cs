using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;

namespace Avalonia.TestRecorder.Selectors;

/// <summary>
/// Resolves stable selectors for UI controls.
/// </summary>
internal sealed class SelectorResolver
{
    private readonly SelectorOptions _options;
    private readonly ILogger? _logger;

    public SelectorResolver(SelectorOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a selector for the given control.
    /// Must be called on the UI thread or will be dispatched automatically.
    /// </summary>
    public (string Selector, SelectorQuality Quality, string? Warning) Resolve(Control control)
    {
        // Ensure we're on the UI thread for all property access
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => ResolveCore(control));
        }

        return ResolveCore(control);
    }

    private (string Selector, SelectorQuality Quality, string? Warning) ResolveCore(Control control)
    {
        // Try to find the nearest parent with AutomationId first
        // This handles cases where inner template elements receive events
        var controlWithId = FindControlWithAutomationId(control);
        if (controlWithId != null)
        {
            var automationId = AutomationProperties.GetAutomationId(controlWithId);
            if (!string.IsNullOrEmpty(automationId))
            {
                _logger?.LogDebug("Resolved selector via AutomationId: {AutomationId}", automationId);
                return (automationId, SelectorQuality.High, null);
            }
        }

        // Priority 2: Name property (if preferred)
        if (_options.PreferName && !string.IsNullOrEmpty(control.Name))
        {
            _logger?.LogWarning("Resolved selector via Name fallback: {Name}", control.Name);
            return (control.Name, SelectorQuality.Medium, 
                _options.WarnOnFallback ? "WARNING: using Name fallback" : null);
        }

        // Priority 3: Tree path
        if (_options.AllowTreePath)
        {
            var treePath = GenerateTreePath(control);
            _logger?.LogWarning("Resolved selector via tree path: {TreePath}", treePath);
            return (treePath, SelectorQuality.Low, 
                _options.WarnOnFallback ? "CRITICAL: tree path selector - high risk of breakage" : null);
        }

        // Fallback: Use control type + index
        var fallback = $"{control.GetType().Name}_NoId";
        _logger?.LogError("Could not resolve stable selector for control: {Type}", control.GetType().Name);
        return (fallback, SelectorQuality.Low, "ERROR: No stable selector available");
    }

    /// <summary>
    /// Walks up the visual tree to find the nearest control with AutomationId.
    /// This handles cases where events bubble up from inner template elements.
    /// </summary>
    private Control? FindControlWithAutomationId(Control startControl)
    {
        var current = startControl as Visual;
        
        while (current != null)
        {
            if (current is Control ctrl)
            {
                var automationId = AutomationProperties.GetAutomationId(ctrl);
                if (!string.IsNullOrEmpty(automationId))
                {
                    return ctrl;
                }
            }
            
            current = current.GetVisualParent();
            
            // Stop at Window boundary
            if (current is Window)
                break;
        }
        
        return null;
    }

    private string GenerateTreePath(Control control)
    {
        var path = new List<string>();
        var current = control as Visual;

        while (current != null)
        {
            if (current is Control ctrl)
            {
                var typeName = ctrl.GetType().Name;
                var index = GetSiblingIndex(ctrl);
                path.Insert(0, $"{typeName}[{index}]");
            }

            current = current.GetVisualParent();
            if (current is Window)
                break;
        }

        return string.Join("/", path);
    }

    private int GetSiblingIndex(Control control)
    {
        var parent = control.GetVisualParent();
        if (parent == null)
            return 0;

        var siblings = parent.GetVisualChildren()
            .OfType<Control>()
            .Where(c => c.GetType() == control.GetType())
            .ToList();

        return siblings.IndexOf(control);
    }
}
