// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
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
        private TextFormattingRunProperties? _format;
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
            if (_format != null)
            {
                var elements = _adornmentLayer.Elements;
                foreach (var element in elements)
                {
                    var tag = (InlineErrorTag)element.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType("IE: " + tag.ErrorType);
                    var format = GetFormat(classificationType);
                    tag.UpdateColor(format, element.Adornment);
                }
            }
        }

        private TextFormattingRunProperties GetFormat(IClassificationType classificationType)
        {
            _format = _formatMap.GetTextProperties(classificationType);
            return _format;
        }

        /// <summary>
        /// Get the spans located on each line so that I can only display the first one that appears on the line
        /// </summary>
        private Dictionary<int, List<IMappingTagSpan<InlineErrorTag>>> GetSpansOnEachLine(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            _tagSpanToPointMap.Clear();
            var map = new Dictionary<int, List<IMappingTagSpan<InlineErrorTag>>>();
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
                        list = new List<IMappingTagSpan<InlineErrorTag>>();
                        map.Add(lineNum, list);
                    }

                    list.Add(tagMappingSpan);
                    _tagSpanToPointMap.Add(tagMappingSpan, point.Value);
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

            var map = GetSpansOnEachLine(changedSpanCollection);
            foreach (var (lineNum, tagMappingSpanList) in map)
            {
                if (tagMappingSpanList.Count >= 1)
                {
                    TryMapToSingleSnapshotSpan(tagMappingSpanList[0].Span, _textView.TextSnapshot, out var span);
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpanList[0].Tag;
                        var classificationType = _classificationRegistryService.GetClassificationType("IE: " + tag.ErrorType);
                        var graphicsResult = tag.GetGraphics(_textView, geometry, GetFormat(classificationType));
                        if (!_tagSpanToPointMap.TryGetValue(tagMappingSpanList[0], out var point))
                        {
                            continue;
                        }

                        var lineView = _textView.GetTextViewLineContainingBufferPosition(point);

                        if (lineView.Right < _textView.ViewportWidth - graphicsResult.VisualElement.DesiredSize.Width)
                        {
                            _adornmentLayer.AddAdornment(
                                behavior: AdornmentPositioningBehavior.TextRelative,
                                visualSpan: span,
                                tag: tag,
                                adornment: graphicsResult.VisualElement,
                                removedCallback: delegate { graphicsResult.Dispose(); });
                        }
                    }
                }
            }
        }
    }
}
