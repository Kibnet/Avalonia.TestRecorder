using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Avalonia.TestRecorder.UI;

/// <summary>
/// Recorder overlay panel that shows recording status and controls.
/// </summary>
public partial class RecorderOverlay : UserControl
{
    private RecorderSession? _session;
    private ILogger? _logger;
    private DispatcherTimer? _updateTimer;
    private bool _isMinimized = false;
    
    // UI Elements
    private TextBlock? _stepCounter;
    private Button? _recordButton;
    // Removed _pauseButton field since we're combining functionality
    private Button? _clearButton;
    private Button? _saveButton;
    private Button? _minimizeButton;
    private Button? _restoreButton;
    private Shape? _playIcon;
    private Shape? _stopIcon;
    private StackPanel? _expandedPanel;
    private StackPanel? _minimizedPanel;
    private TextBlock? _statusBarText;
    private TextBlock? _minimizedStatusBarText;
    private int _previousStepCount = 0;

    public RecorderOverlay()
    {
        InitializeComponent();
        InitializeControls();
        DetectAndApplyTheme();
    }

    private void InitializeComponent()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        // Get UI elements
        _stepCounter = this.FindControl<TextBlock>("StepCounter");
        _recordButton = this.FindControl<Button>("RecordButton");
        // Removed _pauseButton reference since we're combining functionality
        _clearButton = this.FindControl<Button>("ClearButton");
        _saveButton = this.FindControl<Button>("SaveButton");
        _minimizeButton = this.FindControl<Button>("MinimizeButton");
        _restoreButton = this.FindControl<Button>("RestoreButton");
        _playIcon = this.FindControl<Shape>("PlayIcon");
        _stopIcon = this.FindControl<Shape>("StopIcon");
        _expandedPanel = this.FindControl<StackPanel>("ExpandedPanel");
        _minimizedPanel = this.FindControl<StackPanel>("MinimizedPanel");
        _statusBarText = this.FindControl<TextBlock>("StatusBarText");
        _minimizedStatusBarText = this.FindControl<TextBlock>("MinimizedStatusBarText");

        // Attach event handlers
        if (_recordButton != null)
            _recordButton.Click += OnRecordButtonClick;
        
        // Removed _pauseButton handler since we're combining functionality
        
        if (_clearButton != null)
            _clearButton.Click += OnClearButtonClick;
        
        if (_saveButton != null)
            _saveButton.Click += OnSaveButtonClick;
        
        if (_minimizeButton != null)
            _minimizeButton.Click += OnMinimizeButtonClick;
            
        if (_restoreButton != null)
            _restoreButton.Click += OnRestoreButtonClick;

        // Setup update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += OnUpdateTimer;
        _updateTimer.Start();
    }

    private void DetectAndApplyTheme()
    {
        // Try to detect theme from application
        if (Application.Current is IResourceHost host)
        {
            // Check for common theme resource keys
            if (host.TryGetResource("ThemeVariant", null, out var themeVariant))
            {
                var themeName = themeVariant?.ToString();
                if (themeName != null && themeName.Contains("Dark", StringComparison.OrdinalIgnoreCase))
                {
                    Classes.Add("dark-theme");
                    _logger?.LogDebug("Applied dark theme to overlay");
                    return;
                }
            }

            // Check for dark background color as fallback
            if (host.TryGetResource("SystemControlBackgroundAltHighBrush", null, out var bgBrush) ||
                host.TryGetResource("Background", null, out bgBrush))
            {
                if (bgBrush is ISolidColorBrush solidBrush)
                {
                    var color = solidBrush.Color;
                    // Consider it dark if luminance is low
                    var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
                    if (luminance < 0.5)
                    {
                        Classes.Add("dark-theme");
                        _logger?.LogDebug("Applied dark theme to overlay (detected from background)");
                        return;
                    }
                }
            }
        }

        // Default to light theme (no class needed)
        _logger?.LogDebug("Applied light theme to overlay (default)");
    }

    public void AttachSession(RecorderSession session, ILogger? logger = null)
    {
        _session = session;
        _logger = logger;
        
        // Set the callback for showing the save dialog
        _session.SetSaveDialogCallback(ShowSaveFileDialog);
        
        // Set the callback for Clear action via keyboard shortcut
        _session.SetClearCallback(OnClearShortcut);
        
        // Set the callback for Minimize/Restore action via keyboard shortcut
        _session.SetMinimizeRestoreCallback(OnMinimizeRestoreShortcut);
        
        UpdateUI();
    }

    private void OnClearShortcut()
    {
        Dispatcher.UIThread.Post(() =>
        {
            SetStatusBarText(""); // Clear status bar
            _logger?.LogInformation("Steps cleared via keyboard shortcut");
            UpdateUI();
        });
    }

    private void OnMinimizeRestoreShortcut()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_isMinimized)
                Restore();
            else
                Minimize();
        });
    }

    public void SetTheme(bool isDark)
    {
        if (isDark)
        {
            Classes.Add("dark-theme");
        }
        else
        {
            Classes.Remove("dark-theme");
        }
    }

    /// <summary>
    /// Minimizes the overlay to a small button.
    /// </summary>
    public void Minimize()
    {
        if (_isMinimized) return;
        
        _isMinimized = true;
        Classes.Add("minimized");
        
        // Hide expanded panel and show minimized panel
        if (_expandedPanel != null)
            _expandedPanel.IsVisible = false;
            
        if (_minimizedPanel != null)
            _minimizedPanel.IsVisible = true;
            
        _logger?.LogInformation("Overlay minimized");
    }
    
    /// <summary>
    /// Restores the overlay to its full size.
    /// </summary>
    public void Restore()
    {
        if (!_isMinimized) return;
        
        _isMinimized = false;
        Classes.Remove("minimized");
        
        // Show expanded panel and hide minimized panel
        if (_expandedPanel != null)
            _expandedPanel.IsVisible = true;
            
        if (_minimizedPanel != null)
            _minimizedPanel.IsVisible = false;
        
        _logger?.LogInformation("Overlay restored");
    }

    private void OnUpdateTimer(object? sender, EventArgs e)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_session == null)
            return;
        
        // Update step counter
        if (_stepCounter != null)
        {
            var count = _session.GetStepCount();
            _stepCounter.Text = $"{count}";
            
            // Check if a new step was added (works in both Recording and Paused states)
            if (count > _previousStepCount && (_session.State == RecorderState.Recording ))
            {
                ShowLastStepCode();
            }
            _previousStepCount = count;
        }

        // Update record button icon based on state
        if (_playIcon != null && _stopIcon != null)
        {
            // Show play icon when not recording
            _playIcon.IsVisible = _session.State == RecorderState.Off;
            
            // Show stop icon when recording
            _stopIcon.IsVisible = _session.State == RecorderState.Recording;
        }

        // Update button enabled states
        // The record button is always enabled now since it handles all states
    }

    /// <summary>
    /// Shows the code for the last recorded step in the status bar.
    /// </summary>
    private void ShowLastStepCode()
    {
        if (_session == null)
            return;

        var steps = GetSessionSteps(_session);
        if (steps.Count > 0)
        {
            var lastStep = steps[steps.Count - 1];
            var code = GenerateStepCode(lastStep);
            
            SetStatusBarText(code);
        }
    }

    /// <summary>
    /// Gets the list of recorded steps from the session.
    /// </summary>
    private List<RecordedStep> GetSessionSteps(RecorderSession session)
    {
        // Since _steps is private in RecorderSession, we'll use reflection to access it
        var stepsField = typeof(RecorderSession).GetField("_steps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (stepsField != null)
        {
            return (List<RecordedStep>)stepsField.GetValue(session)!;
        }
        return new List<RecordedStep>();
    }

    /// <summary>
    /// Generates the code representation for a recorded step.
    /// </summary>
    private string GenerateStepCode(RecordedStep step)
    {
        var warning = step.Warning != null ? $" // {step.Warning}" : "";
        return step.Type switch
        {
            StepType.Click => $"ui.Click(\"{step.Selector}\");{warning}",
            StepType.RightClick => $"ui.RightClick(\"{step.Selector}\");{warning}",
            StepType.DoubleClick => $"ui.DoubleClick(\"{step.Selector}\");{warning}",
            StepType.TypeText => $"ui.TypeText(\"{step.Selector}\", \"{EscapeString(step.Parameter ?? "")}\");{warning}",
            StepType.KeyPress => $"ui.KeyPress(\"{step.Parameter}\");{warning}",
            StepType.Scroll => $"ui.Scroll(\"{step.Selector}\", {step.Parameter});{warning}",
            StepType.Hover => $"ui.Hover(\"{step.Selector}\");{warning}",
            StepType.AssertText => $"ui.AssertText(\"{step.Selector}\", \"{EscapeString(step.Parameter ?? "")}\");{warning}",
            StepType.AssertChecked => $"ui.AssertChecked(\"{step.Selector}\", {step.Parameter});{warning}",
            StepType.AssertVisible => $"ui.AssertVisible(\"{step.Selector}\");{warning}",
            StepType.AssertEnabled => $"ui.AssertEnabled(\"{step.Selector}\");{warning}",
            _ => $"// Unknown step type: {step.Type}"
        };
    }

    private string EscapeString(string str)
    {
        return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private void OnRecordButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        switch (_session.State)
        {
            case RecorderState.Off:
                _session.Start();
                _logger?.LogInformation("Recording started via overlay");
                break;
                
            case RecorderState.Recording:
                _session.Stop();
                _logger?.LogInformation("Recording stopped via overlay");
                break;
        }

        UpdateUI();
    }
    
    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        _session.ClearSteps();
        SetStatusBarText(""); // Clear status bar
        _logger?.LogInformation("Steps cleared via overlay");
        UpdateUI();
    }

    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        try
        {
            var filePath = await _session.SaveTestToFileWithDialog();
            if (filePath != null)
            {
                SetStatusBarText($"Saved: {System.IO.Path.GetFileName(filePath)}");
                _logger?.LogInformation("Test saved via overlay: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            SetStatusBarText($"Save failed: {ex.Message}");
            _logger?.LogError(ex, "Error saving test via overlay");
        }
    }

    private void OnMinimizeButtonClick(object? sender, RoutedEventArgs e)
    {
        Minimize();
    }

    private void OnRestoreButtonClick(object? sender, RoutedEventArgs e)
    {
        Restore();
    }

    /// <summary>
    /// Sets the status bar text in both expanded and minimized views.
    /// </summary>
    private void SetStatusBarText(string text)
    {
        if (_statusBarText != null)
            _statusBarText.Text = text;
            
        if (_minimizedStatusBarText != null)
            _minimizedStatusBarText.Text = text;
    }

    /// <summary>
    /// Shows the save file dialog and returns the selected file path.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    private async Task<string?> ShowSaveFileDialog()
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
            return null;

        try
        {
            // Show save file dialog
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Test Code",
                SuggestedFileName = _session?.GetSuggestedFileName() ?? "Test.cs",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("C# Source File")
                    {
                        Patterns = new[] { "*.cs" }
                    }
                },
                DefaultExtension = "cs"
            });

            if (file != null)
            {
                var filePath = file.Path.LocalPath;
                _session?.SaveTestToFile(filePath);
                SetStatusBarText($"Saved: {System.IO.Path.GetFileName(filePath)}");
                _logger?.LogInformation("Test saved to: {FilePath}", filePath);
                return filePath;
            }
        }
        catch (Exception ex)
        {
            SetStatusBarText($"Save failed: {ex.Message}");
            _logger?.LogError(ex, "Error saving test file");
        }
        
        return null;
    }
}