// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal sealed class ValueTrackedTreeItemViewModel : TreeItemViewModel
    {
        private bool _childrenCalculated;
        private readonly Solution _solution;
        private readonly IGlyphService _glyphService;
        private readonly IValueTrackingService _valueTrackingService;
        private readonly ValueTrackedItem _trackedItem;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;

        public override bool IsNodeExpanded
        {
            get => base.IsNodeExpanded;
            set
            {
                base.IsNodeExpanded = value;
                CalculateChildren();
            }
        }

        private ValueTrackedTreeItemViewModel(
            ValueTrackedItem trackedItem,
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            Solution solution,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IValueTrackingService valueTrackingService,
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            string fileName,
            ImmutableArray<TreeItemViewModel> children,
            IAsynchronousOperationListener listener,
            IUIThreadOperationExecutor threadOperationExecutor)
            : base(
                  trackedItem.Span,
                  trackedItem.SourceText,
                  trackedItem.DocumentId,
                  fileName,
                  trackedItem.Glyph,
                  classifiedSpans,
                  treeViewModel,
                  glyphService,
                  threadingContext,
                  solution.Workspace,
                  children)
        {

            _trackedItem = trackedItem;
            _solution = solution;
            _glyphService = glyphService;
            _valueTrackingService = valueTrackingService;
            _globalOptions = globalOptions;
            _listener = listener;
            _threadOperationExecutor = threadOperationExecutor;
            if (children.IsEmpty)
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

        internal static async ValueTask<TreeItemViewModel> CreateAsync(
            Solution solution,
            ValueTrackedItem item,
            ImmutableArray<TreeItemViewModel> children,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IValueTrackingService valueTrackingService,
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener listener,
            IUIThreadOperationExecutor threadOperationExecutor,
            CancellationToken cancellationToken)
        {
            var document = solution.GetRequiredDocument(item.DocumentId);
            var fileName = document.FilePath ?? document.Name;

            var options = globalOptions.GetClassificationOptions(document.Project.Language);
            var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(document, item.Span, options, cancellationToken).ConfigureAwait(false);
            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, options, cancellationToken).ConfigureAwait(false);
            var classifiedSpans = classificationResult.ClassifiedSpans;

            return new ValueTrackedTreeItemViewModel(
                item,
                classifiedSpans,
                solution,
                treeViewModel,
                glyphService,
                valueTrackingService,
                globalOptions,
                threadingContext,
                fileName,
                children,
                listener,
                threadOperationExecutor);
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

            Task.Run(async () =>
            {
                try
                {
                    var children = await CalculateChildrenAsync(ThreadingContext.DisposalToken).ConfigureAwait(false);

                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    ChildItems.Clear();

                    foreach (var child in children)
                    {
                        ChildItems.Add(child);
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

        public override void NavigateTo()
        {
            var token = _listener.BeginAsyncOperation(nameof(NavigateTo));
            NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
        }

        private async Task NavigateToAsync()
        {
            using var context = _threadOperationExecutor.BeginExecute(
                ServicesVSResources.Value_Tracking, ServicesVSResources.Navigating, allowCancellation: true, showProgress: false);

            var navigationService = Workspace.Services.GetService<IDocumentNavigationService>();
            if (navigationService is null)
                return;

            // While navigating do not activate the tab, which will change focus from the tool window
            var options = new NavigationOptions(PreferProvisionalTab: true, ActivateTab: false);
            await navigationService.TryNavigateToSpanAsync(
                this.ThreadingContext, Workspace, DocumentId, _trackedItem.Span, options, context.UserCancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<TreeItemViewModel>> CalculateChildrenAsync(CancellationToken cancellationToken)
        {
            var valueTrackedItems = await _valueTrackingService.TrackValueSourceAsync(
                _solution, _trackedItem, cancellationToken).ConfigureAwait(false);

            return await valueTrackedItems.SelectAsArrayAsync(static (item, self, cancellationToken) =>
                CreateAsync(
                    self._solution, item, children: ImmutableArray<TreeItemViewModel>.Empty,
                    self.TreeViewModel, self._glyphService, self._valueTrackingService, self._globalOptions,
                    self.ThreadingContext, self._listener, self._threadOperationExecutor, cancellationToken), this, cancellationToken).ConfigureAwait(false);
        }
    }
}
