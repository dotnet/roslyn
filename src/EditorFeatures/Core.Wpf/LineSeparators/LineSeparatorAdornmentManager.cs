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
    internal class LineSeparatorAdornmentManager : AbstractAdornmentManager<LineSeparatorTag>
    {
        public LineSeparatorAdornmentManager(IThreadingContext threadingContext, IWpfTextView textView,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService, IAsynchronousOperationListener asyncListener, string adornmentLayerName)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
        }

        protected override void AddAdornmentsToAdornmentLayer_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

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
                    if (!TryGetMappedPoint(changedSpan, tagMappingSpan, out var mappedPoint))
                        continue;

                    if (!TryGetViewLine(mappedPoint, out _))
                        continue;

                    if (!ShouldDrawTag(tagMappingSpan))
                        continue;

                    if (!TryMapToSingleSnapshotSpan(tagMappingSpan.Span, TextView.TextSnapshot, out var span))
                        continue;

                    // add the visual to the adornment layer.
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpan.Tag;
                        var graphicsResult = tag.GetGraphics(TextView, geometry, format: null);
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

        protected override void RemoveAdornmentFromAdornmentLayer_CallOnlyOnUIThread(SnapshotSpan span)
        {
            AdornmentLayer.RemoveAdornmentsByVisualSpan(span);
        }
    }
}
