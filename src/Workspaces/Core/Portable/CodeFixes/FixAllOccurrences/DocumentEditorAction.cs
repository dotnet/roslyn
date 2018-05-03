using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    public class DocumentEditorAction : CodeAction
    {
        private readonly Document document;

        public DocumentEditorAction(string title, Document document, Action<DocumentEditor, CancellationToken> action, string equivalenceKey)
        {
            this.document = document;
            this.Title = title;
            this.Action = action;
            this.EquivalenceKey = equivalenceKey;
        }

        public Action<DocumentEditor, CancellationToken> Action { get; }

        public sealed override string Title { get; }

        public sealed override string EquivalenceKey { get; }

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(this.document, cancellationToken)
                                             .ConfigureAwait(false);
            this.Action(editor, cancellationToken);
            return editor.GetChangedDocument();
        }
    }
}
