// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    internal class InlineDiagnosticsTag : GraphicsTag
    {
        public const string TagID = "inline diagnostics - ";
        public readonly string ErrorType;
        public readonly InlineDiagnosticsLocations Location;

        private readonly DiagnosticData _diagnostic;
        private readonly INavigateToLinkService _navigateToLinkService;
        private readonly IEditorFormatMap _editorFormatMap;

        public InlineDiagnosticsTag(string errorType, DiagnosticData diagnostic, IEditorFormatMap editorFormatMap, InlineDiagnosticsLocations location, INavigateToLinkService navigateToLinkService)
            : base(editorFormatMap)
        {
            ErrorType = errorType;
            _diagnostic = diagnostic;
            Location = location;
            _navigateToLinkService = navigateToLinkService;
            _editorFormatMap = editorFormatMap;
        }

        /// <summary>
        /// Creates a GraphicsResult object which is the error block based on the geometry and formatting set for the item.
        /// </summary>
        public override GraphicsResult GetGraphics(IWpfTextView view, Geometry bounds, TextFormattingRunProperties format)
        {
            var block = new TextBlock
            {
                FontFamily = format.Typeface.FontFamily,
                FontSize = 0.75 * format.FontRenderingEmSize,
                FontStyle = FontStyles.Normal,
                Foreground = format.ForegroundBrush,
                Padding = new Thickness(left: 2, top: 0, right: 2, bottom: 0),
            };

            var id = new Run(_diagnostic.Id);
            Hyperlink? link = null;

            if (_diagnostic.HelpLink is null)
            {
                block.Inlines.Add(id);
            }
            else
            {
                link = new Hyperlink(id)
                {
                    NavigateUri = new Uri(_diagnostic.HelpLink)
                };

                link.RequestNavigate += HandleRequestNavigate;
                block.Inlines.Add(link);
            }

            block.Inlines.Add(": " + _diagnostic.Message);

            var lineHeight = Math.Floor(format.Typeface.FontFamily.LineSpacing * block.FontSize);
            var image = new CrispImage
            {
                Moniker = GetMoniker(),
                MaxHeight = lineHeight,
                Margin = new Thickness(1, 0, 5, 0)
            };

            var border = new Border
            {
                BorderBrush = format.BackgroundBrush,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Child = new StackPanel
                {
                    Height = lineHeight,
                    Orientation = Orientation.Horizontal,
                    Children = { image, block }
                },
                CornerRadius = new CornerRadius(2),
                // Highlighting lines are 2px buffer. So shift us up by one from the bottom so we feel centered between them.
                Margin = new Thickness(10, top: 0, 0, bottom: 1),
                Padding = new Thickness(1)
            };

            // This is used as a workaround to the moniker issues in blue theme
            var editorBackground = (Color)_editorFormatMap.GetProperties("TextView Background")["BackgroundColor"];
            ImageThemingUtilities.SetImageBackgroundColor(border, editorBackground);

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            view.ViewportWidthChanged += ViewportWidthChangedHandler;

            return new GraphicsResult(border, dispose:
                () =>
                {
                    if (link is not null)
                    {
                        link.RequestNavigate -= HandleRequestNavigate;
                    }

                    view.ViewportWidthChanged -= ViewportWidthChangedHandler;
                });

            void ViewportWidthChangedHandler(object s, EventArgs e)
            {
                if (Location is InlineDiagnosticsLocations.PlacedAtEndOfEditor)
                {
                    Canvas.SetLeft(border, view.ViewportWidth - border.DesiredSize.Width);
                }
            }

            void HandleRequestNavigate(object sender, RoutedEventArgs e)
            {
                var uri = link.NavigateUri;
                _ = _navigateToLinkService.TryNavigateToLinkAsync(uri, CancellationToken.None);
                e.Handled = true;
            }
        }

        private ImageMoniker GetMoniker()
            => _diagnostic.Severity switch
            {
                DiagnosticSeverity.Warning => KnownMonikers.StatusWarning,
                _ => KnownMonikers.StatusError,
            };

        public static string GetClassificationId(string error)
            => error switch
            {
                EditAndContinueErrorTypeDefinition.Name => "inline diagnostics - Edit and Continue",
                PredefinedErrorTypeNames.SyntaxError => "inline diagnostics - syntax error",
                _ => "inline diagnostics - compiler warning"
            };

        /// <summary>
        /// Gets called when the ClassificationFormatMap is changed to update the adornment
        /// </summary>
        public static void UpdateColor(TextFormattingRunProperties format, UIElement adornment)
        {
            var border = (Border)adornment;
            border.BorderBrush = format.BackgroundBrush;
            var block = (TextBlock)border.Child;
            block.Foreground = format.ForegroundBrush;
        }

        /// <summary>
        /// We do not need to set a default color so this remains unimplemented
        /// </summary>
        protected override Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap)
        {
            throw new NotImplementedException();
        }
    }
}
