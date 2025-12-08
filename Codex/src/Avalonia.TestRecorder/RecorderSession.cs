using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avalonia.TestRecorder;

internal sealed class RecorderSession : IRecorderSession
{
    private readonly Window _window;
    private readonly RecorderOptions _options;
    private readonly SelectorResolver _selectorResolver;
    private readonly List<RecordedStep> _steps = new();
    private readonly ILogger _logger;
    private readonly RecorderOverlay? _overlay;
    private bool _disposed;
    private string _textBuffer = string.Empty;
    private Control? _textBufferTarget;
    private Control? _lastPointerTarget;
    private Point _lastPointerPosition;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.Now;

    public RecorderSession(Window window, RecorderOptions options)
    {
        _window = window;
        _options = options;
        _selectorResolver = new SelectorResolver(options.Selector);
        _logger = options.Logger ?? NullLogger.Instance;
        _overlay = options.EnableOverlay ? RecorderOverlay.TryAttach(window) : null;

        Subscribe();
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    public RecorderState State { get; private set; } = RecorderState.Off;

    public IReadOnlyList<RecordedStep> Steps => new ReadOnlyCollection<RecordedStep>(_steps);

    public void Start()
    {
        if (State == RecorderState.Recording)
        {
            return;
        }

        _logger.LogInformation("Recorder started");
        State = RecorderState.Recording;
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    public void Stop()
    {
        if (State == RecorderState.Off)
        {
            return;
        }

        FlushTextBuffer();
        State = RecorderState.Off;
        _logger.LogInformation("Recorder stopped");
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    public void Pause()
    {
        if (State != RecorderState.Recording)
        {
            return;
        }

        FlushTextBuffer();
        State = RecorderState.Paused;
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    public void Resume()
    {
        if (State != RecorderState.Paused)
        {
            return;
        }

        State = RecorderState.Recording;
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    public string SaveTestToFile()
    {
        var code = ExportTestCode();
        var directory = ResolveOutputDirectory();
        Directory.CreateDirectory(directory);

        var fileName = $"{Sanitize(GetAppName())}.{Sanitize(_options.ScenarioName)}.{DateTime.Now:yyyyMMdd_HHmmss}.g.cs";
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, code, Encoding.UTF8);
        _logger.LogInformation("Recorded test saved to {Path}", path);
        _overlay?.UpdateSavedPath(path);
        return path;
    }

    public string ExportTestCode()
    {
        FlushTextBuffer();
        var test = BuildRecordedTest();
        return _options.Codegen.TemplateProvider.Render(test, _options);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Unsubscribe();
        _overlay?.Detach();
    }

    private void Subscribe()
    {
        _window.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void Unsubscribe()
    {
        _window.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _window.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        _window.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        _window.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
        _window.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
        _window.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastPointerPosition = e.GetPosition(_window);
        _lastPointerTarget = e.Source as Control;

        if (State != RecorderState.Recording)
        {
            return;
        }

        FlushTextBuffer();
        var properties = e.GetCurrentPoint(_window).Properties;
        var target = _lastPointerTarget ?? _window;
        var selector = _selectorResolver.Resolve(target, _lastPointerPosition);
        var warning = selector.IsStable ? null : selector.Diagnostic;

        if (properties.IsLeftButtonPressed)
        {
            _steps.Add(new RecordedStep
            {
                Kind = RecordedStepKind.Click,
                Target = selector,
                Warning = warning
            });
        }
        else if (properties.IsRightButtonPressed)
        {
            _steps.Add(new RecordedStep
            {
                Kind = RecordedStepKind.RightClick,
                Target = selector,
                Warning = warning
            });
        }

        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _lastPointerPosition = e.GetPosition(_window);
        _lastPointerTarget = e.Source as Control;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _lastPointerPosition = e.GetPosition(_window);
        _lastPointerTarget = e.Source as Control;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _lastPointerPosition = e.GetPosition(_window);
        _lastPointerTarget = e.Source as Control;

        if (State != RecorderState.Recording)
        {
            return;
        }

        FlushTextBuffer();
        var selector = _selectorResolver.Resolve(_lastPointerTarget, _lastPointerPosition);
        _steps.Add(new RecordedStep
        {
            Kind = RecordedStepKind.Scroll,
            Target = selector,
            ScrollDelta = e.Delta,
            Warning = selector.IsStable ? null : selector.Diagnostic
        });
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (State != RecorderState.Recording || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        _textBufferTarget ??= GetFocusedControl() ?? e.Source as Control ?? _lastPointerTarget;
        _textBuffer += e.Text;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (HandleHotkeys(e))
        {
            e.Handled = true;
            return;
        }

        if (State != RecorderState.Recording)
        {
            return;
        }

        if (!IsNonTextKey(e.Key))
        {
            return;
        }

        FlushTextBuffer();
        var target = GetFocusedControl() ?? _lastPointerTarget ?? _window;
        var selector = _selectorResolver.Resolve(target, _lastPointerPosition);
        _steps.Add(new RecordedStep
        {
            Kind = RecordedStepKind.KeyPress,
            Target = selector,
            Key = e.Key.ToString(),
            Modifiers = e.KeyModifiers,
            Warning = selector.IsStable ? null : selector.Diagnostic
        });
        _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
    }

    private bool HandleHotkeys(KeyEventArgs e)
    {
        var hotkeys = _options.Hotkeys;

        if (hotkeys.Matches(e, hotkeys.StartStop))
        {
            if (State == RecorderState.Recording || State == RecorderState.Paused)
            {
                Stop();
            }
            else
            {
                Start();
            }

            return true;
        }

        if (hotkeys.Matches(e, hotkeys.PauseResume))
        {
            if (State == RecorderState.Recording)
            {
                Pause();
            }
            else if (State == RecorderState.Paused)
            {
                Resume();
            }

            return true;
        }

        if (hotkeys.Matches(e, hotkeys.Save))
        {
            SaveTestToFile();
            return true;
        }

        if (hotkeys.Matches(e, hotkeys.CaptureAssert))
        {
            CaptureAssert();
            return true;
        }

        return false;
    }

    private void CaptureAssert()
    {
        FlushTextBuffer();
        var control = ResolveAssertControl();
        if (control is null)
        {
            _logger.LogWarning("Could not resolve control for assert capture");
            return;
        }

        foreach (var extractor in _options.AssertExtractors)
        {
            if (!extractor.TryExtract(control, out var assert))
            {
                continue;
            }

            var selector = _selectorResolver.Resolve(control, _lastPointerPosition);
            _steps.Add(new RecordedStep
            {
                Kind = ToStepKind(assert.Kind),
                Target = selector,
                Expected = assert.Value,
                ExpectedBool = assert.BoolValue,
                Warning = selector.IsStable ? null : selector.Diagnostic
            });
            _overlay?.UpdateState(State, _steps.Count, GetDefaultOutputPath());
            return;
        }

        _logger.LogWarning("No assert extractor matched control of type {ControlType}", control.GetType().Name);
    }

    private RecordedStepKind ToStepKind(AssertKind kind) => kind switch
    {
        AssertKind.Text => RecordedStepKind.AssertText,
        AssertKind.Toggle => RecordedStepKind.AssertToggle,
        AssertKind.Visible => RecordedStepKind.AssertVisible,
        AssertKind.Enabled => RecordedStepKind.AssertEnabled,
        _ => RecordedStepKind.AssertText
    };

    private Control? ResolveAssertControl()
    {
        if (_options.Selector.PreferFocusedElementForAssert)
        {
            var focused = GetFocusedControl();
            if (focused is not null)
            {
                return focused;
            }
        }

        var hit = _window.InputHitTest(_lastPointerPosition);
        if (hit is Control control)
        {
            return control;
        }

        return _lastPointerTarget ?? GetFocusedControl();
    }

    private void FlushTextBuffer()
    {
        if (string.IsNullOrEmpty(_textBuffer))
        {
            return;
        }

        var target = _textBufferTarget ?? GetFocusedControl() ?? _lastPointerTarget ?? _window;
        var selector = _selectorResolver.Resolve(target, _lastPointerPosition);
        _steps.Add(new RecordedStep
        {
            Kind = RecordedStepKind.TextInput,
            Target = selector,
            Text = _textBuffer,
            Warning = selector.IsStable ? null : selector.Diagnostic
        });
        _textBuffer = string.Empty;
        _textBufferTarget = null;
    }

    private RecordedTest BuildRecordedTest()
    {
        var appName = GetAppName();
        var scenario = _options.ScenarioName;
        var appType = ResolveAppType() ?? typeof(Application);
        var appTypeName = appType.FullName ?? "Avalonia.Application";

        var windowTypeName = _window.GetType().FullName ?? "Avalonia.Controls.Window";

        return new RecordedTest(appName, scenario, appTypeName, windowTypeName, _steps, DateTimeOffset.Now);
    }

    private Type? ResolveAppType()
    {
        if (Application.Current is not null)
        {
            return Application.Current.GetType();
        }

        try
        {
            return _window.GetType().Assembly.GetTypes().FirstOrDefault(t => typeof(Application).IsAssignableFrom(t));
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types?.FirstOrDefault(t => t is not null && typeof(Application).IsAssignableFrom(t));
        }
        catch
        {
            return null;
        }
    }

    private string ResolveOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_options.OutputDirectory))
        {
            return Path.GetFullPath(_options.OutputDirectory);
        }

        if (Debugger.IsAttached)
        {
            return Path.Combine(AppContext.BaseDirectory, "RecordedTests");
        }

        return Path.Combine(Path.GetTempPath(), "avalonia-recorded-tests");
    }

    private string GetDefaultOutputPath() => _options.OutputDirectory ?? ResolveOutputDirectory();

    private Control? GetFocusedControl()
    {
        var focused = _window.FocusManager?.GetFocusedElement();
        return focused as Control;
    }

    private static bool IsNonTextKey(Key key) => key is Key.Enter or Key.Tab or Key.Back or Key.Delete or Key.Escape
        or Key.Left or Key.Right or Key.Up or Key.Down;

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        var result = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(result) ? "Scenario" : result;
    }

    private string GetAppName()
    {
        return _window.GetType().Assembly.GetName().Name ?? "App";
    }
}
