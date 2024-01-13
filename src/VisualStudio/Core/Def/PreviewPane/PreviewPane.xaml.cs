// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using IVsUIShell = Microsoft.VisualStudio.Shell.Interop.IVsUIShell;
using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    internal partial class PreviewPane : UserControl, IDisposable, IOleCommandTarget
    {
        private const double DefaultWidth = 400;

        private static readonly string s_dummyThreeLineTitle = "A" + Environment.NewLine + "A" + Environment.NewLine + "A";
        private static readonly Size s_infiniteSize = new(double.PositiveInfinity, double.PositiveInfinity);

        private readonly string _id;
        private readonly bool _logIdVerbatimInTelemetry;
        private readonly IVsUIShell _uiShell;

        private bool _isExpanded;
        private readonly double _heightForThreeLineTitle;

        private DifferenceViewerPreview _differenceViewerPreview;
        private readonly IVsRegisterPriorityCommandTarget _registerPriorityCommandTarget;
        private readonly uint _commandTargetCookie = VSConstants.VSCOOKIE_NIL;

        public PreviewPane(
            Image severityIcon,
            string id,
            string title,
            string description,
            Uri helpLink,
            string helpLinkToolTipText,
            IReadOnlyList<object> previewContent,
            bool logIdVerbatimInTelemetry,
            IVsUIShell uiShell,
            Guid optionPageGuid = default)
        {
            InitializeComponent();

            Loaded += PreviewPane_Loaded;

            _id = id;
            _logIdVerbatimInTelemetry = logIdVerbatimInTelemetry;
            _uiShell = uiShell;

            // Initialize header portion.
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

                if (helpLink != null)
                {
                    Contract.ThrowIfNull(helpLinkToolTipText);
                    InitializeHyperlinks(helpLink, helpLinkToolTipText);
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    DescriptionParagraph.Inlines.Add(description);
                }
            }

            _optionPageGuid = optionPageGuid;

            if (optionPageGuid == default)
            {
                OptionsButton.Visibility = Visibility.Collapsed;
            }

            // Initialize preview (i.e. diff view) portion.
            InitializePreviewElement(previewContent);

            _registerPriorityCommandTarget = Package.GetGlobalService(typeof(SVsRegisterPriorityCommandTarget)) as IVsRegisterPriorityCommandTarget;
            Debug.Assert(_registerPriorityCommandTarget != null);

            if (_registerPriorityCommandTarget != null)
            {
                ErrorHandler.ThrowOnFailure(
                    _registerPriorityCommandTarget.RegisterPriorityCommandTarget(
                        dwReserved: 0,
                        pCmdTrgt: this,
                        out _commandTargetCookie));
            }
        }

        public FrameworkElement ParentElement
        {
            get { return (FrameworkElement)GetValue(ParentElementProperty); }
            set { SetValue(ParentElementProperty, value); }
        }

        public static readonly DependencyProperty ParentElementProperty =
            DependencyProperty.Register("ParentElement", typeof(FrameworkElement), typeof(PreviewPane), new PropertyMetadata(null));

        private void PreviewPane_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= PreviewPane_Loaded;
            ParentElement = Parent as FrameworkElement;
        }

        public string AutomationName => ServicesVSResources.Preview_pane;

        private void InitializePreviewElement(IReadOnlyList<object> previewItems)
        {
            var previewElement = CreatePreviewElement(previewItems);

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

        private FrameworkElement CreatePreviewElement(IReadOnlyList<object> previewItems)
        {
            if (previewItems == null || previewItems.Count == 0)
            {
                return null;
            }

            const int MaxItems = 3;
            if (previewItems.Count > MaxItems)
            {
                previewItems = previewItems.Take(MaxItems).Concat("...").ToList();
            }

            var grid = new Grid();

            foreach (var previewItem in previewItems)
            {
                var previewElement = GetPreviewElement(previewItem);

                // no preview element
                if (previewElement == null)
                {
                    continue;
                }

                // the very first preview
                if (grid.RowDefinitions.Count == 0)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                }

                // set row position of the element
                Grid.SetRow(previewElement, grid.RowDefinitions.Count - 1);

                // add the element to the grid
                grid.Children.Add(previewElement);

                // set width of the grid same as the first element
                if (grid.Children.Count == 1)
                {
                    grid.Width = previewElement.Width;
                }
            }

            if (grid.Children.Count == 0)
            {
                // no preview
                return null;
            }

            // if there is only 1 item, just take preview element as it is without grid
            if (grid.Children.Count == 1)
            {
                var preview = grid.Children[0];

                // we need to take it out from visual tree
                grid.Children.Clear();

                return (FrameworkElement)preview;
            }

            return grid;
        }

        private FrameworkElement GetPreviewElement(object previewItem)
        {
            if (previewItem is DifferenceViewerPreview)
            {
                // Contract is there should be only 1 diff viewer, otherwise we leak.
                Contract.ThrowIfFalse(_differenceViewerPreview == null);

                // cache the diff viewer so that we can close it when panel goes away.
                // this is a bit weird since we are mutating state here.
                _differenceViewerPreview = (DifferenceViewerPreview)previewItem;
                var viewer = _differenceViewerPreview.Viewer;
                PreviewDockPanel.Background = viewer.InlineView.Background;

                var previewElement = viewer.VisualElement;
                return previewElement;
            }

            if (previewItem is string s)
            {
                return GetPreviewForString(s);
            }

            if (previewItem is FrameworkElement frameworkElement)
            {
                return frameworkElement;
            }

            // preview item we don't know how to show to users
            return null;
        }

        private void InitializeHyperlinks(Uri helpLink, string helpLinkToolTipText)
        {
            IdHyperlink.Inlines.Add(_id);
            IdHyperlink.NavigateUri = helpLink;
            IdHyperlink.IsEnabled = true;
            IdHyperlink.ToolTip = helpLinkToolTipText;

            LearnMoreHyperlink.Inlines.Add(string.Format(ServicesVSResources.More_about_0, _id));
            LearnMoreHyperlink.NavigateUri = helpLink;
            LearnMoreHyperlink.ToolTip = helpLinkToolTipText;
        }

        private static FrameworkElement GetPreviewForString(string previewContent)
        {
            return new TextBlock()
            {
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Text = previewContent,
                TextWrapping = TextWrapping.Wrap,
            };
        }

        // This method adjusts the width of the header section to match that of the preview content
        // thereby ensuring that the preview pane itself is never wider than the preview content.
        // This method also adjusts the height of the header section so that it displays only three lines
        // worth by default.
        private void AdjustWidthAndHeight(FrameworkElement previewElement)
        {
            double headerStackPanelWidth;
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
                PreviewDockPanel.Measure(availableSize: new Size(
                    double.IsNaN(previewElement.Width) ? DefaultWidth : previewElement.Width,
                    double.PositiveInfinity));
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
            => size > 0 && !double.IsNaN(size) && !double.IsInfinity(size);

        private bool HasDescription
        {
            get
            {
                return DescriptionParagraph.Inlines.Count > 0;
            }
        }

        private readonly Guid _optionPageGuid;
        void IDisposable.Dispose()
        {
            // VS editor will call Dispose at which point we should Close() the embedded IWpfDifferenceViewer.
            _differenceViewerPreview?.Dispose();
            _differenceViewerPreview = null;

            if (_registerPriorityCommandTarget != null && _commandTargetCookie != VSConstants.VSCOOKIE_NIL)
            {
                _registerPriorityCommandTarget.UnregisterPriorityCommandTarget(_commandTargetCookie);
            }
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_differenceViewerPreview != null)
            {
                return _differenceViewerPreview.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            return VSConstants.S_OK;
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_differenceViewerPreview != null)
            {
                return _differenceViewerPreview.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            return (int)VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;
        }

        private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            VisualStudioNavigateToLinkService.StartBrowser(e.Uri);
            e.Handled = true;

            if (sender is not Hyperlink hyperlink)
            {
                return;
            }

            DiagnosticLogger.LogHyperlink(hyperlink.Name ?? "Preview", _id, HasDescription, _logIdVerbatimInTelemetry, e.Uri.AbsoluteUri);
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

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_optionPageGuid != default)
            {
                ErrorHandler.ThrowOnFailure(_uiShell.PostExecCommand(
                    VSConstants.GUID_VSStandardCommandSet97,
                    (uint)VSConstants.VSStd97CmdID.ToolsOptions,
                    (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                    _optionPageGuid.ToString()));
            }
        }
    }
}
