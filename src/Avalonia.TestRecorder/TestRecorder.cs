using Avalonia.Controls;

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
}
