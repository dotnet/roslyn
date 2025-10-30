// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExtractInterface;

internal sealed class ExtractInterfaceTypeAnalysisResult
{
    public readonly bool CanExtractInterface;
    public readonly Document DocumentToExtractFrom;
    public readonly SyntaxNode TypeNode;
    public readonly INamedTypeSymbol TypeToExtractFrom;
    public readonly ImmutableArray<ISymbol> ExtractableMembers;
    public readonly SyntaxFormattingOptions FormattingOptions;
    public readonly string ErrorMessage;

    public ExtractInterfaceTypeAnalysisResult(
        Document documentToExtractFrom,
        SyntaxNode typeNode,
        INamedTypeSymbol typeToExtractFrom,
        ImmutableArray<ISymbol> extractableMembers,
        SyntaxFormattingOptions formattingOptions)
    {
        CanExtractInterface = true;
        DocumentToExtractFrom = documentToExtractFrom;
        TypeNode = typeNode;
        TypeToExtractFrom = typeToExtractFrom;
        ExtractableMembers = extractableMembers;
        FormattingOptions = formattingOptions;
    }

    public ExtractInterfaceTypeAnalysisResult(string errorMessage)
    {
        CanExtractInterface = false;
        ErrorMessage = errorMessage;
    }
}
