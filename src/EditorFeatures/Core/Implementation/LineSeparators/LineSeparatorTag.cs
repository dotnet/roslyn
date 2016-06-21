// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// Tag that specifies line separator.
    /// </summary>
    internal class LineSeparatorTag : GraphicsTag
    {
        public static readonly LineSeparatorTag Instance = new LineSeparatorTag();

        /// <summary>
        /// Creates a very long line at the bottom of bounds.
        /// </summary>
        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds)
        {
            Initialize(view);

            var border = new Border();
            border.BorderBrush = VerticalRuleBrush;
            border.BorderThickness = new Thickness(0, 0, 0, bottom: 1);
            border.Height = 1;
            border.Width = view.ViewportWidth;

            EventHandler viewportWidthChangedHandler = (s, e) =>
            {
                border.Width = view.ViewportWidth;
            };

            view.ViewportWidthChanged += viewportWidthChangedHandler;

            // Subtract rect.Height to ensure that the line separator is drawn
            // at the bottom of the line, rather than immediately below.
            // This makes the line separator line up with the outlining bracket.
            Canvas.SetTop(border, bounds.Bounds.Bottom - border.Height);

            return new GraphicsResult(border,
                () => view.ViewportWidthChanged -= viewportWidthChangedHandler);
        }
    }
}
