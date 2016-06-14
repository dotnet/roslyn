// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    /// <summary>
    /// Tag that specifies line separator.
    /// </summary>
    internal class SuggestionLineTag : GraphicsTag
    {
        public static readonly SuggestionLineTag Instance = new SuggestionLineTag();

        private static Pen s_pen;

        protected override void Initialize(IWpfTextView view)
        {
            base.Initialize(view);

            if (s_pen != null)
            {
                return;
            }

            var color = Color.FromArgb(100, VerticalRuleColor.R, VerticalRuleColor.G, VerticalRuleColor.B);
            s_pen = new Pen
            {
                Brush = new SolidColorBrush(color),
                DashStyle = DashStyles.Dot,
                Thickness = 2,
            };
        }

        /// <summary>
        /// Creates a very long line at the bottom of bounds.
        /// </summary>
        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds)
        {
            Initialize(view);

            var line = new Line
            {
                Width = bounds.Bounds.Width,
                X2 = bounds.Bounds.BottomLeft.X,
                Y1 = bounds.Bounds.BottomLeft.Y - s_pen.Thickness,
                X1 = bounds.Bounds.BottomRight.X,
                Y2 = bounds.Bounds.BottomRight.Y - s_pen.Thickness,
            };
            RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);

            ApplyPen(line, s_pen);

            return new GraphicsResult(line, null);
        }

        public static void ApplyPen(Shape shape, Pen pen)
        {
            shape.Stroke = pen.Brush;
            shape.StrokeThickness = pen.Thickness;
            shape.StrokeDashCap = pen.DashCap;
            if (pen.DashStyle != null)
            {
                shape.StrokeDashArray = pen.DashStyle.Dashes;
                shape.StrokeDashOffset = pen.DashStyle.Offset;
            }
            shape.StrokeStartLineCap = pen.StartLineCap;
            shape.StrokeEndLineCap = pen.EndLineCap;
            shape.StrokeLineJoin = pen.LineJoin;
            shape.StrokeMiterLimit = pen.MiterLimit;
        }
    }
}