// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ISyntaxTreeFactoryService), LanguageNames.CSharp), Shared]
internal partial class CSharpSyntaxTreeFactoryService : AbstractSyntaxTreeFactoryService
{
    private static readonly CSharpParseOptions _parseOptionWithLatestLanguageVersion = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSyntaxTreeFactoryService()
    {
    }

    public override ParseOptions GetDefaultParseOptions()
        => CSharpParseOptions.Default;

    public override ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion()
        => _parseOptionWithLatestLanguageVersion;

    public override ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("language-version", out var langVersionString) ||
            !LanguageVersionFacts.TryParse(langVersionString, out var langVersion))
        {
            langVersion = LanguageVersion.Default;
        }

        return new CSharpParseOptions(
            languageVersion: langVersion,
            preprocessorSymbols: metadata.TryGetValue("define", out var defines) ? defines.Split(',') : null);
    }

    public override bool OptionsDifferOnlyByPreprocessorDirectives(ParseOptions options1, ParseOptions options2)
    {
        var csharpOptions1 = (CSharpParseOptions)options1;
        var csharpOptions2 = (CSharpParseOptions)options2;

        // The easy way to figure out if these only differ by a single field is to update one with the preprocessor symbols of the
        // other, and then do an equality check from there; this is future proofed if another value is ever added.
        return csharpOptions1.WithPreprocessorSymbols(csharpOptions2.PreprocessorSymbolNames) == csharpOptions2;
    }

    public override SyntaxTree CreateSyntaxTree(string filePath, ParseOptions options, SourceText text, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, SyntaxNode root)
    {
        options ??= GetDefaultParseOptions();
        return new ParsedSyntaxTree(text, (CSharpSyntaxNode)root, (CSharpParseOptions)options, filePath, encoding, checksumAlgorithm);
    }

    public override SyntaxTree ParseSyntaxTree(string filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken)
    {
        options ??= GetDefaultParseOptions();
        return SyntaxFactory.ParseSyntaxTree(text, options, filePath, cancellationToken: cancellationToken);
    }
}
