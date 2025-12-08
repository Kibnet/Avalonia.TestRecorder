using System.Collections.ObjectModel;

namespace Avalonia.TestRecorder;

/// <summary>
/// Snapshot of the recorded scenario used for code generation.
/// </summary>
public sealed class RecordedTest
{
    public RecordedTest(
        string appName,
        string scenarioName,
        string applicationTypeName,
        string windowTypeName,
        IReadOnlyList<RecordedStep> steps,
        DateTimeOffset recordedAt)
    {
        AppName = appName;
        ScenarioName = scenarioName;
        ApplicationTypeName = applicationTypeName;
        WindowTypeName = windowTypeName;
        RecordedAt = recordedAt;
        Steps = new ReadOnlyCollection<RecordedStep>(steps.ToList());
    }

    public string AppName { get; }
    public string ScenarioName { get; }
    public string ApplicationTypeName { get; }
    public string WindowTypeName { get; }
    public DateTimeOffset RecordedAt { get; }
    public IReadOnlyList<RecordedStep> Steps { get; }
}
