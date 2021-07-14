// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class PreviewUpdater : ForegroundThreadAffinitizedObject
    {
        private PreviewDialogWorkspace? _previewWorkspace;
        private readonly ITextView _textView;
        private DocumentId? _currentDocumentId;
        private readonly PreviewTagger _tagger;

        public PreviewUpdater(IThreadingContext threadingContext, ITextView textView)
            : base(threadingContext)
        {
            _textView = textView;
            _tagger = new PreviewTagger(textView.TextBuffer);
            _textView.Properties[typeof(PreviewTagger)] = _tagger;
        }

        public void CloseWorkspace()
        {
            if (_previewWorkspace != null)
            {
                _previewWorkspace.Dispose();
            }
        }

        public void UpdateView(TextDocument document, SpanChange spanSource)
        {
            UpdateBuffer(document);

            // Picking a different span: no text change; update span anyway.
            _tagger.Span = spanSource.GetSpan();
            var spanInBuffer = new SnapshotSpan(_textView.TextBuffer.CurrentSnapshot, new Span(_tagger.Span.Start, 0));
            _textView.ViewScroller.EnsureSpanVisible(spanInBuffer, EnsureSpanVisibleOptions.None);

        }

        private void UpdateBuffer(TextDocument document)
        {
            // Note, even if the passed TextDocument has the same ID as _currentDocumentId, we still need
            // to refresh the buffer; if the user has a set of edits for a single document from a fix all operation,
            // you can check and uncheck the individual edits and we show the result. In that case, we're getting new
            // snapshots each time of the same document.

            if (document.Id == _currentDocumentId)
            {
                Contract.ThrowIfNull(_previewWorkspace, "We shouldn't have a current document if we don't have a workspace.");
                var existingDocument = _previewWorkspace.CurrentSolution.GetRequiredTextDocument(_currentDocumentId);
                if (existingDocument.GetTextSynchronously(CancellationToken.None).ContentEquals(document.GetTextSynchronously(CancellationToken.None)))
                {
                    // The contents of the buffer matches what we'd update it to, so no reason to change.
                    return;
                }
            }

            if (_currentDocumentId != null)
            {
                Contract.ThrowIfNull(_previewWorkspace, "We shouldn't have a current document if we don't have a workspace.");
                var currentDocument = _previewWorkspace.CurrentSolution.GetRequiredTextDocument(_currentDocumentId);
                var currentDocumentText = currentDocument.GetTextSynchronously(CancellationToken.None);
                _previewWorkspace.CloseDocument(currentDocument, currentDocumentText);
            }

            if (_previewWorkspace == null)
            {
                _previewWorkspace = new PreviewDialogWorkspace(document.Project.Solution);
            }

            _currentDocumentId = document.Id;
            ApplyDocumentToBuffer(document, out var container);
            _previewWorkspace.OpenDocument(document.Id, container);
        }

        private void ApplyDocumentToBuffer(TextDocument document, out SourceTextContainer container)
        {
            var contentTypeService = document.Project.LanguageServices.GetRequiredService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            _textView.TextBuffer.ChangeContentType(contentType, null);

            var documentText = document.GetTextSynchronously(CancellationToken.None).ToString();

            using (var edit = _textView.TextBuffer.CreateEdit())
            {
                edit.Replace(new Span(0, _textView.TextBuffer.CurrentSnapshot.Length), documentText);
                edit.ApplyAndLogExceptions();
            }

            container = _textView.TextBuffer.AsTextContainer();
        }
    }
}
