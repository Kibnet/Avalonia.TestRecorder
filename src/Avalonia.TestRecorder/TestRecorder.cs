using Avalonia.Controls;
using Avalonia.TestRecorder.UI;
using Avalonia.Layout;

namespace Avalonia.TestRecorder;

/// <summary>
/// Main entry point for attaching test recorder to Avalonia windows.
/// </summary>
public static class TestRecorder
{
    private static readonly Dictionary<Window, IRecorderSession> _sessions = new();

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
        };

        return session;
    }

    private static void AttachOverlay(Window window, RecorderSession session, RecorderOptions options)
    {
        // Create overlay
        var overlay = new RecorderOverlay();
        overlay.AttachSession(session, options.Logger);

        // Insert overlay at top of window content
        if (window.Content is Panel panel)
        {
            // If content is already a panel, add overlay to it
            var dockPanel = new DockPanel();
            DockPanel.SetDock(overlay, Dock.Top);
            
            panel.Children.Insert(0, dockPanel);
            dockPanel.Children.Add(overlay);
        }
        else if (window.Content is Control content)
        {
            // Wrap existing content in DockPanel
            var dockPanel = new DockPanel();
            DockPanel.SetDock(overlay, Dock.Top);
            
            window.Content = dockPanel;
            dockPanel.Children.Add(overlay);
            dockPanel.Children.Add(content);
        }
    }
}
