using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    public sealed class DocumentEditorFixAllProvider : FixAllProvider
    {
        public static readonly DocumentEditorFixAllProvider CurrentDocument = new DocumentEditorFixAllProvider(ImmutableArray.Create(FixAllScope.Document));

        private readonly ImmutableArray<FixAllScope> supportedFixAllScopes;

        private DocumentEditorFixAllProvider(ImmutableArray<FixAllScope> supportedFixAllScopes)
        {
            this.supportedFixAllScopes = supportedFixAllScopes;
        }

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => this.supportedFixAllScopes;

        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document)
                                                 .ConfigureAwait(false);
            var actions = new List<DocumentEditorAction>();
            foreach (var diagnostic in diagnostics)
            {
                var codeFixContext = new CodeFixContext(
                    fixAllContext.Document,
                    diagnostic,
                    (a, _) =>
                    {
                        if (a.EquivalenceKey == fixAllContext.CodeActionEquivalenceKey)
                        {
                            actions.Add((DocumentEditorAction)a);
                        }
                    },
                    fixAllContext.CancellationToken);
                await fixAllContext.CodeFixProvider.RegisterCodeFixesAsync(codeFixContext)
                                   .ConfigureAwait(false);
            }

            if (actions.Count == 0)
            {
                return null;
            }

            return CodeAction.Create(actions[0].Title, c => FixDocumentAsync(fixAllContext.Document, actions, c));
        }

        private static async Task<Document> FixDocumentAsync(Document document, IReadOnlyList<DocumentEditorAction> actions, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken)
                                             .ConfigureAwait(false);
            foreach (var action in actions)
            {
                action.Action(editor, cancellationToken);
            }

            return editor.GetChangedDocument();
        }
    }
}
