using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.TestRecorder.Assertions;

/// <summary>
/// Built-in assertion value extractors for common controls.
/// </summary>
internal static class BuiltInExtractors
{
    public static List<IAssertValueExtractor> GetDefault()
    {
        return new List<IAssertValueExtractor>
        {
            new TextBoxExtractor(),
            new TextBlockExtractor(),
            new ContentControlExtractor(),
            new ToggleButtonExtractor()
        };
    }

    private class TextBoxExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedStep? step)
        {
            if (control is TextBox textBox)
            {
                step = new RecordedStep
                {
                    Type = StepType.AssertText,
                    Parameter = textBox.Text
                };
                return true;
            }
            step = null;
            return false;
        }
    }

    private class TextBlockExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedStep? step)
        {
            if (control is TextBlock textBlock)
            {
                step = new RecordedStep
                {
                    Type = StepType.AssertText,
                    Parameter = textBlock.Text
                };
                return true;
            }
            step = null;
            return false;
        }
    }

    private class ContentControlExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedStep? step)
        {
            if (control is ContentControl content)
            {
                var text = content.Content?.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    step = new RecordedStep
                    {
                        Type = StepType.AssertText,
                        Parameter = text
                    };
                    return true;
                }
            }
            step = null;
            return false;
        }
    }

    private class ToggleButtonExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedStep? step)
        {
            if (control is ToggleButton toggle)
            {
                step = new RecordedStep
                {
                    Type = StepType.AssertChecked,
                    Parameter = toggle.IsChecked?.ToString() ?? "null"
                };
                return true;
            }
            step = null;
            return false;
        }
    }
}
