using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using SampleApp;
using Xunit;

namespace SampleApp.Tests.Headless;

public class ManualDslTests
{
    [AvaloniaFact]
    public async Task Waits_for_status_change_after_clear()
    {
        await using var app = await HeadlessTestApplication.StartAsync<App, MainWindow>();
        await app.RunAsync(async window =>
        {
            var ui = new Ui(window);
            await ui.TypeTextAsync("SearchBox", "Headless");
            await ui.ClickAsync("ClearButton");
            await ui.WaitForAsync("StatusText", control => control is TextBlock tb && tb.Text == "Очищено");
        });
    }

    [AvaloniaFact]
    public async Task Hover_updates_indicator()
    {
        await using var app = await HeadlessTestApplication.StartAsync<App, MainWindow>();
        await app.RunAsync(async window =>
        {
            var ui = new Ui(window);
            await ui.HoverAsync("HoverTarget");
            await ui.AssertTextAsync("HoverStatus", "Курсор над блоком");
        });
    }
}
