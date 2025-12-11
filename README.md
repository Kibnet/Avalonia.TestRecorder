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
- 🎨 **Theme support** - Overlay panel adapts to light/dark themes
- 📊 **Real-time feedback** - Status bar shows generated code and validation results

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

When the recorder is attached, a compact overlay panel appears at the top-right of your window with:
- 🟥 **Record/Pause button** - Unified button to start/stop recording (red when recording, green when paused)
- **Step counter** - Number of recorded steps
- 🗑️ **Clear button** - Reset recorded steps
- 💾 **Save button** - Save test to file (prompts for file path via dialog)
- **Status bar** - Shows real-time code preview and validation feedback

**Hotkeys:**
- `Ctrl+Shift+R` - Start/Stop/Pause recording (unified action)
- `Ctrl+Shift+S` - Save test to file (prompts for file path)
- `Ctrl+Shift+A` - Capture assertion from element **under mouse cursor** (or focused element as fallback)
- `Ctrl+Shift+T` - Capture text content from element under cursor

**Steps:**
1. Launch your app with recording enabled
2. The overlay panel appears at the top of the window
3. Perform your test scenario (click buttons, enter text, etc.)
4. **Hover your mouse over the element** you want to assert, then press `Ctrl+Shift+A` to capture its state
5. Press `Ctrl+Shift+R` to pause recording if needed
6. Click the 💾 button or press `Ctrl+Shift+S` to save - enter the full file path in the prompt dialog
7. The status bar shows real-time code preview and validation results for each step

### 3. Setting Up a Test Project

Create a new test project for your Avalonia application. For detailed step-by-step instructions, see the [Test Project Setup Guide](./docs/test-project-setup.md).

**Quick setup:**

```bash
dotnet new xunit -n YourApp.Tests.Headless
cd YourApp.Tests.Headless
```

Add required packages:

```bash
dotnet add package Avalonia.Headless.XUnit
dotnet add package Avalonia.HeadlessTestKit
```

Add reference to your application:

```bash
dotnet add reference ../YourApp/YourApp.csproj
```

Your `.csproj` should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia.Headless.XUnit" Version="11.3.6" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia.HeadlessTestKit" Version="1.0.0" />
    <ProjectReference Include="..\YourApp\YourApp.csproj" />
  </ItemGroup>
</Project>
```

Create `AvaloniaTestApplication.cs` to configure headless mode:

```csharp
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(YourApp.Tests.Headless.TestAppBuilder))]

namespace YourApp.Tests.Headless;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<YourApp.App>()
        .UsePlatformDetect()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        })
        .LogToTrace();
}
```

**Important Notes:**
- Replace `YourApp` with your actual application namespace
- The `[assembly: AvaloniaTestApplication]` attribute is required for xUnit discovery
- `UseHeadlessDrawing = false` is recommended for better test stability
- Tests will run without visible windows - perfect for CI/CD

### 4. Writing Headless Tests

Add the HeadlessTestKit to your test project (if not already added):

```bash
dotnet add package Avalonia.HeadlessTestKit
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
        // Arrange - Create and show the window
        var window = new MainWindow 
        { 
            DataContext = new MainWindowViewModel() 
        };
        window.Show();
        
        var ui = new Ui(window);

        // Act - Perform user interactions
        ui.Click("usernameField");
        ui.TypeText("usernameField", "testuser");
        ui.Click("passwordField");
        ui.TypeText("passwordField", "password123");
        ui.Click("loginButton");

        // Assert - Verify expected results
        ui.AssertText("statusLabel", "Login successful");
    }
}
```

**Important:** Always call `window.Show()` before creating the `Ui` instance. This ensures the window is properly initialized in headless mode.

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
- `SelectItem(id, itemText)` - Select item in ComboBox or ListBox
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

## Documentation

- **[Test Project Setup Guide](./docs/test-project-setup.md)** - Complete guide for setting up a headless test project
- **[AutomationId Conventions](./docs/conventions-automationid.md)** - Best practices for naming AutomationIds
- **[Overlay Panel Guide](./docs/overlay-panel.md)** - Recorder UI reference
- **[Troubleshooting](./docs/troubleshooting.md)** - Common issues and solutions
- **[GitHub Actions Publishing](./docs/github-nuget-publishing-setup.md)** - CI/CD setup for NuGet packages

## License

MIT

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.