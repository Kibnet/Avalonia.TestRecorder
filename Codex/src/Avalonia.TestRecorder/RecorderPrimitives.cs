using Avalonia;
using Avalonia.Input;

namespace Avalonia.TestRecorder;

/// <summary>
/// Current state of the recorder runtime.
/// </summary>
public enum RecorderState
{
    Off = 0,
    Recording,
    Paused
}

/// <summary>
/// Kind of the captured step for later code generation.
/// </summary>
public enum RecordedStepKind
{
    Click,
    DoubleClick,
    RightClick,
    Hover,
    Scroll,
    TextInput,
    KeyPress,
    AssertText,
    AssertToggle,
    AssertVisible,
    AssertEnabled
}

/// <summary>
/// Selector quality for diagnostics and fallbacks.
/// </summary>
public enum SelectorKind
{
    AutomationId,
    Name,
    VisualTreePath,
    Coordinates
}

/// <summary>
/// Result of assert capture.
/// </summary>
public enum AssertKind
{
    Text,
    Toggle,
    Visible,
    Enabled
}

/// <summary>
/// Selector used for locating controls in generated tests.
/// </summary>
public sealed record TargetSelector(string Value, SelectorKind Kind, string? Diagnostic = null)
{
    public bool IsStable => Kind == SelectorKind.AutomationId;
}

/// <summary>
/// Representation of a single recorded step.
/// </summary>
public sealed record RecordedStep
{
    public RecordedStepKind Kind { get; init; }
    public TargetSelector? Target { get; init; }
    public string? Text { get; init; }
    public string? Key { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public Vector? ScrollDelta { get; init; }
    public string? Expected { get; init; }
    public bool? ExpectedBool { get; init; }
    public string? Warning { get; init; }
}
