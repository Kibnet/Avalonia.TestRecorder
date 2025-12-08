using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using SampleApp.ViewModels;
using SampleApp.Views;
using Avalonia.TestRecorder;
using System;

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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            desktop.MainWindow = mainWindow;

#if DEBUG
            // Attach recorder in debug mode or when explicitly requested
            var shouldRecord = Environment.GetEnvironmentVariable("AV_RECORDER") == "1" ||
                              Environment.GetCommandLineArgs().Contains("--record-tests");
            
            if (shouldRecord)
            {
                var options = new RecorderOptions
                {
                    OutputDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecordedTests"),
                    ScenarioName = "LoginFlow",                    
                };
                
                var session = TestRecorder.Attach(mainWindow, options);
                session.Start(); // Auto-start recording
                
                Console.WriteLine("Test recorder attached. Hotkeys:");
                Console.WriteLine("  Ctrl+Shift+R - Start/Stop Recording");
                Console.WriteLine("  Ctrl+Shift+P - Pause/Resume");
                Console.WriteLine("  Ctrl+Shift+S - Save Test");
                Console.WriteLine("  Ctrl+Shift+A - Capture Assert");
            }
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}