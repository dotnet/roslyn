// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    // Our implementation of an outlining region tag.  The collapsedHintForm
    // is dynamically created using an elision buffer over the actual text
    // we are collapsing.
    internal class RoslynOutliningRegionTag : IOutliningRegionTag
    {
        private const string OutliningRegionTextViewRole = nameof(OutliningRegionTextViewRole);

        private const string Ellipsis = "...";
        private const int MaxPreviewText = 1000;

        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;

        private readonly ITextBuffer _subjectBuffer;
        private readonly ITrackingSpan _hintSpan;

        public bool IsDefaultCollapsed => BlockSpan.IsDefaultCollapsed;
        public bool IsImplementation => BlockSpan.AutoCollapse;
        public object CollapsedForm => BlockSpan.BannerText;

        protected readonly BlockSpan BlockSpan;

        public RoslynOutliningRegionTag(
            ITextEditorFactoryService textEditorFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextSnapshot snapshot,
            BlockSpan outliningSpan)
        {
            _textEditorFactoryService = textEditorFactoryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _subjectBuffer = snapshot.TextBuffer;
            BlockSpan = outliningSpan;

            _hintSpan = snapshot.CreateTrackingSpan(BlockSpan.HintSpan.ToSpan(), SpanTrackingMode.EdgeExclusive);
        }

        public override bool Equals(object obj) 
            => Equals(obj as RoslynOutliningRegionTag);

        // This is only called if the spans for the tags were the same. In that case, we 
        // consider ourselves the same unless the CollapsedForm properties are different.
        public bool Equals(RoslynOutliningRegionTag tag)
            => tag != null && Equals(this.CollapsedForm, tag.CollapsedForm);

        public override int GetHashCode() 
            => EqualityComparer<object>.Default.GetHashCode(this.CollapsedForm);

        public object CollapsedHintForm =>
            new ViewHostingControl(CreateElisionBufferView, CreateElisionBuffer);

        private IWpfTextView CreateElisionBufferView(ITextBuffer finalBuffer)
        {
            return CreateShrunkenTextView(_textEditorFactoryService, finalBuffer);
        }

        internal static IWpfTextView CreateShrunkenTextView(
            ITextEditorFactoryService textEditorFactoryService,
            ITextBuffer finalBuffer)
        {
            var roles = textEditorFactoryService.CreateTextViewRoleSet(OutliningRegionTextViewRole);
            var view = textEditorFactoryService.CreateTextView(finalBuffer, roles);

            view.Background = Brushes.Transparent;

            view.SizeToFit();

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
            return _projectionBufferFactoryService.CreateElisionBufferWithoutIndentation(
                _editorOptionsFactoryService.GlobalOptions,
                contentType: null,
                exposedSpans: new SnapshotSpan(dataBuffer.CurrentSnapshot, shortHintSpan));
        }
    }
}