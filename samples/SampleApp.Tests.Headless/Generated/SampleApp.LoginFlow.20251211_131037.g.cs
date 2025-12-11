using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace SampleApp.Tests;

public partial class Recorded_LoginFlow_Tests
{
    [AvaloniaFact]
    public void Scenario_LoginFlow_20251211_131040()
    {
        // Initialize window with DataContext
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        
        var ui = new Ui(window);
        
        ui.Click("basic_usernameField"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
        ui.TypeText("basic_usernameField", "123"); // VALIDATION OK
        ui.Click("basic_passwordField"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
        ui.TypeText("basic_passwordField", "123"); // VALIDATION OK
        ui.Click("basic_statusLabel"); // VALIDATION OK
        ui.Click("basic_countryCombo"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
        ui.SelectItem("basic_countryCombo", "Canada"); // VALIDATION OK
        ui.Click("basic_submitButton"); // VALIDATION FAILED: Control mismatch: Found control doesn't match the original control. Multiple controls may have the same AutomationId.
        ui.AssertText("basic_statusLabel", "Form submitted successfully"); // VALIDATION OK
    }
}