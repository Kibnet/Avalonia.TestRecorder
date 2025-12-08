using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using SampleApp;
using Xunit;

namespace SampleApp.Tests.Headless;

public sealed class Recorded_SampleScenario_Tests
{
    [AvaloniaFact]
    public async Task Scenario_20251208_Recorded()
    {
        await using var app = await HeadlessTestApplication.StartAsync<App, MainWindow>();
        await app.RunAsync(async window =>
        {
            var ui = new Ui(window);
            await ui.ClickAsync("SearchBox");
            await ui.TypeTextAsync("SearchBox", "Avalonia UI");
            await ui.ScrollAsync("ItemsList", new Vector(0, 4));
            await ui.ClickAsync("SubmitButton");
            await ui.AssertTextAsync("StatusText", "Поиск: Avalonia UI");
            await ui.AssertVisibleAsync("HoverTarget", true);
        });
    }
}
