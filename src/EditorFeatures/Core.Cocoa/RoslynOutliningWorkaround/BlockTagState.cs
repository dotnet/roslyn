// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;
using CoreGraphics;
using AppKit;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    /// <summary>
    /// Shared state object we use for RoslynOutliningRegionTag and RoslynBlockTag
    /// (in EditorFeatures.Next).
    /// </summary>
    internal struct BlockTagState
    {
        private const string OutliningRegionTextViewRole = nameof(OutliningRegionTextViewRole);

        private const string Ellipsis = "...";
        private const int MaxPreviewText = 1000;

        private readonly ICocoaTextEditorFactoryService _textEditorFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;

        private readonly ITextBuffer _subjectBuffer;
        private readonly ITrackingSpan _hintSpan;

        public bool IsDefaultCollapsed => BlockSpan.IsDefaultCollapsed;
        public bool IsImplementation => BlockSpan.AutoCollapse;
        public object CollapsedForm => BlockSpan.BannerText;

        public readonly BlockSpan BlockSpan;

        public BlockTagState(
            ICocoaTextEditorFactoryService textEditorFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextSnapshot snapshot,
            BlockSpan blockSpan)
        {
            _textEditorFactoryService = textEditorFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _subjectBuffer = snapshot.TextBuffer;
            BlockSpan = blockSpan;

            _hintSpan = snapshot.CreateTrackingSpan(BlockSpan.HintSpan.ToSpan(), SpanTrackingMode.EdgeExclusive);
        }

        public override bool Equals(object obj)
            => obj is BlockTagState s && Equals(s);

        public bool Equals(BlockTagState tag)
            => IsImplementation == tag.IsImplementation &&
               Equals(this.CollapsedForm, tag.CollapsedForm);

        public override int GetHashCode()
            => Hash.Combine(IsImplementation,
                            EqualityComparer<object>.Default.GetHashCode(this.CollapsedForm));

        public object CollapsedHintForm
            => CreateElisionBuffer().CurrentSnapshot.GetText(); //new ViewHostingControl(CreateElisionBufferView, CreateElisionBuffer);

#pragma warning disable IDE0051 // Remove unused private members
        private ICocoaTextView CreateElisionBufferView(ITextBuffer finalBuffer)
#pragma warning restore IDE0051 // Remove unused private members
            => CreateShrunkenTextView(_textEditorFactoryService, finalBuffer);

        internal static ICocoaTextView CreateShrunkenTextView(
            ICocoaTextEditorFactoryService textEditorFactoryService,
            ITextBuffer finalBuffer)
        {
            var roles = textEditorFactoryService.CreateTextViewRoleSet(OutliningRegionTextViewRole);
            var view = textEditorFactoryService.CreateTextView(finalBuffer, roles);

            view.Background = NSColor.Clear.CGColor;

            const double HorizontalCorrection = 8.0;
            const double VerticalCorrection = 4.0;

            // Force the view to render, measuring its size in the process.
            view.DisplayTextLineContainingBufferPosition(
                new SnapshotPoint(view.TextSnapshot, 0),
                0,
                ViewRelativePosition.Top,
                double.MaxValue,
                double.MaxValue);

            view.VisualElement.SetFrameSize(new CGSize(view.MaxTextRightCoordinate + HorizontalCorrection, view.TextViewLines.LastVisibleLine.Bottom + VerticalCorrection));

            // Zoom out a bit to shrink the text.
            view.ZoomLevel *= 0.75;

            return view;
        }

        private ITextBuffer CreateElisionBuffer()
        {
            // Remove any starting whitespace.
            var span = TrimStartingNewlines(_hintSpan.GetSpan(_subjectBuffer.CurrentSnapshot));

            // Trim the length if it's too long.
            var shortSpan = span;
            if (span.Length > MaxPreviewText)
            {
                shortSpan = ComputeShortSpan(span);
            }

            // Create an elision buffer for that span, also trimming the
            // leading whitespace.
            var elisionBuffer = CreateElisionBufferWithoutIndentation(_subjectBuffer, shortSpan);
            var finalBuffer = elisionBuffer;

            // If we trimmed the length, then make a projection buffer that 
            // has the above elision buffer and follows it with "..."
            if (span.Length != shortSpan.Length)
            {
                finalBuffer = CreateTrimmedProjectionBuffer(elisionBuffer);
            }

            return finalBuffer;
        }

        private ITextBuffer CreateTrimmedProjectionBuffer(ITextBuffer elisionBuffer)
        {
            // The elision buffer is too long.  We've already trimmed it, but now we want to add
            // a "..." to it.  We do that by creating a projection of both the elision buffer and
            // a new text buffer wrapping the ellipsis.
            var elisionSpan = elisionBuffer.CurrentSnapshot.GetFullSpan();

            var sourceSpans = new List<object>()
                {
                    elisionSpan.Snapshot.CreateTrackingSpan(elisionSpan, SpanTrackingMode.EdgeExclusive),
                    Ellipsis
                };

            var projectionBuffer = _projectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: sourceSpans,
                options: ProjectionBufferOptions.None);

            return projectionBuffer;
        }

        private Span ComputeShortSpan(Span span)
        {
            var endIndex = span.Start + MaxPreviewText;
            var line = _subjectBuffer.CurrentSnapshot.GetLineFromPosition(endIndex);

            return Span.FromBounds(span.Start, line.EndIncludingLineBreak);
        }

        private Span TrimStartingNewlines(Span span)
        {
            while (span.Length > 1 && char.IsWhiteSpace(_subjectBuffer.CurrentSnapshot[span.Start]))
            {
                span = new Span(span.Start + 1, span.Length - 1);
            }

            return span;
        }

        private ITextBuffer CreateElisionBufferWithoutIndentation(
            ITextBuffer dataBuffer, Span shortHintSpan)
        {
            return _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _editorOptionsFactoryService.GlobalOptions,
                contentType: null,
                exposedSpans: new SnapshotSpan(dataBuffer.CurrentSnapshot, shortHintSpan));
        }
    }
}
