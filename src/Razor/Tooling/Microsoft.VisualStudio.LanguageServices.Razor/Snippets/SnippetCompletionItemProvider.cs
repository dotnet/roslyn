// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.Snippets;

[Export(typeof(ISnippetCompletionItemProvider))]
internal sealed class SnippetCompletionItemProvider : ISnippetCompletionItemProvider
{
    [ImportingConstructor]
    public SnippetCompletionItemProvider(SnippetCache snippetCache)
    {
        SnippetCache = snippetCache;
    }

    public SnippetCache SnippetCache { get; }

    public void AddSnippetCompletions(
        ref PooledArrayBuilder<VSInternalCompletionItem> builder,
        RazorLanguageKind projectedKind,
        VSInternalCompletionInvokeKind invokeKind,
        string? triggerCharacter)
    {
        // Temporary fix: snippets are broken in CSharp. We're investigating
        // but this is very disruptive. This quick fix unblocks things.
        // TODO: Add an option to enable this.
        if (projectedKind != RazorLanguageKind.Html)
        {
            return;
        }

        // Don't add snippets for deletion of a character
        if (invokeKind == VSInternalCompletionInvokeKind.Deletion)
        {
            return;
        }

        // Don't add snippets if the trigger characters contain whitespace
        if (triggerCharacter is not null && triggerCharacter.Contains(' '))
        {
            return;
        }

        var snippets = SnippetCache.GetSnippets(ConvertLanguageKind(projectedKind));
        if (snippets.IsDefaultOrEmpty)
        {
            return;
        }

        builder.AddRange(snippets
            .Select(s => new VSInternalCompletionItem()
            {
                Label = s.Shortcut,
                Detail = s.Description,
                InsertTextFormat = InsertTextFormat.Snippet,
                InsertText = s.Shortcut,
                Data = s.CompletionData,
                Kind = CompletionItemKind.Snippet,
                CommitCharacters = []
            }));
    }

    public bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString)
    {
        if (SnippetCompletionData.TryParse(completionItem.Data, out var snippetCompletionData) &&
            SnippetCache.TryResolveSnippetString(snippetCompletionData) is { } snippetInsertText)
        {
            insertString = snippetInsertText;
            return true;
        }

        insertString = null;
        return false;
    }

    private static SnippetLanguage ConvertLanguageKind(RazorLanguageKind languageKind)
        => languageKind switch
        {
            RazorLanguageKind.CSharp => SnippetLanguage.CSharp,
            RazorLanguageKind.Html => SnippetLanguage.Html,
            RazorLanguageKind.Razor => SnippetLanguage.Razor,
            _ => throw new InvalidOperationException($"Unexpected value {languageKind}")
        };
}
