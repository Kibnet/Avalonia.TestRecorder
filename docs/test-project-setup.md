# Test Project Setup Guide

This guide walks you through setting up a headless test project for your Avalonia application.

## Prerequisites

- .NET SDK 8.0 or higher
- An existing Avalonia application
- Basic familiarity with xUnit testing framework

## Step 1: Create the Test Project

Create a new xUnit test project:

```bash
dotnet new xunit -n YourApp.Tests.Headless
cd YourApp.Tests.Headless
```

## Step 2: Add Required Packages

Add the necessary NuGet packages:

```bash
# Avalonia headless testing infrastructure
dotnet add package Avalonia.Headless.XUnit

# Test helper DSL (if using published package)
dotnet add package Avalonia.HeadlessTestKit

# Or add as project reference (if working with source)
dotnet add reference ../../src/Avalonia.HeadlessTestKit/Avalonia.HeadlessTestKit.csproj
```

## Step 3: Reference Your Application

Add a project reference to your Avalonia application:

```bash
dotnet add reference ../YourApp/YourApp.csproj
```

## Step 4: Configure the Project File

Your `.csproj` file should look like this:

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
    <!-- Test framework packages -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    
    <!-- Avalonia headless testing -->
    <PackageReference Include="Avalonia.Headless.XUnit" Version="11.3.6" />
    
    <!-- Optional: Coverage -->
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Implicit xUnit usings -->
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <!-- Test helper DSL -->
    <PackageReference Include="Avalonia.HeadlessTestKit" Version="1.0.0" />
    
    <!-- Your application -->
    <ProjectReference Include="..\YourApp\YourApp.csproj" />
  </ItemGroup>

</Project>
```

## Step 5: Create Avalonia Test Application Bootstrap

Create a file named `AvaloniaTestApplication.cs` in your test project root:

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

**Important Configuration Notes:**

1. **Assembly Attribute**: The `[assembly: AvaloniaTestApplication]` attribute is **required** for xUnit to discover and initialize Avalonia tests
2. **Namespace**: Replace `YourApp.Tests.Headless` with your actual test project namespace
3. **App Configuration**: Replace `YourApp.App` with your actual application's `App` class
4. **UseHeadlessDrawing = false**: Recommended setting for better test stability (uses software rendering)

## Step 6: Organize Test Files

Create a folder structure for your tests:

```
YourApp.Tests.Headless/
├── AvaloniaTestApplication.cs    # Bootstrap file
├── Manual/                        # Hand-written tests
│   ├── LoginTests.cs
│   └── DashboardTests.cs
└── Generated/                     # Auto-generated from recorder
    └── RecordedScenarios.cs
```

## Step 7: Write Your First Test

Create a test file in the `Manual` folder:

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace YourApp.Tests.Headless.Manual;

public class LoginTests
{
    [AvaloniaFact]
    public void Login_WithValidCredentials_ShowsSuccessMessage()
    {
        // Arrange - Create window with DataContext
        var window = new YourApp.Views.MainWindow
        {
            DataContext = new YourApp.ViewModels.MainWindowViewModel()
        };
        window.Show(); // IMPORTANT: Always call Show() first!
        
        var ui = new Ui(window);

        // Act - Perform interactions
        ui.Click("usernameField");
        ui.TypeText("usernameField", "admin");
        
        ui.Click("passwordField");
        ui.TypeText("passwordField", "password123");
        
        ui.Click("loginButton");

        // Assert - Verify results
        ui.AssertText("statusLabel", "Login successful");
    }
}
```

## Step 8: Run Tests

Run your tests using the .NET CLI:

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run specific test
dotnet test --filter "FullyQualifiedName~LoginTests"

# Run with code coverage (if coverlet is installed)
dotnet test /p:CollectCoverage=true
```

## Common Issues and Solutions

### Issue: Tests fail with "Window is null" or "Control not found"

**Solution**: Always call `window.Show()` before creating the `Ui` instance:

```csharp
var window = new MainWindow { DataContext = new MainWindowViewModel() };
window.Show(); // ✅ Required!
var ui = new Ui(window);
```

### Issue: Tests pass locally but fail in CI/CD

**Solution**: Ensure `UseHeadlessDrawing = false` is set in `AvaloniaTestApplication.cs`:

```csharp
.UseHeadless(new AvaloniaHeadlessPlatformOptions
{
    UseHeadlessDrawing = false  // ✅ Recommended for CI
})
```

### Issue: "Control not found" errors for dynamically loaded controls

**Solution**: Use `WaitForVisible` to ensure controls are loaded:

```csharp
ui.WaitForVisible("dynamicButton", TimeSpan.FromSeconds(2));
ui.Click("dynamicButton");
```

### Issue: TabControl tests fail because controls aren't visible

**Solution**: Navigate to the correct tab first:

```csharp
// Navigate to tab before interacting with its controls
var tabControl = window.FindDescendantOfType<TabControl>();
tabControl.SelectedIndex = 1; // Navigate to second tab
Dispatcher.UIThread.RunJobs();
Thread.Sleep(100); // Allow UI to settle

ui.Click("controlOnSecondTab");
```

## Best Practices

1. **Always set AutomationId on interactive controls**:
   ```xml
   <Button AutomationProperties.AutomationId="submitButton" />
   ```

2. **Call window.Show() before UI interactions**:
   ```csharp
   window.Show(); // Always first
   var ui = new Ui(window);
   ```

3. **Use meaningful test names**:
   ```csharp
   // ✅ Good
   public void Login_WithInvalidCredentials_ShowsErrorMessage()
   
   // ❌ Bad
   public void Test1()
   ```

4. **Group related tests in classes**:
   ```csharp
   public class LoginTests { /* login scenarios */ }
   public class DashboardTests { /* dashboard scenarios */ }
   ```

5. **Use WaitFor methods for asynchronous operations**:
   ```csharp
   ui.Click("loadDataButton");
   ui.WaitForText("statusLabel", "Data loaded", TimeSpan.FromSeconds(5));
   ```

6. **Create fresh window instances for each test**:
   ```csharp
   [AvaloniaFact]
   public void Test1()
   {
       var window = new MainWindow { /* ... */ }; // New instance
       // ...
   }
   
   [AvaloniaFact]
   public void Test2()
   {
       var window = new MainWindow { /* ... */ }; // Another new instance
       // ...
   }
   ```

## Advanced: Using Recorded Tests

When you record tests using the Avalonia Test Recorder, save them to the `Generated` folder:

1. Run your app with recorder enabled (`AV_RECORDER=1`)
2. Record your scenario
3. Save to `Generated/ScenarioName.g.cs`
4. The test will automatically be discovered by xUnit

Generated tests follow the same pattern as manual tests but include validation comments:

```csharp
ui.Click("loginButton"); // VALIDATION OK
ui.AssertText("statusLabel", "Success"); // VALIDATION OK
```

## Next Steps

- Read the [AutomationId Conventions](./conventions-automationid.md) guide
- Check out the [Troubleshooting Guide](./troubleshooting.md)
- Explore the [SampleApp.Tests.Headless](../samples/SampleApp.Tests.Headless/) reference project
- Learn about the [Ui DSL API](../README.md#test-dsl-reference)

## Summary

✅ Test project structure created  
✅ Required packages installed  
✅ AvaloniaTestApplication.cs configured  
✅ First test written and running  

You're now ready to write comprehensive headless tests for your Avalonia application!
