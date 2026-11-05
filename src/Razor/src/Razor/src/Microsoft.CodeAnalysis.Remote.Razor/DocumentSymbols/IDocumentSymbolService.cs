// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;

internal interface IDocumentSymbolService
{
    TextDocumentDocumentSymbolResult? GetDocumentSymbols(
        RazorFileKind fileKind,
        DocumentUri razorDocumentUri,
        RazorCSharpDocument implementationDocument,
        TextDocumentDocumentSymbolResult implementationSymbols,
        RazorCSharpDocument? declarationDocument,
        TextDocumentDocumentSymbolResult? declarationSymbols,
        CancellationToken cancellationToken);
}
