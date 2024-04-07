// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.OrderModifiers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;

using static CSharpSyntaxTokens;

internal partial class HideBaseCodeFixProvider
{
    private class AddNewKeywordAction(Document document, SyntaxNode node, CodeActionOptionsProvider fallbackOptions) : CodeAction
    {
        private readonly Document _document = document;
        private readonly SyntaxNode _node = node;
        private readonly CodeActionOptionsProvider _fallbackOptions = fallbackOptions;

        public override string Title => CSharpCodeFixesResources.Hide_base_member;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var options = await _document.GetCSharpCodeFixOptionsProviderAsync(_fallbackOptions, cancellationToken).ConfigureAwait(false);

            var newNode = GetNewNode(_node, options.PreferredModifierOrder.Value);
            var newRoot = root.ReplaceNode(_node, newNode);

            return _document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode GetNewNode(SyntaxNode node, string preferredModifierOrder)
        {
            var syntaxFacts = CSharpSyntaxFacts.Instance;
            var modifiers = syntaxFacts.GetModifiers(node);
            var newModifiers = modifiers.Add(NewKeyword);

            if (!CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(preferredModifierOrder, out var preferredOrder) ||
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
