using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.TestRecorder.CodeGen;
using Xunit;

namespace Avalonia.TestRecorder.Tests;

public class WindowDataContextTest
{
    [AvaloniaFact]
    public void TestCodeGenerator_IncludesWindowAndDataContextInfo()
    {
        // Arrange
        var options = new CodegenOptions();
        var generator = new TestCodeGenerator(options, "TestApp");
        
        var window = new TestWindow
        {
            DataContext = new TestViewModel()
        };
        
        var steps = new List<RecordedStep>
        {
            new RecordedStep
            {
                Type = StepType.Click,
                Selector = "testButton",
                Quality = SelectorQuality.High
            }
        };

        // Act
        var code = generator.Generate(steps, "TestScenario", window);

        // Assert
        Assert.Contains("var window = new Avalonia.TestRecorder.Tests.WindowDataContextTest.TestWindow", code);
        Assert.Contains("DataContext = new Avalonia.TestRecorder.Tests.WindowDataContextTest.TestViewModel()", code);
        Assert.Contains("window.Show()", code);
        Assert.Contains("ui.Click(\"testButton\");", code);
    }

    public class TestWindow : Window
    {
    }

    public class TestViewModel
    {
        public string TestProperty { get; set; } = "Test";
    }
}