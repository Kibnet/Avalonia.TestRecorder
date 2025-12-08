using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using SampleApp.Views;
using Xunit;

namespace SampleApp.Tests;

public class Recorded_LoginFlow_Tests
{
    [AvaloniaFact]
    public void Scenario_LoginFlow_20251208_165301()
    {
        // TODO: Initialize your application and window here
        // Example:
        var window = new MainWindow();
        window.Show();
        
        var ui = new Ui(window);
        
            ui.Click("usernameField");
            ui.TypeText("usernameField", "123");
            ui.Click("passwordField");
            ui.TypeText("passwordField", "йцуйцу");
            ui.Click("loginButton");
            ui.AssertText("statusLabel", "Login successful");
            ui.Click("Panel[0]/VisualLayerManager[0]/ContentPresenter[0]/StackPanel[0]/DockPanel[0]/RecorderOverlay[0]/ContentPresenter[0]/Border[0]/StackPanel[0]/Button[0]/ContentPresenter[0]"); // CRITICAL: tree path selector - high risk of breakage
    }
}
