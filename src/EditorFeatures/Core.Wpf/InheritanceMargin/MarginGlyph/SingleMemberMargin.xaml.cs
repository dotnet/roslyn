// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal partial class SingleMemberMargin
    {
        private readonly ImmutableArray<InheritanceTargetItem> _targetItems;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly IThreadingContext _threadingContext;
        private readonly Solution _solution;

        public SingleMemberMargin(
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IThreadingContext threadingContext,
            InheritanceMarginTag tag)
        {
            var members = tag.MembersOnLine;
            Debug.Assert(members.Length == 1);

            _targetItems = tag.MembersOnLine[0].TargetItems;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _threadingContext = threadingContext;
            InitializeComponent();
            InitializeForSingleMember(tag);
        }

        private void InitializeForSingleMember(InheritanceMarginTag tag)
        {
            var viewModel = new SingleMemberMarginViewModel(tag);
            this.DataContext = viewModel;
            // Context menu doesn't belongs to the same visual tree as its parent, so set its DataContext explicitly.
            if (this.ContextMenu != null)
            {
                this.ContextMenu.DataContext = viewModel;
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

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem)
            {
                var definitionItem = _targetItems[menuItem.Index].DefinitionItem;
               GoToDefinitionHelpers.TryGoToDefinition(
                   ImmutableArray.Create(definitionItem),
                   _solution,
                   "Find",
                   _threadingContext,
                   _streamingFindUsagesPresenter,
                   CancellationToken.None);
            }
        }
    }
}

