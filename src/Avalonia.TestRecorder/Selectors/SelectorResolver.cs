using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.HeadlessTestKit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.TestRecorder.Selectors;

/// <summary>
/// Resolves stable selectors for UI controls.
/// </summary>
internal sealed class SelectorResolver
{
    private readonly SelectorOptions _options;
    private readonly ILogger? _logger;
    private readonly Window? _window;
    private readonly ValidationUi? _validationUi;

    public SelectorResolver(SelectorOptions options, ILogger? logger = null, Window? window = null)
    {
        _options = options;
        _logger = logger;
        _window = window;
        
        // Create validation UI if we have a window
        _validationUi = window != null ? new ValidationUi(window) : null;
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
                // Validate the selector before returning it
                if (ValidateSelector(automationId, controlWithId))
                {
                    _logger?.LogDebug("Resolved and validated selector via AutomationId: {AutomationId}", automationId);
                    return (automationId, SelectorQuality.High, null);
                }
                else
                {
                    _logger?.LogWarning("AutomationId selector failed validation: {AutomationId}", automationId);
                }
            }
        }

        // Priority 2: Name property (if preferred)
        if (_options.PreferName && !string.IsNullOrEmpty(control.Name))
        {
            // Validate the selector before returning it
            if (ValidateSelector(control.Name, control))
            {
                _logger?.LogDebug("Resolved and validated selector via Name: {Name}", control.Name);
                return (control.Name, SelectorQuality.Medium, null);
            }
            else
            {
                _logger?.LogWarning("Name selector failed validation: {Name}", control.Name);
            }
        }

        // Priority 3: Tree path
        if (_options.AllowTreePath)
        {
            var treePath = GenerateTreePath(control);
            // Validate the selector before returning it
            if (ValidateSelector(treePath, control))
            {
                _logger?.LogDebug("Resolved and validated selector via tree path: {TreePath}", treePath);
                return (treePath, SelectorQuality.Low, null);
            }
            else
            {
                _logger?.LogWarning("Tree path selector failed validation: {TreePath}", treePath);
            }
        }

        // Fallback: Use control type + index
        var fallback = $"{control.GetType().Name}_NoId";
        _logger?.LogError("Could not resolve stable selector for control: {Type}", control.GetType().Name);
        return (fallback, SelectorQuality.Low, "ERROR: No stable selector available");
    }
    
    /// <summary>
    /// Validates that a selector can find the correct control.
    /// </summary>
    /// <param name="selector">The selector to validate.</param>
    /// <param name="expectedControl">The control we expect to find.</param>
    /// <returns>True if the selector finds the correct control, false otherwise.</returns>
    private bool ValidateSelector(string selector, Control expectedControl)
    {
        // If we don't have a validation UI, we can't validate
        if (_validationUi == null || _window == null)
            return true; // Assume it's valid if we can't validate
            
        try
        {
            // Try to find the control using the selector
            var foundControl = _validationUi.FindControlPublic(selector);
            
            // Check if it's the same control by comparing their paths in the visual tree
            return AreControlsEquivalent(expectedControl, foundControl);
        }
        catch (ControlNotFoundException)
        {
            // Control not found with this selector
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error validating selector: {Selector}", selector);
            return false;
        }
    }
    
    /// <summary>
    /// Compares two controls to see if they are equivalent.
    /// </summary>
    /// <param name="control1">First control to compare.</param>
    /// <param name="control2">Second control to compare.</param>
    /// <returns>True if controls are equivalent, false otherwise.</returns>
    private bool AreControlsEquivalent(Control control1, Control control2)
    {
        // Check if they are the exact same object
        if (ReferenceEquals(control1, control2))
            return true;
            
        // Check if they have the same type and position in the visual tree
        if (control1.GetType() != control2.GetType())
            return false;
            
        // Compare their positions in the visual tree by getting their path from root
        var path1 = GetControlPath(control1);
        var path2 = GetControlPath(control2);
        
        return path1.Equals(path2, StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Gets a unique path for a control in the visual tree.
    /// </summary>
    /// <param name="control">The control to get the path for.</param>
    /// <returns>A string representing the control's path in the visual tree.</returns>
    private string GetControlPath(Control control)
    {
        var pathParts = new List<string>();
        var current = control as Visual;
        
        while (current != null)
        {
            if (current is Control currentControl)
            {
                // Add type name and index among siblings of the same type
                var parent = current.GetVisualParent();
                if (parent != null)
                {
                    var siblings = parent.GetVisualChildren()
                        .OfType<Control>()
                        .Where(c => c.GetType() == currentControl.GetType())
                        .ToList();
                        
                    var index = siblings.IndexOf(currentControl);
                    pathParts.Add($"{currentControl.GetType().Name}[{index}]");
                }
                else
                {
                    pathParts.Add(currentControl.GetType().Name);
                }
            }
            
            current = current.GetVisualParent();
        }
        
        pathParts.Reverse();
        return string.Join("/", pathParts);
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