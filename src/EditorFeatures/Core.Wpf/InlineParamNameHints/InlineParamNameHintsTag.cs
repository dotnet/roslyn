using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineParamNameHints
{
    class InlineParamNameHintsTag : IntraTextAdornmentTag
    {
        public readonly string TagName;

        public InlineParamNameHintsTag(string text)
            : base(CreateElement(text), null, (PositionAffinity?)PositionAffinity.Successor)
        {
            TagName = text;
            if (TagName.Length != 0)
            {
                TagName += ": ";
            }
        }

        private static UIElement CreateElement(string text)
        {
            if (text.Length != 0)
            {
                text += ": ";
            }
            var block = new TextBlock
            {
                Text = text,
                FontStyle = FontStyles.Normal,
                Padding = new Thickness(0),
            };
            block.FontStyle = FontStyles.Normal;

            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return block;
        }
    }

}
