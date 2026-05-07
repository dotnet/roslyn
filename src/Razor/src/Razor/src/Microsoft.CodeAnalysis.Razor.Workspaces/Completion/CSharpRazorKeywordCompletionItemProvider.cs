// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class CSharpRazorKeywordCompletionItemProvider : IRazorCompletionItemProvider
{
    internal static readonly ImmutableArray<RazorCommitCharacter> KeywordCommitCharacters = RazorCommitCharacter.CreateArray([" "]);

    // internal for testing
    internal static readonly ImmutableArray<string> CSharpRazorKeywords =
    [
        "do", "for", "foreach", "if", "lock", "switch", "try", "while"
    ];

    // Internal for testing
    internal static readonly ImmutableArray<RazorCompletionItem> CSharpRazorKeywordCompletionItems = GetCSharpRazorKeywordCompletionItems();

    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        return ShouldProvideCompletions(context)
            ? CSharpRazorKeywordCompletionItems
            : [];
    }

    // Internal for testing
    internal static bool ShouldProvideCompletions(RazorCompletionContext context)
    {
        var owner = context.Owner;
        if (owner is null)
        {
            return false;
        }

        // Do not provide IntelliSense for explicit expressions. Explicit expressions will usually look like:
        // @(DateTime.Now)
        var implicitExpression = owner.FirstAncestorOrSelf<CSharpImplicitExpressionSyntax>();
        if (implicitExpression is null)
        {
            return false;
        }

        if (implicitExpression.Width > 2 && context.Reason != CompletionReason.Invoked)
        {
            // We only want to provide razor csharp keyword completions if the implicit expression is empty "@|" or at the beginning of a word "@i|", this ensures
            // we're consistent with how C# typically provides completion items.
            return false;
        }

        if (owner.ChildNodesAndTokens().Any(static x => !x.AsToken(out var token) || !IsCSharpRazorKeywordCompletableToken(token)))
        {
            // Implicit expression contains nodes or tokens that aren't completable by a csharp razor keyword
            return false;
        }

        return true;
    }

    private static bool IsCSharpRazorKeywordCompletableToken(AspNetCore.Razor.Language.Syntax.SyntaxToken token)
    {
        return token is { Kind: SyntaxKind.Identifier or SyntaxKind.Marker or SyntaxKind.Keyword }
                     or { Kind: SyntaxKind.Transition, Parent.Kind: SyntaxKind.CSharpTransition };
    }

    private static ImmutableArray<RazorCompletionItem> GetCSharpRazorKeywordCompletionItems()
    {
        var completionItems = new RazorCompletionItem[CSharpRazorKeywords.Length];

        for (var i = 0; i < CSharpRazorKeywords.Length; i++)
        {
            var keyword = CSharpRazorKeywords[i];

            var keywordCompletionItem = RazorCompletionItem.CreateKeyword(
                displayText: keyword,
                insertText: keyword,
                KeywordCommitCharacters);

            completionItems[i] = keywordCompletionItem;
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(completionItems);
    }
}
