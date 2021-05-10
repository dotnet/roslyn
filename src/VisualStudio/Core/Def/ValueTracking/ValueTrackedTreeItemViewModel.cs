// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class ValueTrackedTreeItemViewModel : TreeItemViewModel
    {
        private bool _childrenCalculated;
        private readonly Solution _solution;
        private readonly IGlyphService _glyphService;
        private readonly IValueTrackingService _valueTrackingService;
        private readonly ValueTrackedItem _trackedItem;

        public override bool IsNodeExpanded
        {
            get => base.IsNodeExpanded;
            set
            {
                base.IsNodeExpanded = value;
                CalculateChildren();
            }
        }

        public ValueTrackedTreeItemViewModel(
            ValueTrackedItem trackedItem,
            Solution solution,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IValueTrackingService valueTrackingService,
            IThreadingContext threadingContext,
            ImmutableArray<TreeItemViewModel> children = default)
            : base(
                  trackedItem.Document,
                  trackedItem.Span,
                  trackedItem.SourceText,
                  trackedItem.Symbol,
                  trackedItem.ClassifiedSpans,
                  treeViewModel,
                  glyphService,
                  threadingContext,
                  children: children)
        {

            _trackedItem = trackedItem;
            _solution = solution;
            _glyphService = glyphService;
            _valueTrackingService = valueTrackingService;

            if (children.IsDefaultOrEmpty)
            {
                // Add an empty item so the treeview has an expansion showing to calculate
                // the actual children of the node
                ChildItems.Add(EmptyTreeViewItem.Instance);

                ChildItems.CollectionChanged += (s, a) =>
                {
                    NotifyPropertyChanged(nameof(ChildItems));
                };
            }
        }

        private void CalculateChildren()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_childrenCalculated || IsLoading)
            {
                return;
            }

            TreeViewModel.LoadingCount++;
            IsLoading = true;
            ChildItems.Clear();

            var computingItem = new ComputingTreeViewItem();
            ChildItems.Add(computingItem);

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var items = await _valueTrackingService.TrackValueSourceAsync(_trackedItem, ThreadingContext.DisposalToken).ConfigureAwait(false);

                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    ChildItems.Clear();

                    foreach (var valueTrackedItem in items)
                    {
                        ChildItems.Add(new ValueTrackedTreeItemViewModel(
                                valueTrackedItem,
                                _solution,
                                TreeViewModel,
                                _glyphService,
                                _valueTrackingService,
                                ThreadingContext));
                    }
                }
                finally
                {
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    TreeViewModel.LoadingCount--;
                    _childrenCalculated = true;
                    IsLoading = false;
                }
            }, ThreadingContext.DisposalToken);
        }

        public override void Select()
        {
            var workspace = Document.Project.Solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService is null)
            {
                return;
            }

            // While navigating do not activate the tab, which will change focus from the tool window
            var options = workspace.Options
                .WithChangedOption(new OptionKey(NavigationOptions.PreferProvisionalTab), true)
                .WithChangedOption(new OptionKey(NavigationOptions.ActivateTab), false);

            _ = navigationService.TryNavigateToSpan(workspace, Document.Id, _trackedItem.Span, options, ThreadingContext.DisposalToken);
        }
    }
}
