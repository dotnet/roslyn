// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Factory service for creating syntax trees.
/// </summary>
internal interface ISyntaxTreeFactoryService : ILanguageService
{
    ParseOptions GetDefaultParseOptions();

    ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();

    ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> compilationOptionsMetadata);

    /// <summary>
    /// Returns true if the two options differ only by preprocessor directives; this allows for us to reuse trees
    /// if they don't have preprocessor directives in them.
    /// </summary>
    bool OptionsDifferOnlyByPreprocessorDirectives(ParseOptions options1, ParseOptions options2);

    // new tree from root node
    SyntaxTree CreateSyntaxTree(string? filePath, ParseOptions options, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, SyntaxNode root);

    // new tree from text
    SyntaxTree ParseSyntaxTree(string? filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
}
