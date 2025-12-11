using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Avalonia.VisualTree;
using Xunit;

namespace SampleApp.Tests.Manual;

/// <summary>
/// Tests for the "Layout Controls" tab of the SampleApp.
/// This tab demonstrates various layout panels including Grid, DockPanel, and WrapPanel.
/// </summary>
public class LayoutControlsTest
{
    [AvaloniaFact]
    public void GridLayout_AllButtons_AreAccessible()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Layout Controls tab (index 3)
        NavigateToTab(window, 3);

        // Click all grid buttons to verify they are accessible
        ui.Click("layout_grid_button1");
        ui.Click("layout_grid_button2");
        ui.Click("layout_grid_button3");
        ui.Click("layout_grid_button4");

        // Verify they are all visible
        ui.AssertVisible("layout_grid_button1");
        ui.AssertVisible("layout_grid_button2");
        ui.AssertVisible("layout_grid_button3");
        ui.AssertVisible("layout_grid_button4");
    }

    [AvaloniaFact]
    public void DockPanel_AllButtons_AreAccessible()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Layout Controls tab (index 3)
        NavigateToTab(window, 3);

        // Click all dock panel buttons to verify they are accessible
        ui.Click("layout_dock_top");
        ui.Click("layout_dock_bottom");
        ui.Click("layout_dock_left");
        ui.Click("layout_dock_right");
        ui.Click("layout_dock_center");

        // Verify they are all visible
        ui.AssertVisible("layout_dock_top");
        ui.AssertVisible("layout_dock_bottom");
        ui.AssertVisible("layout_dock_left");
        ui.AssertVisible("layout_dock_right");
        ui.AssertVisible("layout_dock_center");
    }

    [AvaloniaFact]
    public void WrapPanel_AllButtons_AreAccessible()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Layout Controls tab (index 3)
        NavigateToTab(window, 3);

        // Click all wrap panel buttons to verify they are accessible
        ui.Click("layout_wrap_button1");
        ui.Click("layout_wrap_button2");
        ui.Click("layout_wrap_button3");
        ui.Click("layout_wrap_button4");
        ui.Click("layout_wrap_button5");

        // Verify they are all visible
        ui.AssertVisible("layout_wrap_button1");
        ui.AssertVisible("layout_wrap_button2");
        ui.AssertVisible("layout_wrap_button3");
        ui.AssertVisible("layout_wrap_button4");
        ui.AssertVisible("layout_wrap_button5");
    }

    [AvaloniaFact]
    public void AllLayoutControls_AreVisible_InHeadlessMode()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Layout Controls tab (index 3)
        NavigateToTab(window, 3);

        // Verify all layout containers are visible
        ui.AssertVisible("layout_grid");
        ui.AssertVisible("layout_dockPanel");
        ui.AssertVisible("layout_wrapPanel");
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
