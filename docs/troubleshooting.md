# Troubleshooting Guide

## Common Issues and Solutions

### "Call from invalid thread" Exception

**Symptom:**
When running the SampleApp with recording enabled and typing text, you get an exception:
```
Call from invalid thread
at AutomationProperties.GetAutomationId(control)
```

**Cause:**
Avalonia UI properties (including `AutomationProperties.GetAutomationId`) must be accessed from the UI thread. The recorder's event handlers may be called from different threads.

**Solution:**
The `SelectorResolver.Resolve()` method now automatically dispatches to the UI thread when needed:

```csharp
public (string Selector, SelectorQuality Quality, string? Warning) Resolve(Control control)
{
    // Ensure we're on the UI thread for all property access
    if (!Dispatcher.UIThread.CheckAccess())
    {
        return Dispatcher.UIThread.Invoke(() => ResolveCore(control));
    }
    return ResolveCore(control);
}
```

This fix is already included in the current version of `Avalonia.TestRecorder`.

---

### Recorder Captures Inner Template Elements Instead of Control

**Symptom:**
When clicking on a TextBox with AutomationId set, the recorder captures the inner TextBlock from the control template with a long tree path fallback selector instead of using the TextBox's AutomationId.

**Cause:**
Events bubble up from inner template elements (like the TextBlock inside a TextBox template). The event source is the inner element, not the control you clicked on.

**Solution:**
The `SelectorResolver` now walks up the visual tree to find the nearest parent control with an AutomationId:

```csharp
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
                return ctrl;  // Found parent with AutomationId!
            }
        }
        
        current = current.GetVisualParent();
        if (current is Window) break;
    }
    
    return null;
}
```

This ensures that even when clicking on inner template parts, the recorder will find and use the parent control's AutomationId.

**Example:**
```xml
<TextBox AutomationProperties.AutomationId="usernameField">
    <!-- Inner template contains TextBlock, ScrollViewer, etc. -->
    <!-- Events from these elements now resolve to "usernameField" -->
</TextBox>
```

This fix is already included in the current version.

---

### Tests Fail with "Control not found"

**Symptom:**
Headless tests fail with `ControlNotFoundException` even though the control exists.

**Possible Causes:**
1. Missing `AutomationId` on the control
2. Control not yet rendered/visible
3. Control in a different window or popup

**Solutions:**

1. **Verify AutomationId is set:**
```xml
<!-- WRONG -->
<Button Content="Click Me" />

<!-- CORRECT -->
<Button AutomationProperties.AutomationId="myButton" Content="Click Me" />
```

2. **Wait for control to be ready:**
```csharp
ui.WaitForVisible("myControl", timeout: TimeSpan.FromSeconds(5));
ui.Click("myControl");
```

3. **Check available AutomationIds:**
The exception message includes all available AutomationIds in the window. Review this list to find the correct ID.

---

### Generated Test Doesn't Execute Button Command

**Symptom:**
Clicking a button in tests doesn't trigger its Command.

**Cause:**
In headless mode, pointer events may not fully propagate to invoke button commands.

**Solution:**
The `Ui.Click()` method now includes special handling for buttons:

```csharp
if (control is Button button)
{
    // Directly invoke the OnClick method
    button.Focus();
    ProcessUiEvents();
    var clickMethod = typeof(Button).GetMethod("OnClick", 
        BindingFlags.Instance | BindingFlags.NonPublic);
    clickMethod?.Invoke(button, null);
    ProcessUiEvents();
}
```

This ensures commands are properly executed.

---

### Recording Doesn't Start

**Symptom:**
Setting `AV_RECORDER=1` or passing `--record-tests` doesn't enable recording.

**Checklist:**
1. Verify environment variable is set before launching app:
   ```powershell
   $env:AV_RECORDER="1"
   dotnet run --project samples/SampleApp
   ```

2. Check if `#if DEBUG` wrapper is preventing activation in Release builds

3. Verify recorder attachment code is present in `App.axaml.cs`

4. Check console output for recorder initialization messages

---

### Hotkeys Don't Work

**Symptom:**
Pressing Ctrl+Shift+R or other hotkeys has no effect.

**Possible Causes:**
1. Another control is handling the key event first
2. Hotkey conflicts with system or application shortcuts
3. Focus is not on the recorder-attached window

**Solutions:**
1. Ensure hotkeys are configured correctly:
```csharp
var options = new RecorderOptions
{
    Hotkeys = new RecorderHotkeys
    {
        StartStop = "Ctrl+Shift+R",
        Save = "Ctrl+Shift+S"
    }
};
```

2. Try alternative hotkey combinations if conflicts exist

3. Click on the main window to ensure it has focus

---

### Tests Are Flaky or Timing Out

**Symptom:**
Tests sometimes pass, sometimes fail with timeouts or assertion errors.

**Solutions:**

1. **Use explicit waits:**
```csharp
// BAD - assumes instant update
ui.Click("submitButton");
ui.AssertText("status", "Success");

// GOOD - waits for async operation
ui.Click("submitButton");
ui.WaitForText("status", "Success", TimeSpan.FromSeconds(3));
```

2. **Increase settle delay for slow animations:**
```csharp
var ui = new Ui(window) 
{
    SettleDelay = TimeSpan.FromMilliseconds(50) // Default is 10ms
};
```

3. **Wait for control to be ready:**
```csharp
ui.WaitForEnabled("submitButton");
ui.Click("submitButton");
```

---

### Selector Warnings in Generated Code

**Symptom:**
Generated tests include comments like:
```csharp
ui.Click("Button[0]"); // WARNING: tree path selector - high risk of breakage
```

**Cause:**
Control is missing `AutomationId`, so recorder fell back to tree path.

**Solution:**
Add `AutomationId` to the control:
```xml
<Button AutomationProperties.AutomationId="submitButton" />
```

Then re-record the test to get stable selectors.

---

## Getting Help

If you encounter issues not covered here:

1. Check the [AutomationId Conventions](conventions-automationid.md) guide
2. Review the [README](../README.md) for setup instructions
3. Examine the SampleApp implementation for working examples
4. Enable detailed logging to diagnose issues:
   ```csharp
   var options = new RecorderOptions
   {
       Logger = LoggerFactory.Create(builder => 
           builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
           .CreateLogger<RecorderSession>()
   };
   ```

---

## Best Practices to Avoid Issues

✅ **Always set AutomationId on interactive controls**
✅ **Use WaitFor methods for async operations**
✅ **Test on clean state (fresh window/app instance)**
✅ **Keep tests focused on single scenarios**
✅ **Use descriptive AutomationId names**
✅ **Run tests in isolation (don't share state)**

❌ **Don't rely on timing/delays without explicit waits**
❌ **Don't use tree paths or coordinates for selectors**
❌ **Don't record tests with temporary UI states**
❌ **Don't share test data between tests**
