// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
        internal static Span SpanToShow;
        internal static PreviewTagger Tagger;

        public PreviewUpdater(IThreadingContext threadingContext, ITextView textView)
            : base(threadingContext)
        {
            PreviewUpdater.TextView = textView;
            Tagger = new PreviewTagger(textView.TextBuffer);
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
            var documentText = document.GetTextAsync().Result.ToString();
            if (TextView.TextBuffer.CurrentSnapshot.GetText() != documentText)
            {
                UpdateBuffer(document, spanSource);
            }

            // Picking a different span: no text change; update span anyway.
            SpanToShow = spanSource.GetSpan();
            var spanInBuffer = new SnapshotSpan(TextView.TextBuffer.CurrentSnapshot, new Span(SpanToShow.Start, 0));
            TextView.ViewScroller.EnsureSpanVisible(spanInBuffer, EnsureSpanVisibleOptions.None);
            Tagger.OnTextBufferChanged();
        }

        private void UpdateBuffer(TextDocument document, SpanChange spanSource)
        {
            if (_currentDocument != null)
            {
                var currentDocument = _previewWorkspace.CurrentSolution.GetTextDocument(_currentDocument);
                var currentDocumentText = currentDocument.GetTextSynchronously(CancellationToken.None);
                _previewWorkspace.CloseDocument(currentDocument, currentDocumentText);
            }

            if (_previewWorkspace == null)
            {
                _previewWorkspace = new PreviewDialogWorkspace(document.Project.Solution);
            }

            _currentDocument = document.Id;
            ApplyDocumentToBuffer(document, spanSource, out var container);
            _previewWorkspace.OpenDocument(document.Id, container);
        }

        private void ApplyDocumentToBuffer(TextDocument document, SpanChange spanSource, out SourceTextContainer container)
        {
            var contentTypeService = document.Project.LanguageServices.GetRequiredService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            TextView.TextBuffer.ChangeContentType(contentType, null);

            var documentText = document.GetTextAsync().Result.ToString();
            SpanToShow = spanSource.GetSpan();

            using (var edit = TextView.TextBuffer.CreateEdit())
            {
                edit.Replace(new Span(0, TextView.TextBuffer.CurrentSnapshot.Length), documentText);
                edit.ApplyAndLogExceptions();
            }

            container = TextView.TextBuffer.AsTextContainer();
        }
    }
}
