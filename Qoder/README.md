# Avalonia Test Recorder

A powerful toolchain for recording user interactions in Avalonia desktop applications and automatically generating executable C# headless tests.

## Overview

This project consists of four main components:

1. **Avalonia.TestRecorder** - NuGet library for recording user interactions in desktop applications
2. **Avalonia.HeadlessTestKit** - NuGet library providing DSL helpers for writing headless tests
3. **SampleApp** - Demo Avalonia desktop application with recorder integration
4. **SampleApp.Tests.Headless** - Reference test project demonstrating generated tests

## Features

- 🎯 **Record user interactions** - Capture clicks, text input, scrolling, and keyboard actions
- 🧪 **Generate test code** - Automatically create C# test files compatible with Avalonia.Headless
- 🔍 **Stable selectors** - Prioritizes AutomationId-based element identification with intelligent fallbacks
- ✅ **Assertion capture** - Record UI state validations with Ctrl+Shift+A
- 📝 **Clean DSL** - Fluent, readable test code using the `Ui` helper class
- ⚡ **Headless execution** - Run tests without visible windows in CI/CD pipelines
- ➖ **Minimizable overlay** - Collapse the recorder overlay to a small button when not needed

## Quick Start

### 1. Recording Tests in Your Application

Add the TestRecorder to your Avalonia desktop app:

```bash
dotnet add package Avalonia.TestRecorder
```

Integrate the recorder in your `App.axaml.cs`:

```csharp
using Avalonia.TestRecorder;

public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
        desktop.MainWindow = mainWindow;

#if DEBUG
        // Attach recorder when AV_RECORDER=1 environment variable is set
        if (Environment.GetEnvironmentVariable("AV_RECORDER") == "1")
        {
            var session = TestRecorder.Attach(mainWindow, new RecorderOptions
            {
                OutputDirectory = "./RecordedTests",
                ScenarioName = "MyScenario"
            });
            session.Start();
        }
#endif
    }
    base.OnFrameworkInitializationCompleted();
}
```

Run your app with recording enabled:

```bash
set AV_RECORDER=1
dotnet run
```

### 2. Recording Workflow

**Overlay Panel:**

When the recorder is attached, a compact overlay panel appears at the top of your window with:
- 🔴 **Status indicator** - Shows current state (Off/Recording/Paused)
- **Step counter** - Number of recorded steps
- ▶️ **Record/Stop button** - Toggle recording
- ⏸️ **Pause button** - Pause/resume recording
- 🗑️ **Clear button** - Reset recorded steps
- 💾 **Save button** - Save test to file with file picker dialog
- ➖ **Minimize button** - Collapse the overlay to a small restore button
- ➕ **Restore button** - Expand the overlay back to full size (appears when minimized)

**Hotkeys:**
- `Ctrl+Shift+R` - Start/Stop recording
- `Ctrl+Shift+P` - Pause/Resume
- `Ctrl+Shift+S` - Save test to file
- `Ctrl+Shift+A` - Capture assertion from element **under mouse cursor** (or focused element as fallback)

**Steps:**
1. Launch your app with recording enabled
2. The overlay panel appears at the top of the window
3. Click the ▶️ button or press `Ctrl+Shift+R` to start recording
4. Perform your test scenario (click buttons, enter text, etc.)
5. **Hover your mouse over the element** you want to assert, then press `Ctrl+Shift+A` to capture its state
6. Click the ➖ button to minimize the overlay when you need more screen space
7. Click the ➕ button to restore the overlay when you need to access controls
8. Click the 💾 button or press `Ctrl+Shift+S` to save - a file picker dialog will appear
9. Choose the location and filename for your test file

### 3. Writing Headless Tests

Add the HeadlessTestKit to your test project:

```bash
dotnet add package Avalonia.HeadlessTestKit
dotnet add package Avalonia.Headless
dotnet add package Avalonia.Headless.XUnit
```

Example test using the generated code:

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

public class LoginTests
{
    [AvaloniaFact]
    public void LoginScenario_ValidCredentials_ShowsSuccess()
    {
        // Arrange
        var window = new MainWindow { DataContext = new MainWindowViewModel() };
        window.Show();
        var ui = new Ui(window);

        // Act
        ui.Click("usernameField");
        ui.TypeText("usernameField", "testuser");
        ui.Click("passwordField");
        ui.TypeText("passwordField", "password123");
        ui.Click("loginButton");

        // Assert
        ui.AssertText("statusLabel", "Login successful");
    }
}
```

Run your tests:

```bash
dotnet test
```

## AutomationId Best Practices

For stable test recording, **always set AutomationId on interactive controls**:

```xml
<TextBox AutomationProperties.AutomationId="usernameField" />
<Button AutomationProperties.AutomationId="loginButton" />
<TextBlock AutomationProperties.AutomationId="statusLabel" />
```

**Naming Convention:**
- Use descriptive, semantic names: `loginButton`, `usernameField`, `statusLabel`
- Be consistent: `fieldName_Type` or `camelCase`
- Avoid generic names: `button1`, `textBox1`

## Test DSL Reference

The `Ui` class provides fluent methods for interacting with your application:

**Interactions:**
- `Click(id)` - Click element
- `RightClick(id)` - Right-click element
- `DoubleClick(id)` - Double-click element
- `TypeText(id, text)` - Enter text into element
- `KeyPress(keyName)` - Press specific key (e.g., "Enter", "Tab")
- `Scroll(id, deltaX, deltaY)` - Scroll element
- `Hover(id)` - Move mouse over element

**Assertions:**
- `AssertText(id, expected)` - Verify text content
- `AssertChecked(id, expected)` - Verify toggle state
- `AssertVisible(id)` - Verify element is visible
- `AssertEnabled(id)` - Verify element is enabled

**Synchronization:**
- `WaitFor(id, condition, timeout)` - Wait for custom condition
- `WaitForText(id, expected, timeout)` - Wait for text value
- `WaitForVisible(id, timeout)` - Wait for element to appear
- `WaitForEnabled(id, timeout)` - Wait for element to be enabled

## Building from Source

```bash
git clone <repository-url>
cd Avalonia.TestRecorder
dotnet build
dotnet test
```

## Project Structure

```
/
├── src/
│   ├── Avalonia.TestRecorder/          # Recorder library
│   └── Avalonia.HeadlessTestKit/       # Test helper DSL
├── samples/
│   ├── SampleApp/                      # Demo application
│   └── SampleApp.Tests.Headless/       # Demo tests
└── docs/                               # Documentation
```

## License

[License information]

## Contributing

Contributions are welcome! Please see CONTRIBUTING.md for guidelines.