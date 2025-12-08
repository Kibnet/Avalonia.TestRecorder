using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.TestRecorder;

namespace SampleApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            var window = new MainWindow
            {
                DataContext = vm
            };

            if (ShouldEnableRecorder())
            {
                var output = Path.Combine(AppContext.BaseDirectory, "RecordedTests");
                var options = new RecorderOptions
                {
                    OutputDirectory = output,
                    ScenarioName = "SampleScenario",
                    EnableOverlay = true,
                    Codegen = new CodegenOptions
                    {
                        TestNamespace = "SampleApp.Tests.Headless",
                        ClassPrefix = "Recorded",
                        TestFramework = TestFramework.Xunit
                    }
                };

                vm.RecorderOutput = output;
                TestRecorder.Attach(window, options);
            }

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool ShouldEnableRecorder()
    {
#if DEBUG
        return true;
#else
        var env = Environment.GetEnvironmentVariable("AV_RECORDER");
        if (!string.IsNullOrEmpty(env) && env != "0")
        {
            return true;
        }

        return Environment.GetCommandLineArgs()
            .Any(a => a.Equals("--record-tests", StringComparison.OrdinalIgnoreCase));
#endif
    }
}
