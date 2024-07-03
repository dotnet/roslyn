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
    public abstract ParseOptions GetDefaultParseOptions();
    public abstract ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();
    public abstract bool OptionsDifferOnlyByPreprocessorDirectives(ParseOptions options1, ParseOptions options2);
    public abstract ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> metadata);
    public abstract SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, SyntaxNode root);
    public abstract SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);
}
