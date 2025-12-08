using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

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
    private Shape? _iconOff;
    private Shape? _iconRecording;
    private Shape? _iconPaused;
    private TextBlock? _stepCounter;
    private Button? _recordButton;
    private Button? _pauseButton;
    private Button? _clearButton;
    private Button? _saveButton;
    private Button? _minimizeButton;
    private Button? _restoreButton;
    private Shape? _playIcon;
    private Shape? _stopIcon;
    private StackPanel? _expandedPanel;
    private StackPanel? _minimizedPanel;

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
        _iconOff = this.FindControl<Shape>("IconOff");
        _iconRecording = this.FindControl<Shape>("IconRecording");
        _iconPaused = this.FindControl<Shape>("IconPaused");
        _stepCounter = this.FindControl<TextBlock>("StepCounter");
        _recordButton = this.FindControl<Button>("RecordButton");
        _pauseButton = this.FindControl<Button>("PauseButton");
        _clearButton = this.FindControl<Button>("ClearButton");
        _saveButton = this.FindControl<Button>("SaveButton");
        _minimizeButton = this.FindControl<Button>("MinimizeButton");
        _restoreButton = this.FindControl<Button>("RestoreButton");
        _playIcon = this.FindControl<Shape>("PlayIcon");
        _stopIcon = this.FindControl<Shape>("StopIcon");
        _expandedPanel = this.FindControl<StackPanel>("ExpandedPanel");
        _minimizedPanel = this.FindControl<StackPanel>("MinimizedPanel");

        // Attach event handlers
        if (_recordButton != null)
            _recordButton.Click += OnRecordButtonClick;
        
        if (_pauseButton != null)
            _pauseButton.Click += OnPauseButtonClick;
        
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
        UpdateUI();
    }

    /// <summary>
    /// Manually sets the overlay theme.
    /// </summary>
    /// <param name="isDark">True for dark theme, false for light theme.</param>
    public void SetTheme(bool isDark)
    {
        Classes.Remove("dark-theme");
        if (isDark)
        {
            Classes.Add("dark-theme");
        }
        _logger?.LogDebug("Overlay theme manually set to: {Theme}", isDark ? "Dark" : "Light");
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
            
        // Update parent window size
        if (this.Parent is Window window)
        {
            window.Height = 30;
        }
        
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
            
        // Update parent window size
        if (this.Parent is Window window)
        {
            window.Height = 40;
        }
        
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

        // Update status icons
        if (_iconOff != null && _iconRecording != null && _iconPaused != null)
        {
            _iconOff.IsVisible = _session.State == RecorderState.Off;
            _iconRecording.IsVisible = _session.State == RecorderState.Recording;
            _iconPaused.IsVisible = _session.State == RecorderState.Paused;
        }

        // Update step counter
        if (_stepCounter != null)
        {
            var count = _session.GetStepCount();
            _stepCounter.Text = $"{count}";
        }

        // Update record button icon
        if (_playIcon != null && _stopIcon != null)
        {
            _playIcon.IsVisible = _session.State != RecorderState.Recording;
            _stopIcon.IsVisible = _session.State == RecorderState.Recording;
        }

        // Update button enabled states
        if (_pauseButton != null)
        {
            _pauseButton.IsEnabled = _session.State == RecorderState.Recording || _session.State == RecorderState.Paused;
        }

        if (_clearButton != null)
        {
            _clearButton.IsEnabled = _session.GetStepCount() > 0;
        }

        if (_saveButton != null)
        {
            _saveButton.IsEnabled = _session.GetStepCount() > 0;
        }
    }

    private void OnRecordButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        if (_session.State == RecorderState.Recording)
        {
            _session.Stop();
            _logger?.LogInformation("Recording stopped via overlay");
        }
        else
        {
            _session.Start();
            _logger?.LogInformation("Recording started via overlay");
        }

        UpdateUI();
    }

    private void OnPauseButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        if (_session.State == RecorderState.Recording)
        {
            _session.Pause();
            _logger?.LogInformation("Recording paused via overlay");
        }
        else if (_session.State == RecorderState.Paused)
        {
            _session.Resume();
            _logger?.LogInformation("Recording resumed via overlay");
        }

        UpdateUI();
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        _session.ClearSteps();
        _logger?.LogInformation("Steps cleared via overlay");
        UpdateUI();
    }

    private async void OnSaveButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_session == null)
            return;

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window == null)
            return;

        try
        {
            // Show save file dialog
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Test Code",
                SuggestedFileName = _session.GetSuggestedFileName(),
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
                _session.SaveTestToFile(filePath);
                _logger?.LogInformation("Test saved to: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving test file");
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        _updateTimer?.Stop();
        
        // Detach event handlers
        if (_recordButton != null)
            _recordButton.Click -= OnRecordButtonClick;
        
        if (_pauseButton != null)
            _pauseButton.Click -= OnPauseButtonClick;
        
        if (_clearButton != null)
            _clearButton.Click -= OnClearButtonClick;
        
        if (_saveButton != null)
            _saveButton.Click -= OnSaveButtonClick;
        
        if (_minimizeButton != null)
            _minimizeButton.Click -= OnMinimizeButtonClick;
            
        if (_restoreButton != null)
            _restoreButton.Click -= OnRestoreButtonClick;
    }
}