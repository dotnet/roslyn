// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal partial class InheritanceMargin
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly IWaitIndicator _waitIndicator;
        private readonly Solution _solution;

        /// <summary>
        /// Note: This name is used in xaml file.
        /// </summary>
        private const string SingleMemberContextMenuStyle = nameof(SingleMemberContextMenuStyle);

        /// <summary>
        /// Note: This name is used in xaml file.
        /// </summary>
        private const string MultipleMembersContextMenuStyle = nameof(MultipleMembersContextMenuStyle);

        public static InheritanceMargin CreateForSingleMember(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IWaitIndicator waitIndicator,
            Solution solution,
            SingleMemberMarginViewModel viewModel)
        {
            var margin = new InheritanceMargin(threadingContext, streamingFindUsagesPresenter, waitIndicator, solution);
            // This is created in the xaml file.
            margin.DataContext = viewModel;
            var contextMenu = margin.ContextMenu!;
            contextMenu.DataContext = viewModel;
            contextMenu.Style = (Style)margin.FindResource(SingleMemberContextMenuStyle);
            return margin;
        }

        public static InheritanceMargin CreateForMultipleMembers(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IWaitIndicator waitIndicator,
            Solution solution,
            MultipleMembersMarginViewModel viewModel)
        {
            var margin = new InheritanceMargin(threadingContext, streamingFindUsagesPresenter, waitIndicator, solution);
            // This is created in the xaml file.
            margin.DataContext = viewModel;
            var contextMenu = margin.ContextMenu!;
            contextMenu.DataContext = viewModel;
            contextMenu.Style = (Style)margin.FindResource(MultipleMembersContextMenuStyle);
            return margin;
        }

        private InheritanceMargin(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IWaitIndicator waitIndicator,
            Solution solution)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _solution = solution;
            _waitIndicator = waitIndicator;
            InitializeComponent();
        }

        private void Margin_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void TargetMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem && menuItem.DataContext is TargetDisplayViewModel viewModel)
            {
                _waitIndicator.Wait(
                    title: EditorFeaturesResources.Navigating,
                    message: EditorFeaturesResources.Navigating_to_definition,
                    allowCancel: true,
                    context => GoToDefinitionHelpers.TryGoToDefinition(
                        ImmutableArray.Create(viewModel.DefinitionItem),
                        _solution,
                        string.Format(EditorFeaturesResources._0_declarations, viewModel.DisplayName),
                        _threadingContext,
                        _streamingFindUsagesPresenter,
                        context.CancellationToken)
                );
            }
        }
    }
}

