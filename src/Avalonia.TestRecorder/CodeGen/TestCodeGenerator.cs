using Avalonia.Controls;
using Avalonia.Threading;
using System.Reflection;
using System.Text;

namespace Avalonia.TestRecorder.CodeGen;

/// <summary>
/// Generates C# test code from recorded steps.
/// </summary>
public sealed class TestCodeGenerator
{
    private readonly CodegenOptions _options;
    private readonly string _appName;

    public TestCodeGenerator(CodegenOptions options, string appName)
    {
        _options = options;
        _appName = appName;
    }

    public string Generate(IEnumerable<RecordedStep> steps, string scenarioName, Window window)
    {
        var template = LoadTemplate();
        var className = $"Recorded_{scenarioName}_Tests";
        var methodName = _options.IncludeTimestamp
            ? $"Scenario_{scenarioName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            : $"Scenario_{scenarioName}";
        var namespaceName = _options.Namespace ?? $"{_appName}.Tests";

        // Get window and DataContext information
        var windowTypeName = window.GetType().FullName ?? window.GetType().Name;
        
        // Access DataContext on the UI thread to avoid cross-thread exceptions
        object? dataContext = null;
        if (Dispatcher.UIThread.CheckAccess())
        {
            dataContext = window.DataContext;
        }
        else
        {
            dataContext = Dispatcher.UIThread.Invoke(() => window.DataContext);
        }
        
        var dataContextTypeName = dataContext?.GetType().FullName ?? dataContext?.GetType().Name ?? "object";
        
        // Handle generic types properly
        windowTypeName = FormatTypeName(windowTypeName);
        dataContextTypeName = FormatTypeName(dataContextTypeName);

        var windowInitCode = GenerateWindowInitializationCode(windowTypeName, dataContextTypeName);

        var stepsCode = GenerateStepsCode(steps);

        return template
            .Replace("{Namespace}", namespaceName)
            .Replace("{ClassName}", className)
            .Replace("{TestMethod}", methodName)
            .Replace("{WindowInit}", windowInitCode)
            .Replace("{Steps}", stepsCode);
    }

    private string GenerateWindowInitializationCode(string windowTypeName, string dataContextTypeName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"        var window = new {windowTypeName}");
        sb.AppendLine("        {");
        
        // Check if DataContext type is available and not null
        if (!string.IsNullOrEmpty(dataContextTypeName) && dataContextTypeName != "object")
        {
            sb.AppendLine($"            DataContext = new {dataContextTypeName}(),");
        }
        
        sb.AppendLine("        };");
        sb.AppendLine("        window.Show();");
        return sb.ToString();
    }

    private string FormatTypeName(string typeName)
    {
        // Handle generic types by escaping them properly for C#
        return typeName.Replace("+", ".");
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
            return _options.TestFramework == TestFramework.XUnit
                ? GetDefaultXUnitTemplate()
                : GetDefaultNUnitTemplate();
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

public partial class {ClassName}
{
    [AvaloniaFact]
    public void {TestMethod}()
    {
        // Initialize window with DataContext
{WindowInit}
        
        var ui = new Ui(window);
        
{Steps}
    }
}
";
    }

    private string GetDefaultNUnitTemplate()
    {
        return @"using Avalonia.Headless.NUnit;
using Avalonia.HeadlessTestKit;
using NUnit.Framework;

namespace {Namespace};

public partial class {ClassName}
{
    [AvaloniaTest]
    public void {TestMethod}()
    {
        // Initialize window with DataContext
{WindowInit}
        
        var ui = new Ui(window);
        
{Steps}
    }
}
";
    }
}