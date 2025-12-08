using System.Reflection;
using System.Text;

namespace Avalonia.TestRecorder.CodeGen;

/// <summary>
/// Generates C# test code from recorded steps.
/// </summary>
internal sealed class TestCodeGenerator
{
    private readonly CodegenOptions _options;
    private readonly string _appName;

    public TestCodeGenerator(CodegenOptions options, string appName)
    {
        _options = options;
        _appName = appName;
    }

    public string Generate(IEnumerable<RecordedStep> steps, string scenarioName)
    {
        var template = LoadTemplate();
        var className = $"Recorded_{scenarioName}_Tests";
        var methodName = _options.IncludeTimestamp
            ? $"Scenario_{scenarioName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            : $"Scenario_{scenarioName}";
        var namespaceName = _options.Namespace ?? $"{_appName}.Tests";

        var stepsCode = GenerateStepsCode(steps);

        return template
            .Replace("{Namespace}", namespaceName)
            .Replace("{ClassName}", className)
            .Replace("{TestMethod}", methodName)
            .Replace("{Steps}", stepsCode);
    }

    private string GenerateStepsCode(IEnumerable<RecordedStep> steps)
    {
        var sb = new StringBuilder();
        foreach (var step in steps)
        {
            var warning = step.Warning != null ? $" // {step.Warning}" : "";
            var line = step.Type switch
            {
                StepType.Click => $"            ui.Click(\"{step.Selector}\");{warning}",
                StepType.RightClick => $"            ui.RightClick(\"{step.Selector}\");{warning}",
                StepType.DoubleClick => $"            ui.DoubleClick(\"{step.Selector}\");{warning}",
                StepType.TypeText => $"            ui.TypeText(\"{step.Selector}\", \"{EscapeString(step.Parameter ?? "")}\");{warning}",
                StepType.KeyPress => $"            ui.KeyPress(\"{step.Parameter}\");{warning}",
                StepType.Scroll => $"            ui.Scroll(\"{step.Selector}\", {step.Parameter});{warning}",
                StepType.Hover => $"            ui.Hover(\"{step.Selector}\");{warning}",
                StepType.AssertText => $"            ui.AssertText(\"{step.Selector}\", \"{EscapeString(step.Parameter ?? "")}\");{warning}",
                StepType.AssertChecked => $"            ui.AssertChecked(\"{step.Selector}\", {step.Parameter});{warning}",
                StepType.AssertVisible => $"            ui.AssertVisible(\"{step.Selector}\");{warning}",
                StepType.AssertEnabled => $"            ui.AssertEnabled(\"{step.Selector}\");{warning}",
                _ => $"            // Unknown step type: {step.Type}"
            };
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }

    private string EscapeString(string str)
    {
        return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    private string LoadTemplate()
    {
        var templateName = _options.TestFramework == TestFramework.XUnit
            ? "xUnit.TestClass.template"
            : "NUnit.TestClass.template";

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Avalonia.TestRecorder.CodeGen.Templates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Return default template if embedded resource not found
            return GetDefaultXUnitTemplate();
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string GetDefaultXUnitTemplate()
    {
        return @"using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using Xunit;

namespace {Namespace};

public class {ClassName}
{
    [AvaloniaFact]
    public void {TestMethod}()
    {
        // TODO: Initialize your application and window here
        // var window = ...;
        var ui = new Ui(window);
        
{Steps}
    }
}
";
    }
}
