using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using SampleApp;
using SampleApp.ViewModels;
using SampleApp.Views;
using Xunit;

namespace SampleApp.Tests.Headless.Manual;

public class LoginFlowTest
{
    [AvaloniaFact]
    public void LoginScenario_EnterCredentialsAndClickLogin_ShowsSuccessMessage()
    {
        // Arrange - Create the application and window in headless mode
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel()
        };
        window.Show();

        var ui = new Ui(window);

        // Act - Perform the login flow
        ui.Click("usernameField");
        ui.TypeText("usernameField", "testuser");
        
        ui.Click("passwordField");
        ui.TypeText("passwordField", "password123");
        
        ui.Click("loginButton");

        // Assert - Verify the status message
        ui.AssertText("statusLabel", "Login successful");
    }

    [AvaloniaFact]
    public void StatusLabel_InitialState_ShowsReady()
    {
        // Arrange
        var window = new MainWindow
        {
            DataContext = new MainWindowViewModel()
        };
        window.Show();

        var ui = new Ui(window);

        // Assert - Verify initial state
        ui.AssertText("statusLabel", "Ready");
    }
}
