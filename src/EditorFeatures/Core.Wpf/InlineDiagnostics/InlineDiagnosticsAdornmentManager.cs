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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineDiagnostics
{
    internal class InlineDiagnosticsAdornmentManager : AbstractAdornmentManager<InlineDiagnosticsTag>
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

        /// <summary>
        /// Need to remove the tags if they intersect with the editor view, but only if the option
        /// to place the tags at the end of the editor is selected.
        /// </summary>
        private void TextView_ViewportWidthChanged(object sender, EventArgs e)
        {
            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

            if (AdornmentLayer is null)
            {
                return;
            }

            var sourceContainer = TextView.TextBuffer.AsTextContainer();
            if (sourceContainer is null)
            {
                AdornmentLayer.RemoveAllAdornments();
                return;
            }

            if (!Workspace.TryGetWorkspace(sourceContainer, out var workspace))
            {
                AdornmentLayer.RemoveAllAdornments();
                return;
            }

            var document = sourceContainer.GetOpenDocumentInCurrentContext();
            if (document is null)
            {
                AdornmentLayer.RemoveAllAdornments();
                return;
            }

            var option = workspace.Options.GetOption(InlineDiagnosticsOptions.Location, document.Project.Language);
            if (option == InlineDiagnosticsLocations.PlacedAtEndOfEditor)
            {
                var normalizedCollectionSpan = new NormalizedSnapshotSpanCollection(TextView.TextViewLines.FormattedSpan);
                UpdateSpans_CallOnlyOnUIThread(normalizedCollectionSpan, removeOldTags: true);
            }
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

            if (AdornmentLayer is not null)
            {
                foreach (var element in AdornmentLayer.Elements)
                {
                    var tag = (InlineDiagnosticsTag)element.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType(InlineDiagnosticsTag.GetClassificationId(tag.ErrorType));
                    var format = GetFormat(classificationType);
                    InlineDiagnosticsTag.UpdateColor(format, element.Adornment);
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
        private IDictionary<int, IMappingTagSpan<InlineDiagnosticsTag>> GetSpansOnEachLine(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            var map = new Dictionary<int, IMappingTagSpan<InlineDiagnosticsTag>>();
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
                    if (!ShouldDrawTag(changedSpan, tagMappingSpan))
                    {
                        continue;
                    }

                    var mappedPoint = GetMappedPoint(changedSpan, tagMappingSpan);

                    // mappedPoint is known to not be null here because it is checked in the ShouldDrawTag method call.
                    var lineNum = mappedPoint!.Value.GetContainingLine().LineNumber;

                    // If the line does not have an associated tagMappingSpan and changedSpan, then add the first one.
                    if (!map.TryGetValue(lineNum, out var value))
                    {
                        map.Add(lineNum, tagMappingSpan);
                    }

                    // If the map has a value and the value is not a syntax error, then rewrite the value in the map since
                    // each line with errors will be ordered by importance
                    if (value is not null && value.Tag.ErrorType is not PredefinedErrorTypeNames.SyntaxError)
                    {
                        map[lineNum] = tagMappingSpan;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Iterates through the mapping of line number to span and draws the diagnostic in the appropriate position on the screen,
        /// as well as adding the tag to the adornment layer.
        /// </summary>
        protected override void AddAdornmentsToAdornmentLayer_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());
            if (changedSpanCollection.IsEmpty())
            {
                return;
            }

            var viewLines = TextView.TextViewLines;
            var map = GetSpansOnEachLine(changedSpanCollection);
            foreach (var (lineNum, tagMappingSpan) in map)
            {
                // Mapping the IMappingTagSpan back up to the TextView's visual snapshot to ensure there will
                // be no adornments drawn on disjoint spans.
                if (!TryMapToSingleSnapshotSpan(tagMappingSpan.Span, TextView.TextSnapshot, out var span))
                {
                    continue;
                }

                var geometry = viewLines.GetMarkerGeometry(span);
                if (geometry != null)
                {
                    var tag = tagMappingSpan.Tag;
                    var classificationType = _classificationRegistryService.GetClassificationType(InlineDiagnosticsTag.GetClassificationId(tag.ErrorType));
                    var graphicsResult = tag.GetGraphics(TextView, geometry, GetFormat(classificationType));

                    // Need to get the SnapshotPoint to be able to get the IWpfTextViewLine
                    var point = tagMappingSpan.Span.Start.GetPoint(TextView.TextSnapshot, PositionAffinity.Predecessor);
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
                        Canvas.SetLeft(visualElement, TextView.ViewportRight - visualElement.DesiredSize.Width);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    Canvas.SetTop(visualElement, geometry.Bounds.Bottom - visualElement.DesiredSize.Height);

                    // Only place the diagnostics if the diagnostic would not intersect with the editor window
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
}
