using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.HeadlessTestKit;

/// <summary>
/// Lightweight DSL for interacting with Avalonia controls in headless tests.
/// </summary>
public sealed class Ui
{
    private readonly Window _window;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

    public Ui(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public async Task ClickAsync(string selector) => await InvokeAsync(selector, control =>
    {
        control.Focus();
        switch (control)
        {
            case ToggleButton toggle:
                toggle.IsChecked = !(toggle.IsChecked ?? false);
                break;
            case Button button:
                button.Command?.Execute(button.CommandParameter);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                break;
        }
    });

    public Task DoubleClickAsync(string selector) => ClickTwice(selector);

    public Task RightClickAsync(string selector) => ClickAsync(selector);

    public async Task HoverAsync(string selector)
    {
        var control = await FindControlAsync(selector);
        await Dispatcher.UIThread.InvokeAsync(() => control.Focus());
    }

    public async Task ScrollAsync(string selector, Vector delta)
    {
        var control = await FindControlAsync(selector);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var scrollViewer = control as ScrollViewer ?? control.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (scrollViewer is null)
            {
                return;
            }

            var verticalSteps = (int)Math.Max(1, Math.Abs(delta.Y));
            var horizontalSteps = (int)Math.Max(1, Math.Abs(delta.X));

            for (var i = 0; i < verticalSteps; i++)
            {
                if (delta.Y < 0)
                {
                    scrollViewer.LineUp();
                }
                else
                {
                    scrollViewer.LineDown();
                }
            }

            for (var i = 0; i < horizontalSteps; i++)
            {
                if (delta.X < 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
            }
        });

        await WaitForIdleAsync();
    }

    public async Task TypeTextAsync(string selector, string text)
    {
        var control = await FindControlAsync(selector);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.Focus();
                    textBox.Text = text;
                    textBox.CaretIndex = text.Length;
                    break;
            }
        });

        await WaitForIdleAsync();
    }

    public async Task KeyPressAsync(string selector, Key key, KeyModifiers modifiers)
    {
        var control = await FindControlAsync(selector);
        await Dispatcher.UIThread.InvokeAsync(() => control.Focus());
        // In headless mode we keep the method for parity with generator even if the app logic relies on bindings.
        await WaitForIdleAsync();
    }

    public async Task AssertTextAsync(string selector, string expected)
    {
        var control = await FindControlAsync(selector);
        await WaitForAsync(selector, c => ReadText(c) == expected);
    }

    public async Task AssertToggleAsync(string selector, bool expected)
    {
        await WaitForAsync(selector, control =>
        {
            return control switch
            {
                ToggleButton toggle => (toggle.IsChecked ?? false) == expected,
                _ => false
            };
        });
    }

    public async Task AssertVisibleAsync(string selector, bool expected)
    {
        await WaitForAsync(selector, c => c.IsVisible == expected);
    }

    public async Task AssertEnabledAsync(string selector, bool expected)
    {
        await WaitForAsync(selector, c => c.IsEffectivelyEnabled == expected);
    }

    public async Task WaitForAsync(string selector, Func<Control, bool> condition, TimeSpan? timeout = null)
    {
        var control = await FindControlAsync(selector, timeout);
        var sw = Stopwatch.StartNew();
        var limit = timeout ?? _defaultTimeout;

        while (sw.Elapsed < limit)
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() => condition(control));
            if (result)
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Condition not met for selector '{selector}' within {limit.TotalMilliseconds} ms.");
    }

    private async Task InvokeAsync(string selector, Action<Control> action)
    {
        var control = await FindControlAsync(selector);
        await Dispatcher.UIThread.InvokeAsync(() => action(control));
        await WaitForIdleAsync();
    }

    private async Task ClickTwice(string selector)
    {
        await ClickAsync(selector);
        await ClickAsync(selector);
    }

    private async Task WaitForIdleAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
    }

    private async Task<Control> FindControlAsync(string selector, TimeSpan? timeout = null)
    {
        var sw = Stopwatch.StartNew();
        var limit = timeout ?? _defaultTimeout;

        while (sw.Elapsed < limit)
        {
            var control = await Dispatcher.UIThread.InvokeAsync(() => FindControl(selector));
            if (control is not null)
            {
                return control;
            }

            await Task.Delay(50);
        }

        throw new InvalidOperationException($"Control with selector '{selector}' not found after {limit.TotalMilliseconds} ms.");
    }

    private Control? FindControl(string selector)
    {
        Control? byIdOrName = _window.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(c =>
                string.Equals(AutomationProperties.GetAutomationId(c), selector, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Name, selector, StringComparison.OrdinalIgnoreCase));

        if (byIdOrName is not null)
        {
            return byIdOrName;
        }

        var byPath = _window.GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(c => string.Equals(BuildTreePath(c), selector, StringComparison.Ordinal));

        if (byPath is not null)
        {
            return byPath;
        }

        if (TryParsePoint(selector, out var point))
        {
            return _window.InputHitTest(point) as Control;
        }

        return null;
    }

    private static bool TryParsePoint(string value, out Point point)
    {
        point = default;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
        {
            point = new Point(x, y);
            return true;
        }

        return false;
    }

    private static string BuildTreePath(Visual visual)
    {
        var segments = new List<string>();
        var current = visual;
        while (current is not null)
        {
            var parent = current.GetVisualParent();
            var index = 0;
            if (parent != null)
            {
                var siblings = parent.GetVisualChildren().ToList();
                index = siblings.IndexOf(current);
            }

            segments.Add($"{current.GetType().Name}[{index}]");
            current = parent!;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }

    private static string? ReadText(Control control) => control switch
    {
        TextBox textBox => textBox.Text,
        TextBlock textBlock => textBlock.Text,
        ContentControl content => content.Content?.ToString(),
        _ => null
    };
}
