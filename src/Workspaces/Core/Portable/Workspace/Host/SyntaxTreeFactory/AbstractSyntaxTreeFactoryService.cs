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
    private readonly ISyntaxTreeCacheService _syntaxTreeCache;

    protected AbstractSyntaxTreeFactoryService(ISyntaxTreeCacheService syntaxTreeCache)
    {
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

        return _syntaxTreeCache.GetOrCreateSyntaxTree(
            text,
            options,
            static (state, cancellationToken) => state.service.ParseSyntaxTreeCore(
                state.filePath, state.options, state.text, cancellationToken),
            static (root, state) => state.service.CreateSyntaxTree(
                state.filePath, state.options, state.text, state.text.Encoding, state.text.ChecksumAlgorithm, root),
            (service: this, filePath, options, text),
            cancellationToken);
    }

    protected abstract SyntaxTree ParseSyntaxTreeCore(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
}
