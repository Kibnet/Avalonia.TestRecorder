using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Avalonia.VisualTree;
using Xunit;

namespace SampleApp.Tests.Manual;

/// <summary>
/// Tests for the "Advanced Controls" tab of the SampleApp.
/// This tab contains advanced controls like sliders, progress bars, date pickers, list boxes, and tree views.
/// </summary>
public class AdvancedControlsTest
{
    [AvaloniaFact]
    public void Slider_UpdateValue_UpdatesProgressBarAndText()
    {
        // Initialize window with DataContext
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Advanced Controls tab (index 1)
        NavigateToTab(window, 1);

        // Initially slider should be at 50 (default value in ViewModel)
        ui.AssertText("advanced_sliderValueText", "Slider Value: 50");
    }

    [AvaloniaFact]
    public void ListBox_SelectDifferentItems_UpdatesSelection()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);
        
        // Navigate to Advanced Controls tab (index 1)
        NavigateToTab(window, 1);

        // Select items in the list box
        ui.SelectItem("advanced_itemList", "Item 1");
        ui.SelectItem("advanced_itemList", "Item 2");
        ui.SelectItem("advanced_itemList", "Item 3");
        ui.SelectItem("advanced_itemList", "Item 4");
        ui.SelectItem("advanced_itemList", "Item 5");
    }

    [AvaloniaFact]
    public void ListBox_ClickOnItems_SelectsItem()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);
        
        // Navigate to Advanced Controls tab (index 1)
        NavigateToTab(window, 1);

        // Click on individual list items
        ui.Click("item_list_1");
        ui.Click("item_list_2");
        ui.Click("item_list_3");
    }

    [AvaloniaFact]
    public void AdvancedControls_AllControlsAreVisible_CanBeAccessed()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);
        
        // Navigate to Advanced Controls tab (index 1)
        NavigateToTab(window, 1);

        // Verify all advanced controls are visible and accessible
        ui.AssertVisible("advanced_slider");
        ui.AssertVisible("advanced_sliderValueText");
        ui.AssertVisible("advanced_progressBar");
        ui.AssertVisible("advanced_datePicker");
        ui.AssertVisible("advanced_itemList");
        ui.AssertVisible("advanced_treeView");
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
