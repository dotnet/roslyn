// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal partial class InheritanceMargin
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly Workspace _workspace;

        /// <summary>
        /// Note: This name is also used in xaml file.
        /// </summary>
        private const string SingleMemberContextMenuStyle = nameof(SingleMemberContextMenuStyle);

        /// <summary>
        /// Note: This name is also used in xaml file.
        /// </summary>
        private const string MultipleMembersContextMenuStyle = nameof(MultipleMembersContextMenuStyle);

        public static InheritanceMargin CreateForSingleMember(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Workspace workspace,
            SingleMemberMarginViewModel viewModel)
        {
            var margin = new InheritanceMargin(threadingContext, streamingFindUsagesPresenter, workspace);
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
            Workspace workspace,
            MultipleMembersMarginViewModel viewModel)
        {
            var margin = new InheritanceMargin(threadingContext, streamingFindUsagesPresenter, workspace);

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
            Workspace workspace)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _workspace = workspace;
            InitializeComponent();
        }

        public InheritanceMargin(
            ThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            Workspace workspace,
            MultipleMembersMarginViewModel viewModel)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _workspace = workspace;

            InitializeComponent();
            this.DataContext = viewModel;

            // This is created in the xaml file.
            var contextMenu = this.ContextMenu!;
            contextMenu.DataContext = viewModel;
            contextMenu.Style = (Style)FindResource("MultipleMembersContextMenuStyle");
        }

        private void Margin_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void TargetMenuItem_OnHandler(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem && menuItem.DataContext is TargetDisplayViewModel viewModel)
            {
                _streamingFindUsagesPresenter.TryNavigateToOrPresentItemsAsync(
                    _threadingContext,
                    _workspace,
                    $"Navigate to {viewModel.DisplayName}.",
                    ImmutableArray.Create(viewModel.DefinitionItem),
                    // TODO: We should have a cancellationToken instead of none here.
                    CancellationToken.None);
            }
        }
    }
}

