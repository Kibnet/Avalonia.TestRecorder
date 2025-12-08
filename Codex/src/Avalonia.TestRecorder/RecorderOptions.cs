using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;

namespace Avalonia.TestRecorder;

/// <summary>
/// Options controlling recorder lifecycle and output.
/// </summary>
public sealed class RecorderOptions
{
    public string? OutputDirectory { get; init; }
    public string ScenarioName { get; init; } = "Scenario";
    public RecorderHotkeys Hotkeys { get; init; } = RecorderHotkeys.Default;
    public SelectorOptions Selector { get; init; } = new();
    public CodegenOptions Codegen { get; init; } = new();
    public IList<IAssertValueExtractor> AssertExtractors { get; } = new List<IAssertValueExtractor>();
    public bool StartImmediately { get; init; }
    public bool EnableOverlay { get; init; } = true;
    public bool EnableLogging { get; init; } = true;
    public ILogger? Logger { get; init; }
}

/// <summary>
/// Hotkey configuration for recorder commands.
/// </summary>
public sealed class RecorderHotkeys
{
    public KeyGesture StartStop { get; init; } = new(Key.R, KeyModifiers.Control | KeyModifiers.Shift);
    public KeyGesture PauseResume { get; init; } = new(Key.P, KeyModifiers.Control | KeyModifiers.Shift);
    public KeyGesture Save { get; init; } = new(Key.S, KeyModifiers.Control | KeyModifiers.Shift);
    public KeyGesture CaptureAssert { get; init; } = new(Key.A, KeyModifiers.Control | KeyModifiers.Shift);

    public static RecorderHotkeys Default => new();

    public bool Matches(KeyEventArgs args, KeyGesture gesture)
    {
        if (args is null)
        {
            return false;
        }

        var expectedModifiers = gesture.KeyModifiers;
        var actualModifiers = args.KeyModifiers;
        return args.Key == gesture.Key && (actualModifiers & expectedModifiers) == expectedModifiers;
    }
}

/// <summary>
/// Selector lookup preferences.
/// </summary>
public sealed class SelectorOptions
{
    public bool PreferFocusedElementForAssert { get; init; } = true;
    public bool IncludeNameFallback { get; init; } = true;
    public bool IncludeTreePathFallback { get; init; } = true;
    public bool IncludeCoordinateFallback { get; init; } = true;
}

/// <summary>
/// Code generation settings.
/// </summary>
public sealed class CodegenOptions
{
    public string TestNamespace { get; init; } = "RecordedTests";
    public string ClassPrefix { get; init; } = "Recorded";
    public TestFramework TestFramework { get; init; } = TestFramework.Xunit;
    public ITestTemplateProvider TemplateProvider { get; init; } = new DefaultTemplateProvider();
}

public enum TestFramework
{
    Xunit,
    NUnit
}

/// <summary>
/// Allows plugging custom assert capture.
/// </summary>
public interface IAssertValueExtractor
{
    bool TryExtract(Control control, out RecordedAssert assert);
}

public sealed record RecordedAssert(AssertKind Kind, string? Value = null, bool? BoolValue = null);

/// <summary>
/// Entry point for attaching recorder to a window.
/// </summary>
public static class TestRecorder
{
    public static IRecorderSession Attach(Window window, RecorderOptions? options = null)
    {
        if (window is null)
        {
            throw new ArgumentNullException(nameof(window));
        }

        options ??= new RecorderOptions();
        if (!options.AssertExtractors.Any())
        {
            DefaultAssertExtractors.Register(options.AssertExtractors);
        }

        var session = new RecorderSession(window, options);
        if (ShouldStart(options))
        {
            session.Start();
        }

        return session;
    }

    private static bool ShouldStart(RecorderOptions options)
    {
        if (options.StartImmediately)
        {
            return true;
        }

        var env = Environment.GetEnvironmentVariable("AV_RECORDER");
        if (!string.IsNullOrEmpty(env) && env != "0")
        {
            return true;
        }

        var args = Environment.GetCommandLineArgs();
        return args.Any(a => a.Equals("--record-tests", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Recorder session contract used by applications to control recording lifecycle.
/// </summary>
public interface IRecorderSession : IDisposable
{
    RecorderState State { get; }
    IReadOnlyList<RecordedStep> Steps { get; }

    void Start();
    void Stop();
    void Pause();
    void Resume();

    string SaveTestToFile();
    string ExportTestCode();
}

public interface ITestTemplateProvider
{
    string Render(RecordedTest test, RecorderOptions options);
}
