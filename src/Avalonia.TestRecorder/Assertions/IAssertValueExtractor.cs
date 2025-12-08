using Avalonia.Controls;

namespace Avalonia.TestRecorder.Assertions;

/// <summary>
/// Interface for extracting assertion values from controls.
/// </summary>
public interface IAssertValueExtractor
{
    /// <summary>
    /// Attempts to extract an assertion from the specified control.
    /// </summary>
    /// <param name="control">The control to extract value from.</param>
    /// <param name="step">The generated recorded step if extraction succeeded.</param>
    /// <returns>True if extraction was successful, false otherwise.</returns>
    bool TryExtract(Control control, out RecordedStep? step);
}
