// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.Snippets;

[Export(typeof(ISnippetCompletionItemProvider))]
internal sealed class SnippetCompletionItemProvider : ISnippetCompletionItemProvider
{
    /// <summary>
    /// Maps snippet shortcuts to the root HTML element they insert, for snippets
    /// where the shortcut name differs from the tag name. Snippets not in this map
    /// use their shortcut as the tag name (which covers the majority of cases,
    /// including <c>$shortcut$</c>-based snippets like "div", "br", "ul").
    /// </summary>
    private static readonly FrozenDictionary<string, string> s_snippetTagNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["charset"] = "meta",
        ["metaviewport"] = "meta",
        ["dd"] = "dl",
        ["html4f"] = "html",
        ["html4s"] = "html",
        ["html4t"] = "html",
        ["html5"] = "html",
        ["xhtml10f"] = "html",
        ["xhtml10s"] = "html",
        ["xhtml10t"] = "html",
        ["xhtml11"] = "html",
        ["xhtml5"] = "html",
    }.ToFrozenDictionary();

    /// <summary>
    /// Snippets that are not meaningful in Razor files (e.g., ASP.NET Web Forms
    /// constructs like <c>&lt;script runat="server"&gt;</c>).
    /// </summary>
    private static readonly FrozenSet<string> s_razorExcludedSnippets = FrozenSet.ToFrozenSet(
        ["scriptr", "scriptr2"], StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    public SnippetCompletionItemProvider(SnippetCache snippetCache)
    {
        SnippetCache = snippetCache;
    }

    public SnippetCache SnippetCache { get; }

    public void AddSnippetCompletions(
        ref PooledArrayBuilder<VSInternalCompletionItem> builder,
        RazorLanguageKind projectedKind,
        string? triggerCharacter,
        ICollection<string> validElementNames,
        RazorCompletionOptions options,
        bool isStartTagContext)
    {
        // Temporary fix: snippets are broken in CSharp. We're investigating
        // but this is very disruptive. This quick fix unblocks things.
        // TODO: Add an option to enable this.
        if (projectedKind != RazorLanguageKind.Html)
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

        foreach (var s in snippets)
        {
            if (s_razorExcludedSnippets.Contains(s.Shortcut))
            {
                continue;
            }

            // When element names are available, only include snippets whose root
            // element is valid in the current context. The element list is already
            // context-aware (e.g., only tr/thead/tbody inside <table>).
            if (validElementNames.Count > 0)
            {
                var tagName = s_snippetTagNameMap.TryGetValue(s.Shortcut, out var mapped) ? mapped : s.Shortcut;
                if (!validElementNames.Contains(tagName))
                {
                    continue;
                }
            }

            // In start tag context, use the default commit characters for consistency with normal element completion.
            // In text content areas, use an empty commit character set.
            var commitCharacters = isStartTagContext
                ? DefaultCommitCharacters.GetElementCommitCharacterStrings(options.CommitElementsWithSpace)
                : [];

            builder.Add(new VSInternalCompletionItem()
            {
                Label = s.Shortcut,
                Detail = s.Description,
                InsertTextFormat = InsertTextFormat.Snippet,
                InsertText = s.Shortcut,
                Data = s.CompletionData,
                Kind = CompletionItemKind.Snippet,
                CommitCharacters = commitCharacters,
                // Sort snippets after elements with the same label, matching C# behavior.
                SortText = s.SortText,
                VsResolveTextEditOnCommit = true
            });
        }
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
