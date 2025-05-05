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
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics;

internal sealed class InlineDiagnosticsTag : GraphicsTag
{
    public const string TagID = "inline diagnostics - ";
    public readonly string ErrorType;
    public readonly InlineDiagnosticsLocations Location;

    private readonly DiagnosticData _diagnostic;
    private readonly INavigateToLinkService _navigateToLinkService;
    private readonly IEditorFormatMap _editorFormatMap;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly IClassificationTypeRegistryService _classificationTypeRegistryService;
    private readonly IClassificationType? _classificationType;

    public InlineDiagnosticsTag(string errorType, DiagnosticData diagnostic, IEditorFormatMap editorFormatMap,
        IClassificationFormatMapService classificationFormatMapService, IClassificationTypeRegistryService classificationTypeRegistryService,
        InlineDiagnosticsLocations location, INavigateToLinkService navigateToLinkService)
        : base(editorFormatMap)
    {
        ErrorType = errorType;
        _diagnostic = diagnostic;
        Location = location;
        _navigateToLinkService = navigateToLinkService;
        _editorFormatMap = editorFormatMap;
        _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
        _classificationTypeRegistryService = classificationTypeRegistryService;
        _classificationType = _classificationTypeRegistryService.GetClassificationType("url");
    }

    /// <summary>
    /// Creates a GraphicsResult object which is the error block based on the geometry and formatting set for the item.
    /// </summary>
    public override GraphicsResult GetGraphics(IWpfTextView view, Geometry unused, TextFormattingRunProperties format)
    {
        var block = new TextBlock
        {
            FontFamily = format.Typeface.FontFamily,
            FontSize = 0.75 * format.FontRenderingEmSize,
            FontStyle = FontStyles.Normal,
            Foreground = format.ForegroundBrush,
            Padding = new Thickness(left: 2, top: 0, right: 2, bottom: 0),
        };

        var idRun = GetRunForId(out var hyperlink);
        if (hyperlink is null)
        {
            block.Inlines.Add(idRun);
        }
        else
        {
            // Match the hyperlink color to what the classification is set to by the user
            var linkColor = _classificationFormatMap.GetTextProperties(_classificationType);
            hyperlink.Foreground = linkColor.ForegroundBrush;

            block.Inlines.Add(hyperlink);
            hyperlink.RequestNavigate += HandleRequestNavigate;
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
            Margin = new Thickness(10, top: 0, right: 0, bottom: 1),
            Padding = new Thickness(1)
        };

        // This is used as a workaround to the moniker issues in blue theme
        var editorBackground = (Color)_editorFormatMap.GetProperties("TextView Background")["BackgroundColor"];
        ImageThemingUtilities.SetImageBackgroundColor(border, editorBackground);

        border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        view.LayoutChanged += View_LayoutChanged;

        return new GraphicsResult(border, dispose:
            () =>
            {
                if (hyperlink is not null)
                {
                    hyperlink.RequestNavigate -= HandleRequestNavigate;
                }

                view.LayoutChanged -= View_LayoutChanged;
            });

        void View_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (Location is InlineDiagnosticsLocations.PlacedAtEndOfEditor)
            {
                Canvas.SetLeft(border, view.ViewportRight - border.DesiredSize.Width);
            }
        }

        void HandleRequestNavigate(object sender, RoutedEventArgs e)
        {
            var uri = hyperlink.NavigateUri;
            _ = _navigateToLinkService.TryNavigateToLinkAsync(uri, CancellationToken.None);
            e.Handled = true;
        }

        Run GetRunForId(out Hyperlink? link)
        {
            var id = new Run(_diagnostic.Id);
            link = null;

            var helpLinkUri = _diagnostic.GetValidHelpLinkUri();
            if (helpLinkUri != null)
            {
                link = new Hyperlink(id)
                {
                    NavigateUri = helpLinkUri
                };
            }

            return id;
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
            PredefinedErrorTypeNames.Warning => "inline diagnostics - compiler warning",
            _ => throw ExceptionUtilities.UnexpectedValue(error)
        };

    /// <summary>
    /// Gets called when the ClassificationFormatMap is changed to update the adornment
    /// </summary>
    public static void UpdateColor(TextFormattingRunProperties format, UIElement adornment)
    {
        var border = (Border)adornment;
        border.BorderBrush = format.BackgroundBrush;
        var stackPanel = (StackPanel)border.Child;
        foreach (var child in stackPanel.Children)
        {
            if (child is TextBlock block)
            {
                block.Foreground = format.ForegroundBrush;
            }
        }
    }

    /// <summary>
    /// We do not need to set a default color so this remains unimplemented
    /// </summary>
    protected override Color? GetColor(IWpfTextView view, IEditorFormatMap editorFormatMap)
    {
        throw new NotImplementedException();
    }
}
