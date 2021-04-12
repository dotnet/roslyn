// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows.Documents;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class ValueTrackedTreeItemViewModel : ValueTrackingTreeItemViewModel
    {
        private bool _childrenCalculated;
        private readonly Solution _solution;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IGlyphService _glyphService;
        private readonly IValueTrackingService _valueTrackingService;
        private readonly ValueTrackedItem _trackedItem;

        public ValueTrackedTreeItemViewModel(
            ValueTrackedItem trackedItem,
            Solution solution,
            ValueTrackingTreeViewModel treeViewModel,
            IGlyphService glyphService,
            IValueTrackingService valueTrackingService,
            IThreadingContext threadingContext,
            ImmutableArray<ValueTrackingTreeItemViewModel> children = default)
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

            _classificationFormatMap = treeViewModel.ClassificationFormatMap;
            _classificationTypeMap = treeViewModel.ClassificationTypeMap;

            if (children.IsDefaultOrEmpty)
            {
                // Add an empty item so the treeview has an expansion showing to calculate
                // the actual children of the node
                ChildItems.Add(EmptyTreeViewItem.Instance);

                ChildItems.CollectionChanged += (s, a) =>
                {
                    NotifyPropertyChanged(nameof(ChildItems));

                    if (a.NewItems is null)
                    {
                        return;
                    }

                    foreach (var addedChild in a.NewItems.Cast<ValueTrackingTreeItemViewModel>())
                    {
                        addedChild.Parent = this;
                    }
                };

                PropertyChanged += (s, a) =>
                {
                    if (a.PropertyName == nameof(IsNodeExpanded))
                    {
                        CalculateChildren();
                    }
                };
            }
        }

        private void CalculateChildren()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_childrenCalculated)
            {
                return;
            }

            _childrenCalculated = true;

            CalculateChildrenAsync(CancellationToken.None)
                 .ContinueWith(task =>
                 {
                     if (task.Exception is not null)
                     {
                         _childrenCalculated = false;
                         return;
                     }

                     ChildItems.Clear();

                     foreach (var item in task.Result)
                     {
                         ChildItems.Add(item);
                     }
                 },

                 // Use the UI thread synchronization context for calling back to the UI
                 // thread to add the tiems
                 TaskScheduler.FromCurrentSynchronizationContext());
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

            navigationService.TryNavigateToSpan(workspace, Document.Id, _trackedItem.Span, options, ThreadingContext.DisposalToken);
        }

        protected override IList<Inline> GetInlines(int maxLength)
        {
            var isTruncated = _trackedItem.GetTruncatedClassifiedSpans(maxLength, out var beginning, out var middle, out var end, out var totalLength);

            var inlines = ToInlines(beginning);

            if (isTruncated && middle.Start > 0)
            {
                // Show a preamble of "..." 
                inlines = inlines.Add(new Run("..."));
            }

            inlines = inlines.AddRange(ToInlines(middle));

            if (isTruncated && (beginning.Length + middle.Length) < totalLength)
            {
                // Show a postamble of "..."
                inlines = inlines.Add(new Run("..."));
            }

            inlines = inlines.AddRange(ToInlines(end));

            return inlines;


            ImmutableArray<Inline> ToInlines(ValueTrackedItem.TruncatedClassifiedSpans truncatedSpans)
            {
                if (truncatedSpans.IsEmpty)
                {
                    return ImmutableArray<Inline>.Empty;
                }

                var classifiedText = truncatedSpans.Spans.SelectAsArray(
                   cs => new ClassifiedText(cs.ClassificationType, SourceText.ToString(cs.TextSpan)));

                return classifiedText.ToInlines(
                    TreeViewModel.ClassificationFormatMap,
                    TreeViewModel.ClassificationTypeMap,
                    (run, text, position) => BoldRunIfNeeded(run, position, _trackedItem.Span.Start - truncatedSpans.Start, _trackedItem.Span.End - truncatedSpans.Start))
                    .ToImmutableArray();
            }
        }

        private async Task<ImmutableArray<ValueTrackingTreeItemViewModel>> CalculateChildrenAsync(CancellationToken cancellationToken)
        {
            var valueTrackedItems = await _valueTrackingService.TrackValueSourceAsync(
                _trackedItem,
                cancellationToken).ConfigureAwait(false);

            // TODO: Use pooled item here? 
            var builder = new List<ValueTrackingTreeItemViewModel>();
            foreach (var valueTrackedItem in valueTrackedItems)
            {
                builder.Add(new ValueTrackedTreeItemViewModel(
                    valueTrackedItem,
                    _solution,
                    TreeViewModel,
                    _glyphService,
                    _valueTrackingService,
                    ThreadingContext));
            }

            return builder.ToImmutableArray();
        }
    }
}
