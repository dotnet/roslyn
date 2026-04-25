// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class DirectiveVerifier
{
    private static readonly Action<CompletionItem>[] s_defaultDirectiveCollectionVerifiers;

    public static Action<CompletionItem>[] DefaultDirectiveCollectionVerifiers => s_defaultDirectiveCollectionVerifiers;

    static DirectiveVerifier()
    {
        var defaultDirectiveVerifierList = new List<Action<CompletionItem>>(DirectiveCompletionItemProvider.MvcDefaultDirectives.Length * 2);

        foreach (var directive in DirectiveCompletionItemProvider.MvcDefaultDirectives)
        {
            defaultDirectiveVerifierList.Add(item => Assert.Equal(directive.Directive, item.InsertText));
            defaultDirectiveVerifierList.Add(item => AssertDirectiveSnippet(item, directive.Directive));
        }

        s_defaultDirectiveCollectionVerifiers = defaultDirectiveVerifierList.ToArray();
    }

    private static void AssertDirectiveSnippet(CompletionItem completionItem, string directive)
    {
        Assert.StartsWith(directive, completionItem.InsertText);
        Assert.Equal(DirectiveCompletionItemProvider.SingleLineDirectiveSnippets[directive].InsertText, completionItem.InsertText);
        Assert.Equal(CompletionItemKind.Snippet, completionItem.Kind);
    }
}
