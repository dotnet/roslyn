// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal class InlineErrorTag : ITag
    {
        protected string _errorType;
        protected DiagnosticData _diagnostic;

        public InlineErrorTag(string errorType, DiagnosticData diagnostic)
        {
            _errorType = errorType;
            _diagnostic = diagnostic;
        }

        public FrameworkElement GetGraphics(IWpfTextView view)
        {
            var block = new TextBlock
            {
                FontStyle = FontStyles.Normal,
                Text = _diagnostic.Description,
                // Adds a little bit of padding to the left of the text relative to the border to make the text seem
                // more balanced in the border
                Padding = new Thickness(left: 2, top: 0, right: 2, bottom: 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var border = new Border
            {
                Background = new SolidColorBrush(GetColor()),
                Child = block,
                CornerRadius = new CornerRadius(2),
                // Highlighting lines are 2px buffer.  So shift us up by one from the bottom so we feel centered between them.
                Margin = new Thickness(1, top: 0, 1, bottom: 1),
            };

            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(border, TextOptions.GetTextFormattingMode(view.VisualElement));
            TextOptions.SetTextHintingMode(border, TextOptions.GetTextHintingMode(view.VisualElement));
            TextOptions.SetTextRenderingMode(border, TextOptions.GetTextRenderingMode(view.VisualElement));

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return border;
        }

        private Color GetColor()
        {
            if (_errorType is PredefinedErrorTypeNames.SyntaxError)
            {
                return new Color();
            }
            else if (_errorType is PredefinedErrorTypeNames.Warning)
            {
                return new Color();
            }
            else
            {
                return new Color();
            }
        }
    }
}
