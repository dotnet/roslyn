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
        private readonly IClassificationType _classificationType;
        private IClassificationFormatMap _classificationFormatMap;
        private TextFormattingRunProperties _format;
        private TextBlock? _block;
        private Border? _border;
        private IWpfTextView _view;
        private Geometry _bounds;

        public InlineErrorTag(string errorType, DiagnosticData diagnostic, IEditorFormatMap editorFormatMap,
            IClassificationFormatMapService classificationFormatMapService, IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(editorFormatMap)
        {
            _errorType = errorType;
            _diagnostic = diagnostic;
            _editorFormatMap = editorFormatMap;
            _classificationFormatMapService = classificationFormatMapService;
            _classificationType = classificationTypeRegistryService.GetClassificationType(TagId);
        }

        private void SetFormat(IClassificationFormatMap classificationFormatMap)
        {
            _format ??= classificationFormatMap.GetTextProperties(_classificationType);
        }

        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds)
        {
            _view = view;
            _bounds = bounds;
            _classificationFormatMap = _classificationFormatMapService.GetClassificationFormatMap(view);

            SetFormat(_classificationFormatMap);

            _classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;

            _block = new TextBlock
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

            _block.Inlines.Add(_diagnostic.Id + ": " + _diagnostic.Message);
            _block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _block.Arrange(new Rect(_block.DesiredSize));

            var color = _editorFormatMap.GetProperties(_errorType)[EditorFormatDefinition.ForegroundBrushId];
            _border = new Border
            {
                Background = (Brush)color,
                Child = _block,
                CornerRadius = new CornerRadius(2),
                // Highlighting lines are 2px buffer.  So shift us up by one from the bottom so we feel centered between them.
                Margin = new Thickness(0, top: 0, 0, bottom: 5),
            };

            _border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            void ViewportWidthChangedHandler(object s, EventArgs e)
            {
                Canvas.SetLeft(_border, view.ViewportWidth - _border.DesiredSize.Width);
            }

            view.ViewportWidthChanged += ViewportWidthChangedHandler;
            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(_border, TextOptions.GetTextFormattingMode(view.VisualElement));
            TextOptions.SetTextHintingMode(_border, TextOptions.GetTextHintingMode(view.VisualElement));
            TextOptions.SetTextRenderingMode(_border, TextOptions.GetTextRenderingMode(view.VisualElement));

            Canvas.SetTop(_border, bounds.Bounds.Bottom - _border.DesiredSize.Height);
            Canvas.SetLeft(_border, view.ViewportWidth - _border.DesiredSize.Width);

            return new GraphicsResult(_border,
                () => view.ViewportWidthChanged -= ViewportWidthChangedHandler);
        }

        private void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (_format is not null)
            {
                SetFormat(_classificationFormatMap);
                if (_block is not null && _border is not null)
                {
                    _block.FontFamily = _format.Typeface.FontFamily;
                    _block.FontSize = 0.75 * _format.FontHintingEmSize;
                    _block.Foreground = _format.ForegroundBrush;
                    Canvas.SetTop(_border, _bounds.Bounds.Bottom - _border.DesiredSize.Height);
                    Canvas.SetLeft(_border, _view.ViewportWidth - _border.DesiredSize.Width);
                }
            }
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
