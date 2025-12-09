using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace SampleApp.Tests;

public partial class Recorded_LoginFlow_Tests
{
    [AvaloniaFact]
    public void Scenario_LoginFlow_20251209_200951()
    {
        // Initialize window with DataContext
        var window = new SampleApp.Views.MainWindow
        {
            DataContext = new SampleApp.ViewModels.MainWindowViewModel(),
        };
        window.Show();

        
        var ui = new Ui(window);
        
            ui.Click("Panel[0]/VisualLayerManager[0]/ContentPresenter[0]/StackPanel[0]/TextBlock[0]");
            ui.AssertText("Panel[0]/VisualLayerManager[0]/ContentPresenter[0]/StackPanel[0]/TextBlock[4]", "Recording Status:");
    }
}