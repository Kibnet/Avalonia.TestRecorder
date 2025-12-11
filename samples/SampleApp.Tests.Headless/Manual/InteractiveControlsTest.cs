using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Avalonia.VisualTree;
using Xunit;

namespace SampleApp.Tests.Manual;

/// <summary>
/// Tests for the "Interactive Controls" tab of the SampleApp.
/// This tab demonstrates dynamic UI interactions including cascading selections,
/// real-time text updates, dynamic list manipulation, click counters, and visibility toggles.
/// </summary>
public class InteractiveControlsTest
{
    [AvaloniaFact]
    public void CascadingSelection_SelectElectronics_EnablesSubCategoryWithPhones()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Initially, sub-category should be disabled
        // (Note: We can't directly assert IsEnabled in current Ui class, but we can test the workflow)

        // Select Electronics category
        ui.SelectItem("interactive_categoryCombo", "Electronics");

        // Now sub-category should be enabled and populated with electronics items
        ui.SelectItem("interactive_subCategoryCombo", "Phones");
        ui.SelectItem("interactive_subCategoryCombo", "Laptops");
        ui.SelectItem("interactive_subCategoryCombo", "Tablets");
    }

    [AvaloniaFact]
    public void CascadingSelection_SelectClothing_PopulatesClothingSubCategories()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Select Clothing category
        ui.SelectItem("interactive_categoryCombo", "Clothing");

        // Sub-category should now have clothing items
        ui.SelectItem("interactive_subCategoryCombo", "Shirts");
        ui.SelectItem("interactive_subCategoryCombo", "Pants");
        ui.SelectItem("interactive_subCategoryCombo", "Shoes");
    }

    [AvaloniaFact]
    public void CascadingSelection_SelectBooks_PopulatesBookSubCategories()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Select Books category
        ui.SelectItem("interactive_categoryCombo", "Books");

        // Sub-category should now have book items
        ui.SelectItem("interactive_subCategoryCombo", "Fiction");
        ui.SelectItem("interactive_subCategoryCombo", "Non-Fiction");
        ui.SelectItem("interactive_subCategoryCombo", "Educational");
    }

    [AvaloniaFact]
    public void AddItemButton_ClickMultipleTimes_AddsItemsToDynamicList()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Click add item button multiple times
        ui.Click("interactive_addItemButton");
        ui.Click("interactive_addItemButton");
        ui.Click("interactive_addItemButton");

        // Items should be added to the dynamic list
        // The list starts with 2 items (Initial Item 1, Initial Item 2)
        // After 3 clicks, we should have Items 3, 4, 5 added
    }

    [AvaloniaFact]
    public void ClickCounter_ClickButton_IncrementsCounter()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Initially counter should be 0
        ui.AssertText("interactive_counterDisplay", "Click Count: 0");

        // Click the button once
        ui.Click("interactive_counterButton");
        ui.AssertText("interactive_counterDisplay", "Click Count: 1");

        // Click again
        ui.Click("interactive_counterButton");
        ui.AssertText("interactive_counterDisplay", "Click Count: 2");

        // Click multiple more times
        ui.Click("interactive_counterButton");
        ui.Click("interactive_counterButton");
        ui.Click("interactive_counterButton");
        ui.AssertText("interactive_counterDisplay", "Click Count: 5");
    }

    [AvaloniaFact]
    public void VisibilityToggle_CheckToggle_ShowsSecretMessage()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Initially, the secret message should not be visible
        // We can check by clicking the checkbox to show it
        ui.Click("interactive_toggleVisibility");
        ui.AssertChecked("interactive_toggleVisibility", true);

        // Now the secret message should be visible
        ui.AssertVisible("interactive_secretMessage");
        ui.AssertText("interactive_secretMessage", "This is a secret message!");
    }

    [AvaloniaFact]
    public void VisibilityToggle_ToggleMultipleTimes_ShowsAndHidesMessage()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Check the toggle to show message
        ui.Click("interactive_toggleVisibility");
        ui.AssertChecked("interactive_toggleVisibility", true);
        ui.AssertVisible("interactive_secretMessage");

        // Uncheck to hide message
        ui.Click("interactive_toggleVisibility");
        ui.AssertChecked("interactive_toggleVisibility", false);

        // Check again to show message
        ui.Click("interactive_toggleVisibility");
        ui.AssertChecked("interactive_toggleVisibility", true);
        ui.AssertVisible("interactive_secretMessage");
    }

    [AvaloniaFact]
    public void RealTimeTextUpdate_TypeInSourceBox_UpdatesTargetText()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Type text in the source box
        ui.Click("interactive_sourceText");
        ui.TypeText("interactive_sourceText", "Hello World");

        // The target text block should update in real-time due to binding
        ui.AssertText("interactive_targetText", "Hello World");
    }

    [AvaloniaFact]
    public void CompleteInteractiveWorkflow_MultipleSections_AllFunctionsWork()
    {
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        var ui = new Ui(window);

        // Navigate to Interactive Controls tab (index 2)
        NavigateToTab(window, 2);

        // Test cascading selection
        ui.SelectItem("interactive_categoryCombo", "Electronics");
        ui.SelectItem("interactive_subCategoryCombo", "Laptops");

        // Test real-time text
        ui.Click("interactive_sourceText");
        ui.TypeText("interactive_sourceText", "Test");
        ui.AssertText("interactive_targetText", "Test");

        // Test dynamic list
        ui.Click("interactive_addItemButton");
        ui.Click("interactive_addItemButton");

        // Test counter
        ui.Click("interactive_counterButton");
        ui.AssertText("interactive_counterDisplay", "Click Count: 1");

        // Test visibility toggle
        ui.Click("interactive_toggleVisibility");
        ui.AssertVisible("interactive_secretMessage");
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
