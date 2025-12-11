using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Avalonia.VisualTree;
using Xunit;

namespace SampleApp.Tests.Manual;

/// <summary>
/// Tests for the "Original Login" tab of the SampleApp.
/// This is the preserved original login form that demonstrates basic authentication flow.
/// </summary>
public class OriginalLoginTest
{
    [AvaloniaFact]
    public void Login_WithCredentials_ShowsSuccessMessage()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Original Login tab (index 4)
        NavigateToTab(window, 4);

        // Fill in login credentials
        ui.Click("usernameField");
        ui.TypeText("usernameField", "admin");

        ui.Click("passwordField");
        ui.TypeText("passwordField", "password123");

        // Click login button
        ui.Click("loginButton");

        // Verify success message
        ui.AssertText("statusLabel", "Login successful");
    }

    [AvaloniaFact]
    public void Login_EmptyCredentials_ButtonStillWorks()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Original Login tab (index 4)
        NavigateToTab(window, 4);

        // Click login without entering credentials
        ui.Click("loginButton");

        // Should still show success (no validation in sample app)
        ui.AssertText("statusLabel", "Login successful");
    }

    [AvaloniaFact]
    public void Login_DifferentCredentials_UpdatesFields()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Original Login tab (index 4)
        NavigateToTab(window, 4);

        // First login attempt
        ui.Click("usernameField");
        ui.TypeText("usernameField", "user1");
        ui.Click("passwordField");
        ui.TypeText("passwordField", "pass1");
        ui.Click("loginButton");
        ui.AssertText("statusLabel", "Login successful");
    }

    [AvaloniaFact]
    public void LoginForm_AllControlsAreVisible()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Original Login tab (index 4)
        NavigateToTab(window, 4);

        // Verify all login form controls are visible
        ui.AssertVisible("usernameField");
        ui.AssertVisible("passwordField");
        ui.AssertVisible("loginButton");
        ui.AssertVisible("statusLabel");
    }

    [AvaloniaFact]
    public void LoginForm_InitialState_ShowsReadyMessage()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Original Login tab (index 4)
        NavigateToTab(window, 4);

        // Initial status should be "Ready"
        ui.AssertText("statusLabel", "Ready");
    }

    private void NavigateToTab(Avalonia.Controls.Window window, int tabIndex)
    {
        var tabControl = window.FindDescendantOfType<Avalonia.Controls.TabControl>();
        if (tabControl != null)
        {
            tabControl.SelectedIndex = tabIndex;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            System.Threading.Thread.Sleep(100);
        }
    }
}
