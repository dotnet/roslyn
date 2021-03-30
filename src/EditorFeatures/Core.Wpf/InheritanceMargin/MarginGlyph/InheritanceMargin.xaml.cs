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
            IWaitIndicator waitIndicator,
            InheritanceMarginTag tag)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _workspace = tag.Workspace;
            _waitIndicator = waitIndicator;
            InitializeComponent();
            if (tag.MembersOnLine.Length == 1)
            {
                var viewModel = new SingleMemberMarginViewModel(tag);
                DataContext = viewModel;
                ContextMenu.DataContext = viewModel;
                ContextMenu.Style = (Style)FindResource(SingleMemberContextMenuStyle);
            }
            else
            {
                var viewModel = new MultipleMembersMarginViewModel(tag);
                DataContext = viewModel;
                ContextMenu.DataContext = viewModel;
                ContextMenu.Style = (Style)FindResource(MultipleMembersContextMenuStyle);
            }
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
    }
}

