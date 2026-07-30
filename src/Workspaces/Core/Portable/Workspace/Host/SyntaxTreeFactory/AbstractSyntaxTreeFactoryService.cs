// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

internal abstract partial class AbstractSyntaxTreeFactoryService : ISyntaxTreeFactoryService
{
    private readonly string _language;
    private readonly ISyntaxTreeCacheService _syntaxTreeCache;

    protected AbstractSyntaxTreeFactoryService(string language, ISyntaxTreeCacheService syntaxTreeCache)
    {
        _language = language;
        _syntaxTreeCache = syntaxTreeCache;
    }

    public abstract ParseOptions GetDefaultParseOptions();
    public abstract ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();
    public abstract bool OptionsDifferOnlyByPreprocessorDirectives(ParseOptions options1, ParseOptions options2);
    public abstract ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> metadata);
    public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, SourceText text, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, SyntaxNode root);

    public SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken)
    {
        options ??= GetDefaultParseOptions();

        if (_syntaxTreeCache is null)
            return ParseSyntaxTreeCore(filePath, options, text, cancellationToken);

        var key = _syntaxTreeCache.CreateKey(_language, text, options);
        if (_syntaxTreeCache.TryGetRoot(key, out var cachedRoot))
        {
            var cachedTree = CreateSyntaxTree(filePath, options, text, text.Encoding, text.ChecksumAlgorithm, cachedRoot);
            _syntaxTreeCache.RefreshRoot(key, cachedTree.GetRoot(cancellationToken));
            return cachedTree;
        }

        var parsedTree = ParseSyntaxTreeCore(filePath, options, text, cancellationToken);
        var parsedRoot = parsedTree.GetRoot(cancellationToken);
        var canonicalRoot = _syntaxTreeCache.GetOrAddRoot(key, parsedRoot);

        if (canonicalRoot == parsedRoot)
            return parsedTree;

        var raceTree = CreateSyntaxTree(filePath, options, text, text.Encoding, text.ChecksumAlgorithm, canonicalRoot);
        _syntaxTreeCache.RefreshRoot(key, raceTree.GetRoot(cancellationToken));
        return raceTree;
    }

    protected abstract SyntaxTree ParseSyntaxTreeCore(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
}
