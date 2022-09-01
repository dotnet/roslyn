// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

/// <summary>
/// Server instance agnostic snippet parser and cache.
/// This can be re-used across LSP servers as we're just storing an
/// internal representation of an XML snippet.
/// </summary>
[Export(typeof(XmlSnippetParser)), Shared]
internal partial class XmlSnippetParser
{
    /// <summary>
    /// Cache to hold onto the parsed XML for a particular snippet.
    /// </summary>
    private readonly ConcurrentDictionary<string, ParsedXmlSnippet?> _parsedSnippetsCache = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public XmlSnippetParser()
    {
    }

    internal ParsedXmlSnippet? GetParsedXmlSnippet(SnippetInfo matchingSnippetInfo, RequestContext context)
    {
        if (_parsedSnippetsCache.TryGetValue(matchingSnippetInfo.Title, out var cachedSnippet))
        {
            if (cachedSnippet == null)
            {
                context.TraceWarning($"Returning a null cached snippet for {matchingSnippetInfo.Title}");
            }

            return cachedSnippet;
        }

        ParsedXmlSnippet? parsedSnippet = null;
        try
        {
            context.TraceInformation($"Reading snippet for {matchingSnippetInfo.Title} with path {matchingSnippetInfo.Path}");
            parsedSnippet = GetAndParseSnippetFromFile(matchingSnippetInfo);
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
        {
            context.TraceError($"Got exception parsing xml snippet {matchingSnippetInfo.Title} from file {matchingSnippetInfo.Path}");
            context.TraceException(ex);
        }

        // Add the snippet to the cache regardless of if we succeeded in parsing it.
        // We're not likely to succeed in parsing on a second try if we failed initially, so we cache it to avoid repeatedly failing.
        _parsedSnippetsCache.TryAdd(matchingSnippetInfo.Title, parsedSnippet);
        return parsedSnippet;
    }

    private static ParsedXmlSnippet GetAndParseSnippetFromFile(SnippetInfo snippetInfo)
    {
        // Read the XML file to get the snippet and snippet metadata.
        var matchingSnippet = RetrieveSnippetXmlFromFile(snippetInfo);

        Contract.ThrowIfFalse(matchingSnippet.IsExpansionSnippet(), "Only expansion snippets are supported");

        if (!matchingSnippet.IsExpansionSnippet())
        {
            throw new InvalidOperationException();
        }

        var expansion = new ExpansionTemplate(matchingSnippet);

        // Parse the snippet XML into snippet parts we can cache.
        var parsedSnippet = expansion.Parse();
        return parsedSnippet;
    }

    private static CodeSnippet RetrieveSnippetXmlFromFile(SnippetInfo snippetInfo)
    {
        var path = snippetInfo.Path;
        if (path == null)
        {
            throw new ArgumentException($"Missing file path for snippet {snippetInfo.Title}");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Snippet {snippetInfo.Title} has an invalid file path: {snippetInfo.Path}");
        }

        // Load the xml for the snippet from disk.
        // Any exceptions thrown here we allow to bubble up and let the queue log it.
        var snippet = CodeSnippet.ReadSnippetFromFile(snippetInfo.Path, snippetInfo.Title);
        return snippet;
    }

    internal TestAccessor GetTestAccessor() => new TestAccessor(this);

    internal readonly struct TestAccessor
    {
        private readonly XmlSnippetParser _snippetParser;
        public TestAccessor(XmlSnippetParser snippetParser)
        {
            _snippetParser = snippetParser;
        }

        public int GetCachedSnippetsCount() => _snippetParser._parsedSnippetsCache.Count;

        public ParsedXmlSnippet GetCachedSnippet(string snippet) => _snippetParser._parsedSnippetsCache[snippet]!;
    }
}
