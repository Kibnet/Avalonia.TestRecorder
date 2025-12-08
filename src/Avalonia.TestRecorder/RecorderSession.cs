using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.TestRecorder.Assertions;
using Avalonia.TestRecorder.CodeGen;
using Avalonia.TestRecorder.Selectors;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace Avalonia.TestRecorder;

/// <summary>
/// Implementation of recorder session.
/// </summary>
internal sealed class RecorderSession : IRecorderSession
{
    private readonly Window _window;
    private readonly RecorderOptions _options;
    private readonly SelectorResolver _selectorResolver;
    private readonly TestCodeGenerator _codeGenerator;
    private readonly List<RecordedStep> _steps = new();
    private readonly List<IAssertValueExtractor> _extractors;
    private readonly ILogger? _logger;
    private RecorderState _state = RecorderState.Off;
    private readonly System.Timers.Timer? _textInputTimer;
    private Control? _lastTextControl;
    private string _accumulatedText = string.Empty;

    public RecorderState State => _state;

    public RecorderSession(Window window, RecorderOptions options)
    {
        _window = window;
        _options = options;
        _logger = options.Logger;
        _selectorResolver = new SelectorResolver(options.Selector, _logger);
        
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
        _codeGenerator = new TestCodeGenerator(options.Codegen, appName);

        // Initialize extractors
        _extractors = new List<IAssertValueExtractor>(BuiltInExtractors.GetDefault());
        foreach (var extractor in options.AssertExtractors)
        {
            _extractors.Add(extractor);
        }

        // Text input coalescing timer
        _textInputTimer = new System.Timers.Timer(500); // 500ms debounce
        _textInputTimer.Elapsed += (s, e) => FlushTextInput();
        _textInputTimer.AutoReset = false;

        AttachEventHandlers();
        _logger?.LogInformation("RecorderSession initialized for window: {Window}", window.Title);
    }

    public void Start()
    {
        if (_state == RecorderState.Off)
        {
            _state = RecorderState.Recording;
            _logger?.LogInformation("Recording started");
        }
    }

    public void Stop()
    {
        if (_state != RecorderState.Off)
        {
            FlushTextInput();
            _state = RecorderState.Off;
            _steps.Clear();
            _logger?.LogInformation("Recording stopped, steps cleared");
        }
    }

    public void Pause()
    {
        if (_state == RecorderState.Recording)
        {
            FlushTextInput();
            _state = RecorderState.Paused;
            _logger?.LogInformation("Recording paused");
        }
    }

    public void Resume()
    {
        if (_state == RecorderState.Paused)
        {
            _state = RecorderState.Recording;
            _logger?.LogInformation("Recording resumed");
        }
    }

    public string SaveTestToFile()
    {
        FlushTextInput();
        var code = ExportTestCode();
        
        var outputDir = _options.OutputDirectory 
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecordedTests");
        Directory.CreateDirectory(outputDir);

        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{appName}.{_options.ScenarioName}.{timestamp}.g.cs";
        var filePath = Path.Combine(outputDir, fileName);

        File.WriteAllText(filePath, code);
        _logger?.LogInformation("Test saved to: {FilePath}", filePath);

        return filePath;
    }

    public string ExportTestCode()
    {
        FlushTextInput();
        return _codeGenerator.Generate(_steps, _options.ScenarioName);
    }

    public void Dispose()
    {
        DetachEventHandlers();
        _textInputTimer?.Dispose();
        _logger?.LogInformation("RecorderSession disposed");
    }

    private void AttachEventHandlers()
    {
        _window.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void DetachEventHandlers()
    {
        _window.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _window.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
        _window.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_state != RecorderState.Recording)
            return;

        FlushTextInput(); // Flush any pending text before recording click

        var control = e.Source as Control;
        if (control == null)
            return;

        var (selector, quality, warning) = _selectorResolver.Resolve(control);
        
        var stepType = e.GetCurrentPoint(_window).Properties.IsRightButtonPressed
            ? StepType.RightClick
            : StepType.Click;

        var step = new RecordedStep
        {
            Type = stepType,
            Selector = selector,
            Quality = quality,
            Warning = warning
        };

        _steps.Add(step);
        _logger?.LogDebug("Recorded {StepType}: {Selector}", stepType, selector);
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_state != RecorderState.Recording || string.IsNullOrEmpty(e.Text))
            return;

        var control = e.Source as Control;
        if (control == null)
            return;

        // Accumulate text input for same control
        if (_lastTextControl != control)
        {
            FlushTextInput();
            _lastTextControl = control;
        }

        _accumulatedText += e.Text;
        _textInputTimer?.Stop();
        _textInputTimer?.Start(); // Reset timer
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_state != RecorderState.Recording)
            return;

        // Handle hotkeys
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            switch (e.Key)
            {
                case Key.R: // Start/Stop
                    if (_state == RecorderState.Recording)
                        Stop();
                    else
                        Start();
                    e.Handled = true;
                    return;

                case Key.P: // Pause/Resume
                    if (_state == RecorderState.Recording)
                        Pause();
                    else if (_state == RecorderState.Paused)
                        Resume();
                    e.Handled = true;
                    return;

                case Key.S: // Save
                    var path = SaveTestToFile();
                    Debug.WriteLine($"Test saved to: {path}");
                    e.Handled = true;
                    return;

                case Key.A: // Capture Assert
                    CaptureAssert();
                    e.Handled = true;
                    return;
            }
        }

        // Record special keys (Enter, Tab, etc.)
        if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape)
        {
            FlushTextInput();
            
            var step = new RecordedStep
            {
                Type = StepType.KeyPress,
                Selector = "",
                Parameter = e.Key.ToString()
            };
            _steps.Add(step);
            _logger?.LogDebug("Recorded KeyPress: {Key}", e.Key);
        }
    }

    private void FlushTextInput()
    {
        if (_lastTextControl != null && !string.IsNullOrEmpty(_accumulatedText))
        {
            var (selector, quality, warning) = _selectorResolver.Resolve(_lastTextControl);
            
            var step = new RecordedStep
            {
                Type = StepType.TypeText,
                Selector = selector,
                Parameter = _accumulatedText,
                Quality = quality,
                Warning = warning
            };

            _steps.Add(step);
            _logger?.LogDebug("Recorded TypeText: {Selector} = {Text}", selector, _accumulatedText);

            _accumulatedText = string.Empty;
            _lastTextControl = null;
        }
    }

    private void CaptureAssert()
    {
        // Try to get control under mouse or focused control
        var control = GetTargetControl();
        if (control == null)
        {
            _logger?.LogWarning("No control found for assertion capture");
            return;
        }

        var (selector, quality, warning) = _selectorResolver.Resolve(control);

        // Try extractors
        foreach (var extractor in _extractors)
        {
            if (extractor.TryExtract(control, out var step) && step != null)
            {
                // Create new step with updated selector info
                var updatedStep = new RecordedStep
                {
                    Type = step.Type,
                    Selector = selector,
                    Parameter = step.Parameter,
                    Quality = quality,
                    Warning = warning
                };
                _steps.Add(updatedStep);
                _logger?.LogInformation("Captured assertion: {Type} on {Selector}", updatedStep.Type, selector);
                return;
            }
        }

        _logger?.LogWarning("No assertion extractor matched control type: {Type}", control.GetType().Name);
    }

    private Control? GetTargetControl()
    {
        // Try focused control first
        var focused = TopLevel.GetTopLevel(_window)?.FocusManager?.GetFocusedElement() as Control;
        if (focused != null)
            return focused;

        // Fallback: try to get control under mouse pointer
        // Note: This is simplified; real implementation would need pointer position
        return null;
    }
}
