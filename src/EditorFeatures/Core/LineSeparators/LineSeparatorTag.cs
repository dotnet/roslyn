// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators;

/// <summary>
/// Tag that specifies line separator.
/// </summary>
internal sealed class LineSeparatorTag : GraphicsTag
{
    public LineSeparatorTag(IEditorFormatMap editorFormatMap)
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
    public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds, TextFormattingRunProperties? format)
    {
        var border = new Border()
        {
            BorderBrush = GetBrush(view),
            BorderThickness = new Thickness(0, 0, 0, bottom: 1),
            Height = 1,
            Width = view.ViewportWidth
        };

        view.ViewportWidthChanged += ViewportWidthChangedHandler;

        // Subtract rect.Height to ensure that the line separator is drawn
        // at the bottom of the line, rather than immediately below.
        // This makes the line separator line up with the outlining bracket.
        Canvas.SetTop(border, bounds.Bounds.Bottom - border.Height);

        return new GraphicsResult(border,
            () => view.ViewportWidthChanged -= ViewportWidthChangedHandler);

        void ViewportWidthChangedHandler(object s, EventArgs e)
        {
            border.Width = view.ViewportWidth;
        }
    }
}
