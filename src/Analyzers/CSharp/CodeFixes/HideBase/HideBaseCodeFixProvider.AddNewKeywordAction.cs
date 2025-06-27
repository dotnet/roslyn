// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrderModifiers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;

using static CSharpSyntaxTokens;

internal sealed partial class HideBaseCodeFixProvider
{
    private async Task<Document> GetChangedDocumentAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);

        var newNode = GetNewNode(node, configOptions.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder).Value);
        var newRoot = root.ReplaceNode(node, newNode);

        return document.WithSyntaxRoot(newRoot);
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
