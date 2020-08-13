﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to create the UIElements that get shown
    /// in the editor
    /// </summary>
    internal class InlineParameterNameHintsTag : IntraTextAdornmentTag
    {
        public const string TagId = "inline parameter name hints";

        /// <summary>
        /// Creates the UIElement on call
        /// Uses PositionAffinity.Predecessor because we want the tag to be associated with the preceding character
        /// </summary>
        /// <param name="text">The name of the parameter associated with the argument</param>
        public InlineParameterNameHintsTag(string text, IWpfTextView textView, TextFormattingRunProperties format)
            : base(CreateElement(text, textView, format), removalCallback: null, PositionAffinity.Predecessor)
        {
        }

        private static UIElement CreateElement(string text, IWpfTextView textView, TextFormattingRunProperties format)
        {
            // Constructs the hint block which gets assigned parameter name and fontstyles according to the options
            // page. Calculates a font size 1/4 smaller than the font size of the rest of the editor
            var block = new TextBlock
            {
                FontFamily = format.Typeface.FontFamily,
                FontSize = format.FontRenderingEmSize - (0.25 * format.FontRenderingEmSize),
                FontStyle = FontStyles.Normal,
                Foreground = format.ForegroundBrush,
                Padding = new Thickness(0),
                Text = text + ":",
                VerticalAlignment = VerticalAlignment.Center,
            };

            // Encapsulates the textblock within a border. Sets the height of the border to be 3/4 of the original 
            // height. Gets foreground/background colors from the options menu. The margin is the distance from the 
            // adornment to the text and pushing the adornment upwards to create a separation when on a specific line
            var border = new Border
            {
                Background = format.BackgroundBrush,
                Child = block,
                CornerRadius = new CornerRadius(2),
                Height = textView.LineHeight - (0.25 * textView.LineHeight),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -0.20 * textView.LineHeight, 5, 0),
                Padding = new Thickness(1),

                // Need to set SnapsToDevicePixels and UseLayoutRounding to avoid unnecessary reformatting
                SnapsToDevicePixels = textView.VisualElement.SnapsToDevicePixels,
                UseLayoutRounding = textView.VisualElement.UseLayoutRounding,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(border, TextOptions.GetTextFormattingMode(textView.VisualElement));
            TextOptions.SetTextHintingMode(border, TextOptions.GetTextHintingMode(textView.VisualElement));
            TextOptions.SetTextRenderingMode(border, TextOptions.GetTextRenderingMode(textView.VisualElement));

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return border;
        }
    }
}
