using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class RemoveNewKeywordAction : CodeActions.CodeAction
        {
            private readonly Document _document;
            private readonly MemberDeclarationSyntax _node;

            //TODO: use resources
            public override string Title => "Remove 'new' keyword";

            public RemoveNewKeywordAction(Document document, MemberDeclarationSyntax node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxFacts = CSharpSyntaxFacts.Instance;
                var modifiers = syntaxFacts.GetModifiers(_node);

                var newModifiers = modifiers;
                int i;
                SyntaxToken modifier = default;
                for (i = 0; i < modifiers.Count; i++)
                {
                    modifier = modifiers[i];
                    if (modifier.Kind() == SyntaxKind.NewKeyword)
                    {
                        break;
                    }
                }
                Debug.Assert(modifier != default, $"'new' keyword was not found, but diagnostic was triggered");

                newModifiers = modifiers.RemoveAt(i);

                var trivia = modifier.TrailingTrivia;
                if (trivia.Any())
                {
                    var previousModifier = newModifiers[i - 1];

                    if (trivia[0].IsWhitespace() && previousModifier.TrailingTrivia.Last().IsWhitespace())
                    {
                        trivia = trivia.RemoveAt(0);
                    }

                    var previousModifierWithMovedTrivia = previousModifier.WithAppendedTrailingTrivia(trivia);
                    newModifiers = newModifiers.Replace(previousModifier, previousModifierWithMovedTrivia);
                }

                var newNode = syntaxFacts.WithModifiers(_node, newModifiers);
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }
        }
    }
}
