// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineErrors
{
    internal class InlineErrorAdornmentManager : AdornmentManager<GraphicsTag>
    {
        private readonly IClassificationType _classificationType;
        private readonly IClassificationFormatMap _formatMap;
        /// <summary>
        /// Lazy initialized, use <see cref="Format"/> for access
        /// </summary>
        private TextFormattingRunProperties? _format;

        public InlineErrorAdornmentManager(IThreadingContext threadingContext,
            IWpfTextView textView, IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener, string adornmentLayerName,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
            _classificationType = classificationTypeRegistryService.GetClassificationType(InlineErrorTag.TagId);
            _formatMap = classificationFormatMapService.GetClassificationFormatMap(textView);
            _formatMap.ClassificationFormatMappingChanged += OnClassificationFormatMappingChanged;
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            if (_format != null)
            {
                _format = null;
            }
        }

        /*public override AdornmentManager<GraphicsTag> Create(
            IThreadingContext threadingContext,
            IWpfTextView textView,
            IViewTagAggregatorFactoryService aggregatorService,
            IAsynchronousOperationListener asyncListener,
            string adornmentLayerName)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(aggregatorService);
            Contract.ThrowIfNull(adornmentLayerName);
            Contract.ThrowIfNull(asyncListener);

            return new InlineErrorAdornmentManager(threadingContext, textView, aggregatorService, asyncListener, adornmentLayerName);
        }*/

        private TextFormattingRunProperties Format
        {
            get
            {
                _format ??= _formatMap.GetTextProperties(_classificationType);
                return _format;
            }
        }

        private Dictionary<int, List<IMappingTagSpan<GraphicsTag>>> GetSpansOnOwnLine(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            var map = new Dictionary<int, List<IMappingTagSpan<GraphicsTag>>>();
            var viewSnapshot = _textView.TextSnapshot;
            var viewLines = _textView.TextViewLines;

            foreach (var changedSpan in changedSpanCollection)
            {
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = _tagAggregator.GetTags(changedSpan);
                foreach (var tagMappingSpan in tagSpans)
                {
                    // We don't want to draw line separators if they would intersect a collapsed outlining
                    // region.  So we test if we can map the start of the line separator up to our visual 
                    // snapshot. If we can't, then we just skip it.
                    var point = tagMappingSpan.Span.Start.GetPoint(changedSpan.Snapshot, PositionAffinity.Predecessor);
                    if (point == null)
                    {
                        continue;
                    }

                    var mappedPoint = _textView.BufferGraph.MapUpToSnapshot(
                        point.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, _textView.VisualSnapshot);
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
                        list = new List<IMappingTagSpan<GraphicsTag>>();
                        map.Add(lineNum, list);
                    }

                    list.Add(tagMappingSpan);
                }
            }

            return map;
        }

        protected override void UpdateSpans_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection, bool removeOldTags)
        {
            Contract.ThrowIfNull(changedSpanCollection);

            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(_textView.VisualElement.Dispatcher.CheckAccess());

            var viewLines = _textView.TextViewLines;
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
                        _adornmentLayer.RemoveAdornmentsByVisualSpan(changedSpan);
                    }
                }
            }

            var map = GetSpansOnOwnLine(changedSpanCollection);
            foreach (var (lineNum, tagMappingSpanList) in map)
            {
                if (tagMappingSpanList.Count >= 1)
                {
                    TryMapToSingleSnapshotSpan(tagMappingSpanList[0].Span, _textView.TextSnapshot, out var span);
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpanList[0].Tag;
                        var graphicsResult = tag.GetGraphics(_textView, geometry, Format);
                        _adornmentLayer.AddAdornment(
                            behavior: AdornmentPositioningBehavior.TextRelative,
                            visualSpan: span,
                            tag: tag,
                            adornment: graphicsResult.VisualElement,
                            removedCallback: delegate { graphicsResult.Dispose(); });
                    }
                }
            }
            /*foreach (var changedSpan in changedSpanCollection)
            {
                // is there any effect on the view?
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = _tagAggregator.GetTags(changedSpan);
                foreach (var tagMappingSpan in tagSpans)
                {
                    // We don't want to draw line separators if they would intersect a collapsed outlining
                    // region.  So we test if we can map the start of the line separator up to our visual 
                    // snapshot. If we can't, then we just skip it.
                    var point = tagMappingSpan.Span.Start.GetPoint(changedSpan.Snapshot, PositionAffinity.Predecessor);
                    if (point == null)
                    {
                        continue;
                    }

                    var mappedPoint = _textView.BufferGraph.MapUpToSnapshot(
                        point.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, _textView.VisualSnapshot);
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

                    // add the visual to the adornment layer.
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpan.Tag;
                        var graphicsResult = tag.GetGraphics(_textView, geometry);
                        _adornmentLayer.AddAdornment(
                            behavior: AdornmentPositioningBehavior.TextRelative,
                            visualSpan: span,
                            tag: tag,
                            adornment: graphicsResult.VisualElement,
                            removedCallback: delegate { graphicsResult.Dispose(); });
                    }
                }
            }*/
        }
    }
}
