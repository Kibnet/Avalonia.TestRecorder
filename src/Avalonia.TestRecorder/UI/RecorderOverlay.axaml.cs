using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    // UI Elements
    private Shape? _iconOff;
    private Shape? _iconRecording;
    private Shape? _iconPaused;
    private TextBlock? _stepCounter;
    private Button? _recordButton;
    private Button? _pauseButton;
    private Button? _clearButton;
    private Button? _saveButton;
    private Button? _closeButton;
    private Shape? _playIcon;
    private Shape? _stopIcon;

    public RecorderOverlay()
    {
        InitializeComponent();
        InitializeControls();
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
        _closeButton = this.FindControl<Button>("CloseButton");
        _playIcon = this.FindControl<Shape>("PlayIcon");
        _stopIcon = this.FindControl<Shape>("StopIcon");

        // Attach event handlers
        if (_recordButton != null)
            _recordButton.Click += OnRecordButtonClick;
        
        if (_pauseButton != null)
            _pauseButton.Click += OnPauseButtonClick;
        
        if (_clearButton != null)
            _clearButton.Click += OnClearButtonClick;
        
        if (_saveButton != null)
            _saveButton.Click += OnSaveButtonClick;
        
        if (_closeButton != null)
            _closeButton.Click += OnCloseButtonClick;

        // Setup update timer
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += OnUpdateTimer;
        _updateTimer.Start();
    }

    public void AttachSession(RecorderSession session, ILogger? logger = null)
    {
        _session = session;
        _logger = logger;
        UpdateUI();
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
            _stepCounter.Text = count == 1 ? "1 step" : $"{count} steps";
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

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
        _logger?.LogInformation("Overlay hidden");
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
        
        if (_closeButton != null)
            _closeButton.Click -= OnCloseButtonClick;
    }
}
