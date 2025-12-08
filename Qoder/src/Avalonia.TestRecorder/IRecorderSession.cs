using System.Threading.Tasks;

namespace Avalonia.TestRecorder;

/// <summary>
/// Represents an active test recording session.
/// </summary>
public interface IRecorderSession : IDisposable
{
    /// <summary>
    /// Gets the current state of the recorder.
    /// </summary>
    RecorderState State { get; }

    /// <summary>
    /// Starts or resumes recording.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops recording and clears recorded steps.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses recording without clearing steps.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes recording from paused state.
    /// </summary>
    void Resume();

    /// <summary>
    /// Saves the recorded test to a file.
    /// </summary>
    /// <returns>The path to the generated test file.</returns>
    /// <exception cref="IOException">Thrown when file I/O fails.</exception>
    string SaveTestToFile();

    /// <summary>
    /// Exports the test code as a string (for clipboard copy).
    /// </summary>
    /// <returns>The generated C# test code.</returns>
    string ExportTestCode();
    
    /// <summary>
    /// Saves the test code to a file selected by the user via dialog.
    /// </summary>
    /// <returns>The path to the saved file, or null if cancelled.</returns>
    Task<string?> SaveTestToFileWithDialog();
}

/// <summary>
/// Recording session states.
/// </summary>
public enum RecorderState
{
    /// <summary>
    /// Recorder is not active.
    /// </summary>
    Off,

    /// <summary>
    /// Actively recording interactions.
    /// </summary>
    Recording,

    /// <summary>
    /// Recording paused, steps preserved.
    /// </summary>
    Paused
}