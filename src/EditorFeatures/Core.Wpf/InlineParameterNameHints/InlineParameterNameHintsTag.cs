// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
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
        public InlineParameterNameHintsTag(string text)
            : base(CreateElement(text), removalCallback: null, PositionAffinity.Successor)
        {
        }

        private static UIElement CreateElement(string text)
        {
            // Constructs the hint block which gets assigned parameter name, a normal fontstyle, and sets the padding 
            // space around the block to 0
            var block = new TextBlock
            {
                Text = text + ": ",
                FontStyle = FontStyles.Normal,
                Padding = new Thickness(0),
            };

            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return block;
        }
    }
}
