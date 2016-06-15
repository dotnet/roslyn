// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    /// <summary>
    /// Tag that specifies line separator.
    /// </summary>
    internal class SuggestionTag : GraphicsTag
    {
        public static readonly SuggestionTag Instance = new SuggestionTag();

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
                DashCap = PenLineCap.Round,
                Thickness = 2,
            };
        }

        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry geometry)
        {
            Initialize(view);

            // We clip off a bit off the start of the line to prevent a half-square being
            // drawn.
            var clipRectangle = geometry.Bounds;
            clipRectangle.Offset(1, 0);

            var line = new Line
            {
                X1 = geometry.Bounds.Left,
                Y1 = geometry.Bounds.Bottom - s_pen.Thickness,
                X2 = geometry.Bounds.Right,
                Y2 = geometry.Bounds.Bottom - s_pen.Thickness,
                Clip = new RectangleGeometry { Rect = clipRectangle }
            };
            // RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);

            ApplyPen(line, s_pen);

            // Shift the line over to offset the clipping we did.
            line.RenderTransform = new TranslateTransform(-s_pen.Thickness, 0);
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