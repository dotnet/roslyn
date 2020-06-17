using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineParamNameHints
{
    class InlineParamHintsTag : IntraTextAdornmentTag
    {
        public readonly int TagLength;
        public InlineParamHintsTag(string text, int tagLength, TextFormattingRunProperties format)
            : base(CreateElement(text, format), null, (tagLength == 0) ? ((PositionAffinity?)PositionAffinity.Predecessor) : null)
        {
            TagLength = tagLength;
        }

        private static UIElement CreateElement(string text, TextFormattingRunProperties format)
        {
            var block = new TextBox
            {
                Text = text,
                Foreground = format.ForegroundBrush,
                FontFamily = format.Typeface.FontFamily,
                FontSize = format.FontRenderingEmSize,
                FontStyle = FontStyles.Normal
            };
            block.FontSize = format.FontRenderingEmSize;
            block.FontStyle = FontStyles.Italic;//.Oblique;

            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return block;
        }
    }

}
