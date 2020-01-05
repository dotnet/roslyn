// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.OrderModifiers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private readonly Document _document;
            private readonly SyntaxNode _node;

            public override string Title => CSharpFeaturesResources.Hide_base_member;

            public AddNewKeywordAction(Document document, SyntaxNode node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newNode = await GetNewNodeAsync(_node, cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }

            private async Task<SyntaxNode> GetNewNodeAsync(SyntaxNode node, CancellationToken cancellationToken)
            {
                var syntaxFacts = CSharpSyntaxFactsService.Instance;
                var modifiers = syntaxFacts.GetModifiers(node);
                var newModifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));

                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var option = options.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
                if (!CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder) ||
                    !AbstractOrderModifiersHelpers.IsOrdered(preferredOrder, modifiers))
                {
                    return syntaxFacts.WithModifiers(node, newModifiers);
                }

                var orderedModifiers = new SyntaxTokenList(
                    newModifiers.OrderBy(CompareModifiers));

                return syntaxFacts.WithModifiers(node, orderedModifiers);

                int CompareModifiers(SyntaxToken left, SyntaxToken right)
                    => GetOrder(left) - GetOrder(right);

                int GetOrder(SyntaxToken token)
                    => preferredOrder.TryGetValue(token.RawKind, out var value) ? value : int.MaxValue;
            }
        }
    }
}
