// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation
{
    /// <summary>
    /// Tag that specifies line separator.
    /// </summary>
    internal class StringIndentationTag : GraphicsTag
    {
        public StringIndentationTag(IEditorFormatMap editorFormatMap)
            : base(editorFormatMap)
        {
        }

        protected override Color? GetColor(
            IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            var brush = view.VisualElement.TryFindResource("outlining.verticalrule.foreground") as SolidColorBrush;
            return brush?.Color;
        }

        /// <summary>
        /// Creates a very long line at the bottom of bounds.
        /// </summary>
        public override GraphicsResult? GetGraphics(
            IWpfTextView view, Geometry bounds, SnapshotSpan span, TextFormattingRunProperties? format)
        {
            Initialize(view);

            var lines = view.TextViewLines;
            var startLine = lines.GetTextViewLineContainingBufferPosition(span.Start);
            var endLine = lines.GetTextViewLineContainingBufferPosition(span.End);

            var border = new Border()
            {
                BorderBrush = GraphicsTagBrush,
                BorderThickness = new Thickness(left: 1, 0, 0, 0),
                Height = endLine.Top - startLine.Bottom,
                Width = 1,
            };

            // Subtract rect.Height to ensure that the line separator is drawn
            // at the bottom of the line, rather than immediately below.
            // This makes the line separator line up with the outlining bracket.
            Canvas.SetTop(border, startLine.Bottom);
            Canvas.SetLeft(border, bounds.Bounds.Left);

            return new GraphicsResult(border, dispose: null);
        }
    }
}
