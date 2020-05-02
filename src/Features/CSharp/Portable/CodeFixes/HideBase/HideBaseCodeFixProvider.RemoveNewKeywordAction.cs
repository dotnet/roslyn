using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class RemoveNewKeywordAction : CodeActions.CodeAction
        {
            private readonly Document _document;
            private readonly SyntaxNode _node;

            //TODO: use resources
            public override string Title => "Remove 'new' keyword";

            public RemoveNewKeywordAction(Document document, SyntaxNode node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxFacts = CSharpSyntaxFacts.Instance;

                var newModifier = GetNewModifier(_node);
                Debug.Assert(newModifier != default, $"'new' keyword was not found, but diagnostic was triggered");

                var newNode = _node;

                if (newModifier.HasTrailingTrivia || newModifier.HasLeadingTrivia)
                {
                    var newModifierTrivia = newModifier.GetAllTrivia().ToSyntaxTriviaList();
                    var previousToken = newModifier.GetPreviousToken();
                    var nextToken = newModifier.GetNextToken();

                    var sourceText = _document.GetTextSynchronously(cancellationToken);
                    var isFirstTokenOnLine = newModifier.IsFirstTokenOnLine(sourceText);

                    var newTrivia = new SyntaxTriviaList();
                    if (!isFirstTokenOnLine)
                    {
                        newTrivia = newTrivia.AddRange(previousToken.TrailingTrivia);
                    }
                    newTrivia = newTrivia
                        .AddRange(newModifierTrivia)
                        .AddRange(nextToken.LeadingTrivia)
                        .CollapseSequentialWhitespaces();

                    if (isFirstTokenOnLine)
                    {
                        var nextTokenWithMovedTrivia = nextToken.WithLeadingTrivia(newTrivia);
                        newNode = newNode.ReplaceToken(nextToken, nextTokenWithMovedTrivia);
                    }
                    else
                    {
                        var previousTokenWithMovedTrivia = previousToken.WithTrailingTrivia(newTrivia);
                        newNode = newNode.ReplaceToken(previousToken, previousTokenWithMovedTrivia);
                    }
                }

                newNode = newNode.ReplaceToken(GetNewModifier(newNode), SyntaxFactory.Token(SyntaxKind.None));

                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);

                SyntaxToken GetNewModifier(SyntaxNode node) =>
                    syntaxFacts.GetModifierTokens(node).FirstOrDefault(m => m.IsKind(SyntaxKind.NewKeyword));
            }
        }
    }
}
