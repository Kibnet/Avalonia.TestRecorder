using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Avalonia.HeadlessTestKit;

/// <summary>
/// Minimal host that spins up Avalonia headless platform for UI tests.
/// </summary>
public static class HeadlessTestApplication
{
    public static Task<HeadlessAppHost<TApp, TWindow>> StartAsync<TApp, TWindow>()
        where TApp : Application, new()
        where TWindow : Window, new()
    {
        return HeadlessAppHost<TApp, TWindow>.StartAsync();
    }
}

public sealed class HeadlessAppHost<TApp, TWindow> : IAsyncDisposable
    where TApp : Application, new()
    where TWindow : Window, new()
{
    private readonly ClassicDesktopStyleApplicationLifetime _lifetime;

    private HeadlessAppHost(ClassicDesktopStyleApplicationLifetime lifetime, TWindow window)
    {
        _lifetime = lifetime;
        Window = window;
    }

    public TWindow Window { get; }

    internal static async Task<HeadlessAppHost<TApp, TWindow>> StartAsync()
    {
        var lifetime = new ClassicDesktopStyleApplicationLifetime();
        AppBuilder.Configure<TApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithLifetime(lifetime);

        TWindow? window = null;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            window = new TWindow();
            lifetime.MainWindow = window;
            window.Show();
        }, DispatcherPriority.Send);

        return new HeadlessAppHost<TApp, TWindow>(lifetime, window!);
    }

    public async Task RunAsync(Func<TWindow, Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(async () => await action(Window), DispatcherPriority.Send);
        await WaitForIdleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Window.Close();
            _lifetime.Shutdown();
        }).GetTask();
    }

    public Task WaitForIdleAsync()
    {
        return Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
    }
}
