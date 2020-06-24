using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.InlineParamNameHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to
    /// create the UIElements that get shown in the editor
    /// </summary>
    class InlineParamNameHintsTag : IntraTextAdornmentTag
    {
        public readonly string TagName;

        /// <summary>
        /// Creates the UIElement on call
        /// </summary>
        /// <param name="text">The name of the parameter associated with the argument</param>
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
