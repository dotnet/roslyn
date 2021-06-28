// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.LineSeparators
{
    internal class LineSeparatorAdornmentManager : AdornmentManager<LineSeparatorTag>
    {
        public LineSeparatorAdornmentManager(IThreadingContext threadingContext, IWpfTextView textView,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService, IAsynchronousOperationListener asyncListener, string adornmentLayerName)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
        }
     
        protected override void AddAdornmentsToAdornmentLayer(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            var viewSnapshot = TextView.TextSnapshot;
            var viewLines = TextView.TextViewLines;

            foreach (var changedSpan in changedSpanCollection)
            {
                // is there any effect on the view?
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = TagAggregator.GetTags(changedSpan);
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

                    // add the visual to the adornment layer.
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpan.Tag;
                        var graphicsResult = tag.GetGraphics(TextView, geometry);
                        AdornmentLayer.AddAdornment(
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
