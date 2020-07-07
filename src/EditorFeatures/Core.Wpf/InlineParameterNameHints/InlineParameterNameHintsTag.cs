// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to create the UIElements that get shown
    /// in the editor
    /// </summary>
    internal class InlineParameterNameHintsTag : IntraTextAdornmentTag
    {
        /// <summary>
        /// Creates the UIElement on call
        /// Uses PositionAffinity.Successor because we want the tag to be associated with the following character
        /// </summary>
        /// <param name="text">The name of the parameter associated with the argument</param>
        public InlineParameterNameHintsTag(string text, double lineHeight)
            : base(CreateElement(text, lineHeight), removalCallback: null, PositionAffinity.Successor)
        {
        }

        private static UIElement CreateElement(string text, double lineHeight)
        {
            // Constructs the hint block which gets assigned parameter name, a normal fontstyle, and sets the padding 
            // space around the block to 0
            var block = new TextBlock
            {
                Text = text + ": ",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontStyle = FontStyles.Normal,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 9,
                Padding = new Thickness(0),
                Background = System.Windows.Media.Brushes.Lavender,
                Foreground = System.Windows.Media.Brushes.Gray
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(2),
                Background = System.Windows.Media.Brushes.Lavender,
                BorderBrush = System.Windows.Media.Brushes.Lavender,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Height = lineHeight,
                Child = block,
            };
            block.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            border.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return border;
            //return block;

        }
    }
}
