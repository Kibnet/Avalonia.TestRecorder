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
public sealed class RecorderSession : IRecorderSession
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
    private Point _lastPointerPosition;
    private Control? _lastHoveredControl;
    
    // Callback for showing save dialog
    private Func<Task<string?>>? _showSaveDialogCallback;

    public RecorderState State => _state;

    /// <summary>
    /// Gets the current step count.
    /// </summary>
    public int GetStepCount() => _steps.Count;

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
            _logger?.LogInformation("Recording stopped");
        }
    }

    /// <summary>
    /// Clears all recorded steps.
    /// </summary>
    public void ClearSteps()
    {
        FlushTextInput();
        _steps.Clear();
        _logger?.LogInformation("Steps cleared");
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
        var outputDir = _options.OutputDirectory 
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RecordedTests");
        Directory.CreateDirectory(outputDir);

        var fileName = GetSuggestedFileName();
        var filePath = Path.Combine(outputDir, fileName);

        return SaveTestToFile(filePath);
    }

    /// <summary>
    /// Saves the test code to the specified file path.
    /// </summary>
    public string SaveTestToFile(string filePath)
    {
        FlushTextInput();
        var code = ExportTestCode();
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, code);
        _logger?.LogInformation("Test saved to: {FilePath}", filePath);

        return filePath;
    }

    /// <summary>
    /// Gets the suggested file name for the test.
    /// </summary>
    public string GetSuggestedFileName()
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{appName}.{_options.ScenarioName}.{timestamp}.g.cs";
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
        _window.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        _window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void DetachEventHandlers()
    {
        _window.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        _window.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
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

        // Update pointer position and hovered control
        _lastPointerPosition = e.GetPosition(_window);
        _lastHoveredControl = control;

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

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_state != RecorderState.Recording)
            return;

        // Update last known pointer position and control under pointer
        _lastPointerPosition = e.GetPosition(_window);
        
        // Store the control under pointer for assertion capture
        if (e.Source is Control control)
        {
            _lastHoveredControl = control;
        }
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
                    // Instead of directly saving, show the save dialog
                    _ = Task.Run(async () =>
                    {
                        var path = await SaveTestToFileWithDialog();
                        if (path != null)
                        {
                            Debug.WriteLine($"Test saved to: {path}");
                        }
                    });
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
        // Priority 1: Control under mouse pointer (most recent hover)
        if (_lastHoveredControl != null)
        {
            _logger?.LogDebug("Using hovered control for assertion: {Type}", _lastHoveredControl.GetType().Name);
            return _lastHoveredControl;
        }

        // Priority 2: Try to find control at last pointer position using hit testing
        var controlAtPointer = FindControlAtPosition(_lastPointerPosition);
        if (controlAtPointer != null)
        {
            _logger?.LogDebug("Found control at pointer position: {Type}", controlAtPointer.GetType().Name);
            return controlAtPointer;
        }

        // Priority 3: Focused control as fallback
        var focused = TopLevel.GetTopLevel(_window)?.FocusManager?.GetFocusedElement() as Control;
        if (focused != null)
        {
            _logger?.LogDebug("Using focused control for assertion: {Type}", focused.GetType().Name);
            return focused;
        }

        _logger?.LogWarning("No target control found for assertion capture");
        return null;
    }

    private Control? FindControlAtPosition(Point position)
    {
        try
        {
            // Perform hit testing to find control at the given position
            var hitTestResult = _window.InputHitTest(position);
            
            if (hitTestResult is Control control)
            {
                return control;
            }
            
            // If hit test returns a visual that's not a control, walk up to find the parent control
            if (hitTestResult is Visual visual)
            {
                var parent = visual.GetVisualParent();
                while (parent != null)
                {
                    if (parent is Control parentControl)
                        return parentControl;
                    parent = parent.GetVisualParent();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding control at position {Position}", position);
        }
        
        return null;
    }

    /// <summary>
    /// Sets a callback function for showing the save file dialog.
    /// This is used to enable the keyboard shortcut to show the save dialog.
    /// </summary>
    /// <param name="callback">Function that shows the save dialog and returns the selected file path, or null if cancelled.</param>
    public void SetSaveDialogCallback(Func<Task<string?>> callback)
    {
        _showSaveDialogCallback = callback;
    }

    /// <summary>
    /// Saves the test code to a file selected by the user via dialog.
    /// </summary>
    /// <returns>The path to the saved file, or null if cancelled.</returns>
    public async Task<string?> SaveTestToFileWithDialog()
    {
        if (_showSaveDialogCallback != null)
        {
            var filePath = await _showSaveDialogCallback();
            if (!string.IsNullOrEmpty(filePath))
            {
                return SaveTestToFile(filePath);
            }
        }
        else
        {
            // Fallback to default behavior if no callback is set
            return SaveTestToFile();
        }
        
        return null;
    }
}
