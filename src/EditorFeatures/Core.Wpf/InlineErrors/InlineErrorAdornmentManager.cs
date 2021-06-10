// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal class InlineErrorAdornmentManager : AdornmentManager<InlineErrorTag>
    {
        private readonly IClassificationTypeRegistryService _classificationRegistryService;
        private readonly IClassificationFormatMap _formatMap;
        private readonly Dictionary<IMappingTagSpan<InlineErrorTag>, SnapshotPoint> _tagSpanToPointMap;

        public InlineErrorAdornmentManager(IThreadingContext threadingContext,
            IWpfTextView textView, IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener, string adornmentLayerName,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
            _classificationRegistryService = classificationTypeRegistryService;
            _formatMap = classificationFormatMapService.GetClassificationFormatMap(textView);
            _formatMap.ClassificationFormatMappingChanged += OnClassificationFormatMappingChanged;
            _tagSpanToPointMap = new Dictionary<IMappingTagSpan<InlineErrorTag>, SnapshotPoint>();
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (AdornmentLayer is not null)
            {
                var elements = AdornmentLayer.Elements;
                foreach (var element in elements)
                {
                    var tag = (InlineErrorTag)element.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType(InlineErrorTag.TagID + tag.ErrorType);
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
        /// Get the spans located on each line so that I can only display the first one that appears on the line
        /// </summary>
        private ImmutableDictionary<int, List<IMappingTagSpan<InlineErrorTag>>> GetSpansOnEachLine(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            _tagSpanToPointMap.Clear();
            if (changedSpanCollection.IsEmpty())
            {
                return ImmutableDictionary<int, List<IMappingTagSpan<InlineErrorTag>>>.Empty;
            }

            var map = ImmutableDictionary.CreateBuilder<int, List<IMappingTagSpan<InlineErrorTag>>>();
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
                        // span is outside of the view so we will not get geometry for it, but may 
                        // spent a lot of time trying.
                        continue;
                    }

                    var lineNum = mappedPoint.Value.GetContainingLine().LineNumber;
                    if (!map.TryGetValue(lineNum, out var list))
                    {
                        list = new List<IMappingTagSpan<InlineErrorTag>>();
                        map.Add(lineNum, list);
                    }

                    list.Add(tagMappingSpan);
                    _tagSpanToPointMap.Add(tagMappingSpan, point.Value);
                }
            }

            return map.ToImmutable();
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
            foreach (var (lineNum, tagMappingSpanList) in map)
            {
                var tagMappingSpan = GetHighestOrderTag(tagMappingSpanList);
                if (tagMappingSpan != null)
                {
                    TryMapToSingleSnapshotSpan(tagMappingSpan.Span, TextView.TextSnapshot, out var span);
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpan.Tag;
                        var classificationType = _classificationRegistryService.GetClassificationType(InlineErrorTag.TagID + tag.ErrorType);
                        var graphicsResult = tag.GetGraphics(TextView, geometry, GetFormat(classificationType));
                        if (!_tagSpanToPointMap.TryGetValue(tagMappingSpan, out var point))
                        {
                            continue;
                        }

                        var lineView = TextView.GetTextViewLineContainingBufferPosition(point);

                        var visualElement = graphicsResult.VisualElement;
                        if (tag.Location is InlineErrorsLocations.HookedToCode)
                        {
                            Canvas.SetLeft(visualElement, lineView.Right);
                        }
                        else if (tag.Location is InlineErrorsLocations.HookedToWindow)
                        {
                            Canvas.SetLeft(visualElement, TextView.ViewportWidth - visualElement.DesiredSize.Width);
                        }

                        Canvas.SetTop(visualElement, geometry.Bounds.Bottom - visualElement.DesiredSize.Height);

                        if (lineView.Right < TextView.ViewportWidth - visualElement.DesiredSize.Width)
                        {
                            AdornmentLayer.AddAdornment(
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

        private static IMappingTagSpan<InlineErrorTag>? GetHighestOrderTag(List<IMappingTagSpan<InlineErrorTag>> list)
        {
            foreach (var span in list)
            {
                if (span.Tag.ErrorType is PredefinedErrorTypeNames.SyntaxError)
                {
                    return span;
                }
            }

            if (list.Count > 0)
            {
                return list[0];
            }

            return null;
        }
    }
}
