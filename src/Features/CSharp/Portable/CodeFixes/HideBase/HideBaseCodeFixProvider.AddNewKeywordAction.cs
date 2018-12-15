// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private readonly Document _document;
            private readonly SyntaxNode _node;

            private static CSharpSyntaxFactsService SyntaxFacts => CSharpSyntaxFactsService.Instance;

            private static Options.Option<CodeAnalysis.CodeStyle.CodeStyleOption<string>> PreferredModifierOrderOption => CodeStyle.CSharpCodeStyleOptions.PreferredModifierOrder;

            private static OrderModifiers.CSharpOrderModifiersHelper OrderModifiersHelper => OrderModifiers.CSharpOrderModifiersHelper.Instance;

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
                var generator = SyntaxGenerator.GetGenerator(_document);
                var newNode = generator.WithModifiers(node, generator.GetModifiers(node).WithIsNew(true));

                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var option = options.GetOption(PreferredModifierOrderOption);
                if (!OrderModifiersHelper.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
                {
                    return newNode;
                }

                var modifiers = SyntaxFacts.GetModifiers(newNode);
                var orderedModifiers = new SyntaxTokenList(
                    modifiers.OrderBy(CompareModifiers)
                             .Select((t, i) => t.WithTriviaFrom(modifiers[i])));

                return SyntaxFacts.WithModifiers(newNode, orderedModifiers);

                int CompareModifiers(SyntaxToken t1, SyntaxToken t2)
                    => GetOrder(t1) - GetOrder(t2);

                int GetOrder(SyntaxToken token)
                    => preferredOrder.TryGetValue(token.RawKind, out var value) ? value : int.MaxValue;
            }
        }
    }
}
