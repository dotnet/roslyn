// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal partial class InheritanceMargin
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly IWaitIndicator _waitIndicator;
        private readonly Workspace _workspace;

        /// <summary>
        /// Note: This name is used in xaml file.
        /// </summary>
        private const string SingleMemberContextMenuStyle = nameof(SingleMemberContextMenuStyle);

        /// <summary>
        /// Note: This name is used in xaml file.
        /// </summary>
        private const string MultipleMembersContextMenuStyle = nameof(MultipleMembersContextMenuStyle);

        public InheritanceMargin(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            IWaitIndicator waitIndicator,
            InheritanceMarginTag tag)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _workspace = tag.Workspace;
            _waitIndicator = waitIndicator;
            InitializeComponent();

            var viewModel = InheritanceMarginViewModel.Create(classificationTypeMap, classificationFormatMap, tag);
            DataContext = viewModel;
            ContextMenu.DataContext = viewModel;
            ToolTip = new ToolTip { Content = viewModel.ToolTipTextBlock, Style = (Style)FindResource("ToolTipStyle") };

            if (tag.MembersOnLine.Length == 1)
            {
                ContextMenu.Style = (Style)FindResource(SingleMemberContextMenuStyle);
            }
            else
            {
                ContextMenu.Style = (Style)FindResource(MultipleMembersContextMenuStyle);
            }
        }

        private void InheritanceMargin_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void TargetMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem { DataContext: TargetDisplayViewModel viewModel })
            {
                _waitIndicator.Wait(
                    title: EditorFeaturesResources.Navigating,
                    message: string.Format(EditorFeaturesWpfResources.Navigate_to_0, viewModel.DisplayContent),
                    allowCancel: true,
                    context => GoToDefinitionHelpers.TryGoToDefinition(
                        ImmutableArray.Create(viewModel.DefinitionItem),
                        _workspace,
                        string.Format(EditorFeaturesResources._0_declarations, viewModel.DisplayContent),
                        _threadingContext,
                        _streamingFindUsagesPresenter,
                        context.CancellationToken));
            }
        }

        private void ChangeBorderToHoveringColor()
        {
            SetResourceReference(BackgroundProperty, "VsBrush.CommandBarMenuBackgroundGradient");
            SetResourceReference(BorderBrushProperty, "VsBrush.CommandBarMenuBorder");
        }

        private void ResetBorderToInitialColor()
        {
            this.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
        }

        private void InheritanceMargin_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ChangeBorderToHoveringColor();
        }

        private void InheritanceMargin_OnMouseLeave(object sender, MouseEventArgs e)
        {
            // If the context menu is open, then don't reset the color of the button because we need
            // the margin looks like being pressed.
            if (!ContextMenu.IsOpen)
            {
                ResetBorderToInitialColor();
            }
        }

        private void ContextMenu_OnClose(object sender, RoutedEventArgs e)
        {
            ResetBorderToInitialColor();
        }
    }
}

