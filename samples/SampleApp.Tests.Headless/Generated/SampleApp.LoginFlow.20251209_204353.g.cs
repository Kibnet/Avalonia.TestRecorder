using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace SampleApp.Tests;

public partial class Recorded_LoginFlow_Tests
{
    [AvaloniaFact]
    public void Scenario_LoginFlow_20251209_204355()
    {
        // Initialize window with DataContext
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        
        var ui = new Ui(window);
        
            ui.AssertText("statusLabel", "Ready");
            ui.Click("usernameField"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.TypeText("usernameField", "123");
            ui.AssertText("usernameField", "123"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.Click("passwordField"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.TypeText("passwordField", "йцу");
            ui.AssertText("passwordField", "йцу"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.AssertText("loginButton", "Login"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.Click("loginButton"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
            ui.AssertText("statusLabel", "Login successful");
            ui.AssertText("Panel[0]/VisualLayerManager[0]/ContentPresenter[0]/StackPanel[0]/TextBlock[4]", "Recording Status:");
    }
}