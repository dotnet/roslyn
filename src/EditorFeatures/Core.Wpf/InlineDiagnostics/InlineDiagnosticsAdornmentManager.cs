// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    internal class InlineDiagnosticsAdornmentManager : AdornmentManager<InlineDiagnosticsTag>
    {
        private readonly IClassificationTypeRegistryService _classificationRegistryService;
        private readonly IClassificationFormatMap _formatMap;

        public InlineDiagnosticsAdornmentManager(
            IThreadingContext threadingContext, IWpfTextView textView, IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener, string adornmentLayerName,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
            _classificationRegistryService = classificationTypeRegistryService;
            _formatMap = classificationFormatMapService.GetClassificationFormatMap(textView);
            _formatMap.ClassificationFormatMappingChanged += OnClassificationFormatMappingChanged;
            TextView.ViewportWidthChanged += TextView_ViewportWidthChanged;
        }

        private void TextView_ViewportWidthChanged(object sender, EventArgs e)
        {
            if (AdornmentLayer is not null)
            {
                var normalizedCollectionSpan = new NormalizedSnapshotSpanCollection(TextView.TextViewLines.FormattedSpan);
                UpdateSpans_CallOnlyOnUIThread(normalizedCollectionSpan, removeOldTags: true);
            }
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (AdornmentLayer is not null)
            {
                var elements = AdornmentLayer.Elements;
                foreach (var element in elements)
                {
                    var tag = (InlineDiagnosticsTag)element.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType(InlineDiagnosticsTag.TagID + tag.ErrorType);
                    var format = GetFormat(classificationType);
                    tag.UpdateColor(format, element.Adornment);
                }
            }
        }

        private TextFormattingRunProperties GetFormat(IClassificationType classificationType)
        {
            return _formatMap.GetTextProperties(classificationType);
        }

        /// <summary>
        /// Get the spans located on each line so that it can only display the first one that appears on the line
        /// </summary>
        private IDictionary<int, (IMappingTagSpan<InlineDiagnosticsTag> mapTagSpan, SnapshotSpan snapshotSpan)> GetSpansOnEachLine(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            if (changedSpanCollection.IsEmpty())
            {
                return SpecializedCollections.EmptyDictionary<int, (IMappingTagSpan<InlineDiagnosticsTag>, SnapshotSpan)>();
            }

            var map = new Dictionary<int, (IMappingTagSpan<InlineDiagnosticsTag> mapTagSpan, SnapshotSpan snapshotSpan)>();
            var viewSnapshot = TextView.TextSnapshot;
            var viewLines = TextView.TextViewLines;

            foreach (var changedSpan in changedSpanCollection)
            {
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = TagAggregator.GetTags(changedSpan);
                foreach (var tagMappingSpan in tagSpans)
                {
                    var point = tagMappingSpan.Span.Start.GetPoint(changedSpan.Snapshot, PositionAffinity.Predecessor);
                    if (point == null)
                    {
                        continue;
                    }

                    var mappedPoint = TextView.BufferGraph.MapUpToSnapshot(
                        point.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, TextView.VisualSnapshot);
                    if (mappedPoint == null)
                    {
                        continue;
                    }

                    if (!TryMapToSingleSnapshotSpan(tagMappingSpan.Span, viewSnapshot, out var span))
                    {
                        continue;
                    }

                    if (!viewLines.IntersectsBufferSpan(span))
                    {
                        continue;
                    }

                    var lineNum = mappedPoint.Value.GetContainingLine().LineNumber;
                    if (!map.TryGetValue(lineNum, out var value))
                    {
                        map.Add(lineNum, (tagMappingSpan, changedSpan));
                    }

                    if (value.mapTagSpan is not null && value.mapTagSpan.Tag.ErrorType is not PredefinedErrorTypeNames.SyntaxError)
                    {
                        map[lineNum] = (tagMappingSpan, changedSpan);
                    }
                }
            }

            return map;
        }

        protected override void UpdateSpans_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection, bool removeOldTags)
        {
            Contract.ThrowIfNull(changedSpanCollection);

            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

            var viewLines = TextView.TextViewLines;
            if (viewLines == null || viewLines.Count == 0)
            {
                return; // nothing to draw on
            }

            // removing is a separate pass from adding so that new stuff is not removed.
            if (removeOldTags)
            {
                foreach (var changedSpan in changedSpanCollection)
                {
                    // is there any effect on the view?
                    if (viewLines.IntersectsBufferSpan(changedSpan))
                    {
                        AdornmentLayer.RemoveAdornmentsByVisualSpan(changedSpan);
                    }
                }
            }

            var map = GetSpansOnEachLine(changedSpanCollection);
            foreach (var (lineNum, spanTuple) in map)
            {
                var tagMappingSpan = spanTuple.mapTagSpan;
                TryMapToSingleSnapshotSpan(tagMappingSpan.Span, TextView.TextSnapshot, out var span);
                var geometry = viewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    var tag = tagMappingSpan.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType(InlineDiagnosticsTag.TagID + tag.ErrorType);
                    var graphicsResult = tag.GetGraphics(TextView, geometry, GetFormat(classificationType));

                    var point = tagMappingSpan.Span.Start.GetPoint(spanTuple.snapshotSpan.Snapshot, PositionAffinity.Predecessor);
                    if (point == null)
                    {
                        continue;
                    }
                    var lineView = TextView.GetTextViewLineContainingBufferPosition(point.Value);

                    var visualElement = graphicsResult.VisualElement;
                    if (tag.Location is InlineDiagnosticsLocations.PlacedAtEndOfCode)
                    {
                        Canvas.SetLeft(visualElement, lineView.Right);
                    }
                    else if (tag.Location is InlineDiagnosticsLocations.PlacedAtEndOfEditor)
                    {
                        Canvas.SetLeft(visualElement, TextView.ViewportWidth - visualElement.DesiredSize.Width);
                    }

                    Canvas.SetTop(visualElement, geometry.Bounds.Bottom - visualElement.DesiredSize.Height);

                    if (lineView.Right < TextView.ViewportWidth - visualElement.DesiredSize.Width)
                    {
                        AdornmentLayer.AddAdornment(
                            // TextRelative because the adornment is positioned relative to the text in the editor
                            behavior: AdornmentPositioningBehavior.TextRelative,
                            visualSpan: span,
                            tag: tag,
                            adornment: visualElement,
                            removedCallback: delegate { graphicsResult.Dispose(); });
                    }
                }
            }
        }
    }
}
