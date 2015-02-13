// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class PreviewUpdater : ForegroundThreadAffinitizedObject
    {
        private PreviewDialogWorkspace _previewWorkspace;
        public static ITextView TextView;
        private DocumentId _currentDocument;
        private bool _isCurrentAdditionalDocument;
        internal static Span SpanToShow;
        internal static PreviewTagger Tagger;

        public PreviewUpdater(ITextView textView)
        {
            PreviewUpdater.TextView = textView;
            Tagger = new PreviewTagger(textView, textView.TextBuffer);
        }

        public void CloseWorkspace()
        {
            if (_previewWorkspace != null)
            {
                _previewWorkspace.Dispose();
            }
        }

        public void UpdateView(TextDocument document, SpanChange spanSource, bool isAdditionalDocument)
        {
            var documentText = document.GetTextAsync().Result.ToString();
            if (TextView.TextBuffer.CurrentSnapshot.GetText() != documentText)
            {
                SourceTextContainer container;
                TextDocument documentBackedByTextBuffer;
                UpdateBuffer(document, spanSource, isAdditionalDocument, out container, out documentBackedByTextBuffer);
            }

            // Picking a different span: no text change; update span anyway.
            SpanToShow = spanSource.GetSpan();
            var spanInBuffer = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, new Span(SpanToShow.Start, 0));
            TextView.ViewScroller.EnsureSpanVisible(spanInBuffer, EnsureSpanVisibleOptions.None);
            Tagger.OnTextBufferChanged();
        }

        private void UpdateBuffer(TextDocument document, SpanChange spanSource, bool isAdditionalDocument, out SourceTextContainer container, out TextDocument documentBackedByTextBuffer)
        {
            if (_previewWorkspace != null)
            {
                // Close the current document in preview solution.
                if (_isCurrentAdditionalDocument)
                {
                    _previewWorkspace.CloseAdditionalDocument(_currentDocument, _previewWorkspace.CurrentSolution.GetAdditionalDocument(_currentDocument).GetTextAsync().Result);
                }
                else
                {
                    _previewWorkspace.CloseDocument(_currentDocument, _previewWorkspace.CurrentSolution.GetDocument(_currentDocument).GetTextAsync().Result);
                }

                // Put the new document into the current preview solution.
                TextDocument updatedDocument;
                if (isAdditionalDocument)
                {
                    var updatedSolution = _previewWorkspace.CurrentSolution.WithAdditionalDocumentText(document.Id, document.GetTextAsync().Result);
                    updatedDocument = updatedSolution.GetAdditionalDocument(document.Id);
                }
                else
                {
                    var updatedSolution = _previewWorkspace.CurrentSolution.WithDocumentText(document.Id, document.GetTextAsync().Result);
                    updatedDocument = updatedSolution.GetDocument(document.Id);
                }

                ApplyDocumentToBuffer(updatedDocument, spanSource, isAdditionalDocument, out container, out documentBackedByTextBuffer);

                _previewWorkspace.TryApplyChanges(documentBackedByTextBuffer.Project.Solution);
                OpenDocument(document.Id, isAdditionalDocument);
                _currentDocument = document.Id;
            }
            else
            {
                _currentDocument = document.Id;

                ApplyDocumentToBuffer(document, spanSource, isAdditionalDocument, out container, out documentBackedByTextBuffer);

                _previewWorkspace = new PreviewDialogWorkspace(documentBackedByTextBuffer.Project.Solution);
                OpenDocument(document.Id, isAdditionalDocument);
            }

            _isCurrentAdditionalDocument = isAdditionalDocument;
        }

        private void OpenDocument(DocumentId documentId, bool isAdditionalDocument)
        {
            if (isAdditionalDocument)
            {
                _previewWorkspace.OpenAdditionalDocument(documentId);
            }
            else
            {
                _previewWorkspace.OpenDocument(documentId);
            }
        }

        private void ApplyDocumentToBuffer(TextDocument document, SpanChange spanSource, bool isAdditionalDocument, out SourceTextContainer container, out TextDocument documentBackedByTextBuffer)
        {
            var contentTypeService = document.Project.LanguageServices.GetService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            TextView.TextBuffer.ChangeContentType(contentType, null);

            var documentText = document.GetTextAsync().Result.ToString();
            SpanToShow = spanSource.GetSpan();

            using (var edit = TextView.TextBuffer.CreateEdit())
            {
                edit.Replace(new Span(0, TextView.TextBuffer.CurrentSnapshot.Length), documentText);
                edit.Apply();
            }

            container = TextView.TextBuffer.AsTextContainer();
            documentBackedByTextBuffer = isAdditionalDocument ?
                document.WithAdditionalDocumentText(container.CurrentText) :
                document.WithText(container.CurrentText);
        }
    }
}
