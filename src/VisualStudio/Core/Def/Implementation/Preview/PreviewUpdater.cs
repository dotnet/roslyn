// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class PreviewUpdater : ForegroundThreadAffinitizedObject
    {
        private PreviewDialogWorkspace _previewWorkspace;
        public static ITextView TextView;
        private DocumentId _currentDocument;
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

        public void UpdateView(Document document, SpanChange spanSource)
        {
            var documentText = document.GetTextAsync().Result.ToString();
            if (TextView.TextBuffer.CurrentSnapshot.GetText() != documentText)
            {
                SourceTextContainer container;
                Document documentBackedByTextBuffer;
                UpdateBuffer(document, spanSource, out container, out documentBackedByTextBuffer);
            }

            // Picking a different span: no text change; update span anyway.
            SpanToShow = spanSource.GetSpan();
            var spanInBuffer = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, new Span(SpanToShow.Start, 0));
            TextView.ViewScroller.EnsureSpanVisible(spanInBuffer, EnsureSpanVisibleOptions.None);
            Tagger.OnTextBufferChanged();
        }

        private void UpdateBuffer(Document document, SpanChange spanSource, out SourceTextContainer container, out Document documentBackedByTextBuffer)
        {
            if (_previewWorkspace != null)
            {
                _previewWorkspace.CloseDocument(_currentDocument, _previewWorkspace.CurrentSolution.GetDocument(_currentDocument).GetTextAsync().Result);

                // Put the new document into the current preview solution
                var updatedSolution = _previewWorkspace.CurrentSolution.WithDocumentText(document.Id, document.GetTextAsync().Result);
                var updatedDocument = updatedSolution.GetDocument(document.Id);

                ApplyDocumentToBuffer(updatedDocument, spanSource, out container, out documentBackedByTextBuffer);

                _previewWorkspace.TryApplyChanges(documentBackedByTextBuffer.Project.Solution);
                _previewWorkspace.OpenDocument(document.Id);
                _currentDocument = document.Id;
            }
            else
            {
                _currentDocument = document.Id;

                ApplyDocumentToBuffer(document, spanSource, out container, out documentBackedByTextBuffer);

                _previewWorkspace = new PreviewDialogWorkspace(documentBackedByTextBuffer.Project.Solution);
                _previewWorkspace.OpenDocument(document.Id);
            }
        }

        private void ApplyDocumentToBuffer(Document document, SpanChange spanSource, out SourceTextContainer container, out Document documentBackedByTextBuffer)
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
            documentBackedByTextBuffer = document.WithText(container.CurrentText);
        }
    }
}
