// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

/// <summary>
/// Modifies delegated snippet completion items
/// </summary>
/// <remarks>
/// At the moment primarily used to remove the C# "using" snippet because we have our own
/// </remarks>
internal class SnippetResponseRewriter : IDelegatedCSharpCompletionResponseRewriter
{
    public RazorVSInternalCompletionList Rewrite(
        RazorVSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition,
        RazorCompletionOptions completionOptionsn)
    {
        using var items = new PooledArrayBuilder<VSInternalCompletionItem>(completionList.Items.Length);

        foreach (var item in completionList.Items)
        {
            if (item is { Kind: CompletionItemKind.Snippet, Label: "using" })
            {
                continue;
            }

            items.Add(item);
        }

        // If we didn't remove anything, then don't bother materializing the array
        if (completionList.Items.Length != items.Count)
        {
            completionList.Items = items.ToArray();
        }

        return completionList;
    }
}
