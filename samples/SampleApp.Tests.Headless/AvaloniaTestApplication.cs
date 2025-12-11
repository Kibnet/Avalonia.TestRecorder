using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(SampleApp.Tests.Headless.TestAppBuilder))]

namespace SampleApp.Tests.Headless;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<SampleApp.App>()
        .UsePlatformDetect()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        })
        .LogToTrace();
}
