using Avalonia.Controls;
using Avalonia.TestRecorder.UI;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.Logging;

namespace Avalonia.TestRecorder;

/// <summary>
/// Main entry point for attaching test recorder to Avalonia windows.
/// </summary>
public static class TestRecorder
{
    private static readonly Dictionary<Window, IRecorderSession> _sessions = new();
    private static readonly Dictionary<Window, Window> _overlayWindows = new();

    /// <summary>
    /// Attaches a test recorder to the specified window.
    /// </summary>
    /// <param name="window">The window to record interactions from.</param>
    /// <param name="options">Optional recorder configuration.</param>
    /// <returns>A recorder session control interface.</returns>
    /// <exception cref="ArgumentNullException">Thrown when window is null.</exception>
    public static IRecorderSession Attach(Window window, RecorderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Idempotent: return existing session if already attached
        if (_sessions.TryGetValue(window, out var existingSession))
        {
            return existingSession;
        }

        options ??= new RecorderOptions();
        var session = new RecorderSession(window, options);
        _sessions[window] = session;

        // Add overlay panel if enabled
        if (options.ShowOverlay)
        {
            AttachOverlay(window, session, options);
        }

        // Cleanup on window close
        window.Closed += (s, e) =>
        {
            if (_sessions.Remove(window))
            {
                session.Dispose();
            }
            
            // Close overlay window if exists
            if (_overlayWindows.TryGetValue(window, out var overlayWindow))
            {
                _overlayWindows.Remove(window);
                overlayWindow.Close();
            }
        };

        return session;
    }

    private static void AttachOverlay(Window window, RecorderSession session, RecorderOptions options)
    {
        // Create overlay
        var overlay = new RecorderOverlay();
        overlay.AttachSession(session, options.Logger);

        // Apply theme if specified
        if (options.OverlayTheme.HasValue)
        {
            overlay.SetTheme(options.OverlayTheme.Value == Avalonia.TestRecorder.OverlayTheme.Dark);
        }

        // Create separate window for overlay
        var overlayWindow = new Window
        {
            Width = 200,
            Height = 40,
            CanResize = false,
            ShowInTaskbar = false,
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Background = Brushes.Transparent,
            Content = overlay,
            Topmost = true,
            Title = "Test Recorder"
        };

        // Position overlay at center-top of main window
        void PositionOverlay()
        {
            if (window.WindowState != WindowState.Minimized)
            {
                var mainWindowBounds = new PixelRect(
                    window.Position,
                    PixelSize.FromSize(new Size(window.Width, window.Height), 1.0));

                var x = mainWindowBounds.X + (mainWindowBounds.Width - (int)overlayWindow.Width) / 2;
                var y = mainWindowBounds.Y;

                overlayWindow.Position = new PixelPoint(x, y);
            }
        }

        // Track main window position and state changes
        window.PositionChanged += (s, e) => PositionOverlay();
        window.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Window.WindowState))
            {
                overlayWindow.IsVisible = window.WindowState != WindowState.Minimized;
                if (window.WindowState != WindowState.Minimized)
                {
                    PositionOverlay();
                }
            }
        };

        overlayWindow.Show();
        PositionOverlay();

        _overlayWindows[window] = overlayWindow;
        
        options.Logger?.LogDebug("Overlay attached as separate window");
    }
}