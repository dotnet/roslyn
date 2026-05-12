// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentSymbols;

internal interface IDocumentSymbolService
{
    SumType<DocumentSymbol[], SymbolInformation[]>? GetDocumentSymbols(RazorFileKind fileKind, Uri razorDocumentUri, RazorCSharpDocument csharpDocument, SumType<DocumentSymbol[], SymbolInformation[]> csharpSymbols);
}
