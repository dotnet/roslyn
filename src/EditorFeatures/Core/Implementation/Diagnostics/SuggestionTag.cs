// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(SuggestionTagFormat.ResourceName)]
    [UserVisible(true)]
    internal sealed class SuggestionTagFormat : EditorFormatDefinition
    {
        public const string ResourceName = "SuggestionTagFormat";

        public SuggestionTagFormat()
        {
            this.ForegroundColor = Color.FromArgb(200, 0xA5, 0xA5, 0xA5);
            this.BackgroundCustomizable = false;
            this.DisplayName = EditorFeaturesResources.Suggestion_ellipses;
        }
    }

    /// <summary>
    /// Tag that specifies line separator.
    /// </summary>
    internal class SuggestionTag : GraphicsTag
    {
        private static Brush s_graphicsTagBrush;
        private static Color s_graphicsTagColor;
        private static Pen s_graphicsTagPen;

        public static readonly SuggestionTag Instance = new SuggestionTag();

        protected override Color? GetColor(
            IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            var property = editorFormatMap.GetProperties(SuggestionTagFormat.ResourceName)["ForegroundColor"];
            return property as Color?;
        }

        protected override void ClearCachedFormatData()
        {
            s_graphicsTagBrush = null;
            s_graphicsTagColor = default(Color);
            s_graphicsTagPen = null;
        }

        protected override void Initialize(IWpfTextView view, 
            ref Brush graphicsTagBrush, 
            ref Color graphicsTagColor)
        {
            base.Initialize(view, ref graphicsTagBrush, ref graphicsTagColor);

            if (s_graphicsTagPen != null)
            {
                return;
            }

            var color = graphicsTagColor;
            s_graphicsTagPen = new Pen
            {
                Brush = new SolidColorBrush(color),
                DashStyle = DashStyles.Dot,
                DashCap = PenLineCap.Round,
                Thickness = 2,
            };
        }

        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry geometry)
        {
            Initialize(view, ref s_graphicsTagBrush, ref s_graphicsTagColor);

            // We clip off a bit off the start of the line to prevent a half-square being
            // drawn.
            var clipRectangle = geometry.Bounds;
            clipRectangle.Offset(1, 0);

            var line = new Line
            {
                X1 = geometry.Bounds.Left,
                Y1 = geometry.Bounds.Bottom - s_graphicsTagPen.Thickness,
                X2 = geometry.Bounds.Right,
                Y2 = geometry.Bounds.Bottom - s_graphicsTagPen.Thickness,
                Clip = new RectangleGeometry { Rect = clipRectangle }
            };
            // RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);

            ApplyPen(line, s_graphicsTagPen);

            // Shift the line over to offset the clipping we did.
            line.RenderTransform = new TranslateTransform(-s_graphicsTagPen.Thickness, 0);
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