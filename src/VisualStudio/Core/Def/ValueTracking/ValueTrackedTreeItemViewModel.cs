// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ValueTracking;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Language.Intellisense;

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
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            IGlyphService glyphService,
            IValueTrackingService valueTrackingService,
            ImmutableArray<ValueTrackingTreeItemViewModel> children = default)
            : base(
                  trackedItem.Document,
                  trackedItem.LineSpan.Start,
                  trackedItem.SourceText,
                  trackedItem.Symbol,
                  trackedItem.ClassifiedSpans,
                  classificationFormatMap,
                  classificationTypeMap,
                  glyphService)
        {

            _trackedItem = trackedItem;
            _solution = solution;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;
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

        private async Task<ImmutableArray<ValueTrackingTreeItemViewModel>> CalculateChildrenAsync(CancellationToken cancellationToken)
        {
            var valueTrackedItems = await _valueTrackingService.TrackValueSourceAsync(
                _solution,
                _trackedItem,
                cancellationToken).ConfigureAwait(false);

            // TODO: Use pooled item here? 
            var builder = new List<ValueTrackingTreeItemViewModel>();
            foreach (var valueTrackedItem in valueTrackedItems)
            {
                builder.Add(new ValueTrackedTreeItemViewModel(
                    valueTrackedItem,
                    _solution,
                    _classificationFormatMap,
                    _classificationTypeMap,
                    _glyphService,
                    _valueTrackingService));
            }

            return builder.ToImmutableArray();
        }
    }
}
