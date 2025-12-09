using Avalonia.Controls;
using Avalonia.HeadlessTestKit;
using Avalonia.TestRecorder.Selectors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.VisualTree;

namespace Avalonia.TestRecorder;

/// <summary>
/// Validates that recorded steps can successfully find and interact with target elements.
/// </summary>
public class StepValidator
{
    private readonly Window _window;
    private readonly ILogger? _logger;
    
    public StepValidator(Window window, ILogger? logger = null)
    {
        _window = window;
        _logger = logger;
    }
    
    /// <summary>
    /// Validates that a recorded step can successfully execute and that the found control matches the original.
    /// </summary>
    /// <param name="step">The step to validate.</param>
    /// <param name="originalControl">The original control that was recorded.</param>
    /// <returns>Validation result with success status and any error messages.</returns>
    public ValidationResult ValidateStep(RecordedStep step, Control? originalControl = null)
    {
        try
        {
            // Create a temporary UI helper to test the step
            var ui = new ValidationUi(_window);
            
            // Try to find the control first to check if it matches the original
            if (originalControl != null)
            {
                var foundControl = ui.FindControlPublic(step.Selector);
                if (!AreControlsEquivalent(originalControl, foundControl))
                {
                    return new ValidationResult(false, $"Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.");
                }
            }
            
            // Try to execute the step based on its type
            switch (step.Type)
            {
                case StepType.Click:
                    ui.ValidateClick(step.Selector);
                    break;
                case StepType.RightClick:
                    ui.ValidateClick(step.Selector); // Same validation as click
                    break;
                case StepType.DoubleClick:
                    ui.ValidateClick(step.Selector); // Same validation as click
                    break;
                case StepType.TypeText:
                    ui.ValidateTypeText(step.Selector);
                    break;
                case StepType.Hover:
                    ui.ValidateClick(step.Selector); // Same validation as click
                    break;
                case StepType.AssertText:
                    ui.AssertText(step.Selector, step.Parameter); 
                    break;
                case StepType.AssertChecked:
                    //ui.AssertChecked(step.Selector, step.Parameter);
                    break;
                case StepType.AssertVisible:
                    //ui.AssertVisible(step.Selector, step.Parameter);
                    break;
                case StepType.AssertEnabled:
                    //ui.AssertEnabled(step.Selector, step.Parameter);
                    break;
                case StepType.KeyPress:
                case StepType.Scroll:
                    // These don't require element finding, so they're always valid
                    return new ValidationResult(true, null);
                default:
                    return new ValidationResult(false, $"Unknown step type: {step.Type}");
            }
            
            return new ValidationResult(true, null);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Step validation failed for selector: {Selector}", step.Selector);
            return new ValidationResult(false, ex.Message);
        }
    }
    
    /// <summary>
    /// Compares two controls to see if they are equivalent.
    /// </summary>
    /// <param name="control1">First control to compare.</param>
    /// <param name="control2">Second control to compare.</param>
    /// <returns>True if controls are equivalent, false otherwise.</returns>
    private bool AreControlsEquivalent(Control? control1, Control? control2)
    {
        if (control1 == null && control2 == null)
            return true;
            
        if (control1 == null || control2 == null)
            return false;
            
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
}

/// <summary>
/// Result of step validation.
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    
    public ValidationResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Specialized UI helper for validation that doesn't actually perform actions.
/// </summary>
internal class ValidationUi : Ui
{
    public ValidationUi(Window window) : base(window)
    {
    }
    
    /// <summary>
    /// Public method to expose the FindControl method for validation purposes.
    /// </summary>
    public Control FindControlPublic(string id)
    {
        return FindControl(id);
    }
    
    /// <summary>
    /// Validates that a click operation can find the target element.
    /// </summary>
    public void ValidateClick(string id)
    {
        // This will throw ControlNotFoundException if element cannot be found
        _ = FindControl(id);
    }
    
    /// <summary>
    /// Validates that a type text operation can find the target element.
    /// </summary>
    public void ValidateTypeText(string id)
    {
        // This will throw ControlNotFoundException if element cannot be found
        _ = FindControl(id);
    }
}