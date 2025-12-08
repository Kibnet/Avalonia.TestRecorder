using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using SampleApp.Views;
using Xunit;

namespace SampleApp.Tests;

public class Recorded_LoginFlow_Tests2
{
    [AvaloniaFact]
    public void Scenario_LoginFlow_20251208_165914()
    {
        // TODO: Initialize your application and window here
        // Example:
        var window = new MainWindow();
        window.Show();
        
        var ui = new Ui(window);
        
            ui.Click("usernameField");
            ui.TypeText("usernameField", "123");
            ui.KeyPress("Tab");
            ui.TypeText("passwordField", "уыцкцук452");
            ui.AssertText("statusLabel", "Ready");
            ui.Click("loginButton");
            ui.AssertText("statusLabel", "Login successful");
    }
}
