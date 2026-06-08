// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;

internal interface IDocumentSymbolService
{
    SumType<DocumentSymbol[], SymbolInformation[]>? GetDocumentSymbols(RazorFileKind fileKind, DocumentUri razorDocumentUri, RazorCSharpDocument csharpDocument, SumType<DocumentSymbol[], SymbolInformation[]> csharpSymbols);
}
