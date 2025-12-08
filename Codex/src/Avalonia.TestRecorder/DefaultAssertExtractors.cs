using Avalonia.Controls;

namespace Avalonia.TestRecorder;

internal static class DefaultAssertExtractors
{
    public static void Register(ICollection<IAssertValueExtractor> extractors)
    {
        extractors.Add(new TextBoxAssertExtractor());
        extractors.Add(new TextBlockAssertExtractor());
        extractors.Add(new ContentControlAssertExtractor());
        extractors.Add(new ToggleAssertExtractor());
        extractors.Add(new VisibilityAssertExtractor());
        extractors.Add(new EnabledAssertExtractor());
    }

    private sealed class TextBoxAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            if (control is TextBox textBox)
            {
                assert = new RecordedAssert(AssertKind.Text, textBox.Text);
                return true;
            }

            assert = null!;
            return false;
        }
    }

    private sealed class TextBlockAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            if (control is TextBlock textBlock)
            {
                assert = new RecordedAssert(AssertKind.Text, textBlock.Text);
                return true;
            }

            assert = null!;
            return false;
        }
    }

    private sealed class ContentControlAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            if (control is ContentControl contentControl)
            {
                assert = new RecordedAssert(AssertKind.Text, contentControl.Content?.ToString());
                return true;
            }

            assert = null!;
            return false;
        }
    }

    private sealed class ToggleAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            switch (control)
            {
                case CheckBox cb:
                    assert = new RecordedAssert(AssertKind.Toggle, BoolToString(cb.IsChecked), cb.IsChecked ?? false);
                    return true;
                case ToggleSwitch ts:
                    assert = new RecordedAssert(AssertKind.Toggle, BoolToString(ts.IsChecked), ts.IsChecked ?? false);
                    return true;
                default:
                    assert = null!;
                    return false;
            }
        }

        private static string BoolToString(bool? value) => (value ?? false) ? "true" : "false";
    }

    private sealed class VisibilityAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            assert = new RecordedAssert(AssertKind.Visible, BoolToString(control.IsVisible), control.IsVisible);
            return true;
        }

        private static string BoolToString(bool value) => value ? "true" : "false";
    }

    private sealed class EnabledAssertExtractor : IAssertValueExtractor
    {
        public bool TryExtract(Control control, out RecordedAssert assert)
        {
            assert = new RecordedAssert(AssertKind.Enabled, BoolToString(control.IsEnabled), control.IsEnabled);
            return true;
        }

        private static string BoolToString(bool value) => value ? "true" : "false";
    }
}
