// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal class InlineErrorTag : GraphicsTag
    {
        public const string TagId = "inline error";
        private readonly string _errorType;
        private readonly DiagnosticData _diagnostic;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private IClassificationFormatMap _classificationFormatMap;
        private readonly IClassificationType _classificationType;
        private TextFormattingRunProperties _format;

        public InlineErrorTag(string errorType, DiagnosticData diagnostic, IEditorFormatMap editorFormatMap,
            IClassificationFormatMapService classificationFormatMapService, IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(editorFormatMap)
        {
            _errorType = errorType;
            _diagnostic = diagnostic;
            _editorFormatMap = editorFormatMap;
            _classificationFormatMapService = classificationFormatMapService;
            _classificationType = classificationTypeRegistryService.GetClassificationType(TagId);
            if (_classificationFormatMap is not null)
            {
                _classificationFormatMap.ClassificationFormatMappingChanged += OnClassificationFormatMappingChanged;
            }
        }

        private void SetFormat(IClassificationFormatMap classificationFormatMap)
        {
            _format ??= classificationFormatMap.GetTextProperties(_classificationType);
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (_format != null)
            {
                SetFormat(_classificationFormatMap);
            }
        }

        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds)
        {
            _classificationFormatMap = _classificationFormatMapService.GetClassificationFormatMap(view);
            SetFormat(_classificationFormatMap);

            var block = new TextBlock
            {
                FontFamily = _format.Typeface.FontFamily,
                FontSize = 0.75 * _format.FontRenderingEmSize,
                FontStyle = FontStyles.Normal,
                Foreground = _format.ForegroundBrush,
                // Adds a little bit of padding to the left of the text relative to the border to make the text seem
                // more balanced in the border
                Padding = new Thickness(left: 2, top: 0, right: 2, bottom: 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            block.Inlines.Add(_diagnostic.Id + ": " + _diagnostic.Message);
            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            block.Arrange(new Rect(block.DesiredSize));

            var color = _editorFormatMap.GetProperties(_errorType)[EditorFormatDefinition.ForegroundBrushId];
            var border = new Border
            {
                Background = (Brush)color,
                Child = block,
                CornerRadius = new CornerRadius(2),
                // Highlighting lines are 2px buffer.  So shift us up by one from the bottom so we feel centered between them.
                Margin = new Thickness(0, top: 0, 0, bottom: 5),
            };

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            void viewportWidthChangedHandler(object s, EventArgs e)
            {
                Canvas.SetLeft(border, view.ViewportWidth - block.DesiredSize.Width);
            }

            view.ViewportWidthChanged += viewportWidthChangedHandler;
            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(border, TextOptions.GetTextFormattingMode(view.VisualElement));
            TextOptions.SetTextHintingMode(border, TextOptions.GetTextHintingMode(view.VisualElement));
            TextOptions.SetTextRenderingMode(border, TextOptions.GetTextRenderingMode(view.VisualElement));

            Canvas.SetTop(border, bounds.Bounds.Bottom - block.DesiredSize.Height);
            Canvas.SetLeft(border, view.ViewportWidth - block.DesiredSize.Width);

            return new GraphicsResult(border,
                () => view.ViewportWidthChanged -= viewportWidthChangedHandler);
        }

        protected override Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            if (_errorType is PredefinedErrorTypeNames.SyntaxError)
            {
                return Colors.Red;
            }
            else if (_errorType is PredefinedErrorTypeNames.Warning)
            {
                return Colors.Green;
            }
            else
            {
                return Colors.Purple;
            }
        }
    }
}
