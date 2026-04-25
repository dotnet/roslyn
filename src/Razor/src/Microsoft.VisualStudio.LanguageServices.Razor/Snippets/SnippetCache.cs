// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using static Microsoft.VisualStudio.Razor.Snippets.XmlSnippetParser;

namespace Microsoft.VisualStudio.Razor.Snippets;

[Export(typeof(SnippetCache))]
internal sealed class SnippetCache
{
    private Dictionary<SnippetLanguage, ImmutableArray<SnippetInfo>> _snippetCache = new();
    private ReadWriterLocker _lock = new();

    internal void Update(SnippetLanguage language, ImmutableArray<SnippetInfo> snippets)
    {
        using (_lock.EnterWriteLock())
        {
            _snippetCache[language] = snippets;
        }
    }

    public ImmutableArray<SnippetInfo> GetSnippets(SnippetLanguage language)
    {
        using var _ = _lock.EnterReadLock();
        return _snippetCache[language];
    }

    internal string? TryResolveSnippetString(SnippetCompletionData completionData)
    {
        ImmutableArray<SnippetInfo> snippets;
        using (_lock.EnterReadLock())
        {
            snippets = _snippetCache.Values.SelectManyAsArray(v => v);
        }

        // Search through all the snippets to find a match
        var snippet = snippets.FirstOrDefault(completionData.Matches);
        if (snippet is null)
        {
            return null;
        }

        var parsedSnippet = snippet.GetParsedXmlSnippet();
        if (parsedSnippet is null)
        {
            return null;
        }

        using var _ = StringBuilderPool.GetPooledObject(out var functionSnippetBuilder);

        foreach (var part in parsedSnippet.Parts)
        {
            var toAppend = part is SnippetShortcutPart
                ? snippet.Shortcut
                : part.DefaultText;

            functionSnippetBuilder.Append(toAppend);
        }

        return functionSnippetBuilder.ToString();
    }
}
