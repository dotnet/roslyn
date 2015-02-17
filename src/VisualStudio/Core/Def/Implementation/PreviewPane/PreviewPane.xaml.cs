// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    internal partial class PreviewPane : UserControl, IDisposable
    {
        private static readonly string s_dummyThreeLineTitle = "A" + Environment.NewLine + "A" + Environment.NewLine + "A";
        private static readonly Size s_infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        private readonly string _errorId;
        private readonly bool _telemetry;

        private readonly IServiceProvider _serviceProvider;

        private bool _isExpanded;
        private double _heightForThreeLineTitle;
        private IWpfDifferenceViewer _previewDiffViewer;

        public PreviewPane(Image severityIcon, string id, string title, string helpMessage, string description, string helpLink,
            bool telemetry, object previewContent, IServiceProvider serviceProvider)
        {
            InitializeComponent();

            InitializeHyperlinkStyles();

            if ((severityIcon != null) && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title))
            {
                HeaderStackPanel.Visibility = Visibility.Visible;

                SeverityIconBorder.Child = severityIcon;

                // Set the initial title text to three lines worth so that we can measure the pixel height
                // that WPF requires to render three lines of text under the current font and DPI settings.
                TitleRun.Text = s_dummyThreeLineTitle;
                TitleTextBlock.Measure(availableSize: s_infiniteSize);
                _heightForThreeLineTitle = TitleTextBlock.DesiredSize.Height;

                // Now set the actual title text.
                TitleRun.Text = title;

                Uri helpUri;
                if (BrowserHelper.TryGetUri(helpLink, out helpUri))
                {
                    InitializeDiagnosticIdHyperLink(id, helpUri, bingLink: false);
                }
                else
                {
                    InitializeDiagnosticIdHyperLink(id, BrowserHelper.CreateBingQueryUri(id, helpMessage), bingLink: true);
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    DescriptionParagraph.Inlines.Add(description);
                }
            }

            InitializePreviewElement(previewContent);

            _serviceProvider = serviceProvider;
            _errorId = id;

            // save permission whether we are allowed to save data as it is or not.
            _telemetry = telemetry;
        }

        private void InitializePreviewElement(object previewContent)
        {
            FrameworkElement previewElement = null;
            if (previewContent is IWpfDifferenceViewer)
            {
                _previewDiffViewer = previewContent as IWpfDifferenceViewer;
                previewElement = _previewDiffViewer.VisualElement;
                PreviewDockPanel.Background = _previewDiffViewer.InlineView.Background;
            }
            else if (previewContent is string)
            {
                previewElement = GetPreviewForString(previewContent as string);
            }
            else if (previewContent is FrameworkElement)
            {
                previewElement = previewContent as FrameworkElement;
            }

            if (previewElement != null)
            {
                HeaderSeparator.Visibility = Visibility.Visible;
                PreviewDockPanel.Visibility = Visibility.Visible;
                PreviewScrollViewer.Content = previewElement;
                previewElement.VerticalAlignment = VerticalAlignment.Top;
                previewElement.HorizontalAlignment = HorizontalAlignment.Left;
            }

            // 1. Width of the header section should not exceed the width of the preview content.
            // In other words, the text we put in the header at the top of the preview pane
            // should not cause the preview pane itself to grow wider than the width required to
            // display the preview content at the bottom of the pane.
            // 2. Adjust the height of the header section so that it displays only three lines worth
            // by default.
            AdjustWidthAndHeight(previewElement);
        }

        private void InitializeHyperlinkStyles()
        {
            this.IdHyperlink.SetVSHyperLinkStyle();
            this.LearnMoreHyperlink.SetVSHyperLinkStyle();
        }

        private void InitializeDiagnosticIdHyperLink(string id, Uri helpUri, bool bingLink)
        {
            IdHyperlink.Inlines.Add(id);
            IdHyperlink.NavigateUri = helpUri;
            IdHyperlink.IsEnabled = true;

            LearnMoreHyperlink.Inlines.Add(string.Format(ServicesVSResources.LearnMoreLinkText, id));
            LearnMoreHyperlink.NavigateUri = helpUri;
        }

        public static Border GetPreviewForString(string previewContent, bool useItalicFontStyle = false, bool centerAlignTextHorizontally = false)
        {
            var textBlock = new TextBlock()
            {
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Text = previewContent,
                TextWrapping = TextWrapping.Wrap,
            };

            if (useItalicFontStyle)
            {
                textBlock.FontStyle = FontStyles.Italic;
            }

            if (centerAlignTextHorizontally)
            {
                textBlock.TextAlignment = TextAlignment.Center;
            }

            var preview = new Border()
            {
                Width = 400,
                MinHeight = 75,
                Child = textBlock
            };

            return preview;
        }

        // This method adjusts the width of the header section to match that of the preview content
        // thereby ensuring that the preview pane itself is never wider than the preview content.
        // This method also adjusts the height of the header section so that it displays only three lines
        // worth by default.
        private void AdjustWidthAndHeight(FrameworkElement previewElement)
        {
            var headerStackPanelWidth = double.PositiveInfinity;
            var titleTextBlockHeight = double.PositiveInfinity;
            if (previewElement == null)
            {
                HeaderStackPanel.Measure(availableSize: s_infiniteSize);
                headerStackPanelWidth = HeaderStackPanel.DesiredSize.Width;

                TitleTextBlock.Measure(availableSize: s_infiniteSize);
                titleTextBlockHeight = TitleTextBlock.DesiredSize.Height;
            }
            else
            {
                PreviewDockPanel.Measure(availableSize: new Size(previewElement.Width, double.PositiveInfinity));
                headerStackPanelWidth = PreviewDockPanel.DesiredSize.Width;
                if (IsNormal(headerStackPanelWidth))
                {
                    TitleTextBlock.Measure(availableSize: new Size(headerStackPanelWidth, double.PositiveInfinity));
                    titleTextBlockHeight = TitleTextBlock.DesiredSize.Height;
                }
            }

            if (IsNormal(headerStackPanelWidth))
            {
                HeaderStackPanel.Width = headerStackPanelWidth;
            }

            // If the pixel height required to render the complete title in the
            // TextBlock is larger than that required to render three lines worth,
            // then trim the contents of the TextBlock with an ellipsis at the end and
            // display the expander button that will allow users to view the full text.
            if (HasDescription || (IsNormal(titleTextBlockHeight) && (titleTextBlockHeight > _heightForThreeLineTitle)))
            {
                TitleTextBlock.MaxHeight = _heightForThreeLineTitle;
                TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;

                ExpanderToggleButton.Visibility = Visibility.Visible;

                if (_isExpanded)
                {
                    ExpanderToggleButton.IsChecked = true;
                }
            }
        }

        private static bool IsNormal(double size)
        {
            return size > 0 && !double.IsNaN(size) && !double.IsInfinity(size);
        }

        private bool HasDescription
        {
            get
            {
                return DescriptionParagraph.Inlines.Count > 0;
            }
        }

        #region IDisposable Implementation
        private bool _disposedValue = false;

        // VS editor will call Dispose at which point we should Close() the embedded IWpfDifferenceViewer.
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && (_previewDiffViewer != null) && !_previewDiffViewer.IsClosed)
                {
                    _previewDiffViewer.Close();
                }
            }

            _disposedValue = true;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion

        private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            BrowserHelper.StartBrowser(_serviceProvider, e.Uri);
            e.Handled = true;

            var hyperlink = sender as Hyperlink;
            if (hyperlink == null)
            {
                return;
            }

            DiagnosticLogger.LogHyperlink(hyperlink.Name ?? "Preview", _errorId, HasDescription, _telemetry, e.Uri.AbsoluteUri);
        }

        private void ExpanderToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ExpanderToggleButton.IsChecked ?? false)
            {
                TitleTextBlock.MaxHeight = double.PositiveInfinity;
                TitleTextBlock.TextTrimming = TextTrimming.None;

                if (HasDescription)
                {
                    DescriptionDockPanel.Visibility = Visibility.Visible;

                    if (LearnMoreHyperlink.NavigateUri != null)
                    {
                        LearnMoreTextBlock.Visibility = Visibility.Visible;
                        LearnMoreHyperlink.IsEnabled = true;
                    }
                }

                _isExpanded = true;
            }
            else
            {
                TitleTextBlock.MaxHeight = _heightForThreeLineTitle;
                TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;

                DescriptionDockPanel.Visibility = Visibility.Collapsed;

                _isExpanded = false;
            }
        }
    }
}
