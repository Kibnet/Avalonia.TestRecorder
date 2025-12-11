using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace SampleApp.Tests.Manual;

/// <summary>
/// Tests for the "Basic Controls" tab of the SampleApp.
/// This tab contains basic form controls like text boxes, combo boxes, checkboxes, radio buttons, and a submit button.
/// </summary>
public class BasicControlsTest
{
    [AvaloniaFact]
    public void SubmitBasicForm_WithValidData_ShowsSuccessMessage()
    {
        // Initialize window with DataContext
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Fill out the form
        ui.Click("basic_usernameField");
        ui.TypeText("basic_usernameField", "john_doe");

        ui.Click("basic_passwordField");
        ui.TypeText("basic_passwordField", "secret123");

        ui.SelectItem("basic_countryCombo", "Canada");

        // Check subscription checkbox
        ui.Click("basic_subscribeCheck");
        ui.AssertChecked("basic_subscribeCheck", true);

        // Select gender
        ui.Click("basic_genderMale");
        ui.AssertChecked("basic_genderMale", true);

        // Submit the form
        ui.Click("basic_submitButton");

        // Verify success message
        ui.AssertText("basic_statusLabel", "Form submitted successfully");
    }

    [AvaloniaFact]
    public void CountryComboBox_SelectDifferentCountries_UpdatesSelection()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Test selecting different countries
        ui.SelectItem("basic_countryCombo", "United States");
        ui.SelectItem("basic_countryCombo", "Germany");
        ui.SelectItem("basic_countryCombo", "France");
        
        // Form should still work after changing selections
        ui.Click("basic_submitButton");
        ui.AssertText("basic_statusLabel", "Form submitted successfully");
    }

    [AvaloniaFact]
    public void GenderRadioButtons_SelectFemale_UpdatesSelection()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Select female radio button
        ui.Click("basic_genderFemale");
        ui.AssertChecked("basic_genderFemale", true);

        // Male should be unchecked (radio buttons are mutually exclusive)
        ui.AssertChecked("basic_genderMale", false);
    }

    [AvaloniaFact]
    public void SubscribeCheckbox_ToggleMultipleTimes_UpdatesState()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Initially unchecked
        ui.AssertChecked("basic_subscribeCheck", false);

        // Check it
        ui.Click("basic_subscribeCheck");
        ui.AssertChecked("basic_subscribeCheck", true);

        // Uncheck it
        ui.Click("basic_subscribeCheck");
        ui.AssertChecked("basic_subscribeCheck", false);

        // Check it again
        ui.Click("basic_subscribeCheck");
        ui.AssertChecked("basic_subscribeCheck", true);
    }

    [AvaloniaFact]
    public void TextBoxes_TypeDifferentValues_AcceptsInput()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Type in username
        ui.Click("basic_usernameField");
        ui.TypeText("basic_usernameField", "alice");

        // Type in password
        ui.Click("basic_passwordField");
        ui.TypeText("basic_passwordField", "pass123");

        // Submit and verify
        ui.Click("basic_submitButton");
        ui.AssertText("basic_statusLabel", "Form submitted successfully");
    }
}
