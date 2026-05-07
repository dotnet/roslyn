// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentSymbols;

internal class DocumentSymbolService(IDocumentMappingService documentMappingService) : IDocumentSymbolService
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;

    public SumType<DocumentSymbol[], SymbolInformation[]>? GetDocumentSymbols(RazorFileKind fileKind, Uri razorDocumentUri, RazorCSharpDocument csharpDocument, SumType<DocumentSymbol[], SymbolInformation[]> csharpSymbols)
    {
        if (csharpSymbols.TryGetFirst(out var documentSymbols))
        {
            return RemapDocumentSymbols(fileKind, csharpDocument, documentSymbols);
        }
        else if (csharpSymbols.TryGetSecond(out var symbolInformations))
        {
            using var _ = ListPool<SymbolInformation>.GetPooledObject(out var mappedSymbols);

            foreach (var symbolInformation in symbolInformations)
            {
                // SymbolInformation is obsolete, but things still return it so we have to handle it
#pragma warning disable CS0618 // Type or member is obsolete
                if (symbolInformation.Name == RenderMethodSignature(fileKind))
                {
                    symbolInformation.Name = RenderMethodDisplay(fileKind);
                    symbolInformation.Location.Range = LspFactory.DefaultRange;
                    symbolInformation.Location.Uri = razorDocumentUri;
                    mappedSymbols.Add(symbolInformation);
                }
                else if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, symbolInformation.Location.Range, out var newRange))
                {
                    symbolInformation.Location.Range = newRange;
                    symbolInformation.Location.Uri = razorDocumentUri;
                    mappedSymbols.Add(symbolInformation);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            return mappedSymbols.ToArray();
        }
        else
        {
            Debug.Fail("Unsupported response type");
            throw new InvalidOperationException();
        }
    }

    private DocumentSymbol[]? RemapDocumentSymbols(RazorFileKind fileKind, RazorCSharpDocument csharpDocument, DocumentSymbol[]? documentSymbols)
    {
        if (documentSymbols is null)
        {
            return null;
        }

        using var _ = ListPool<DocumentSymbol>.GetPooledObject(out var mappedSymbols);

        foreach (var documentSymbol in documentSymbols)
        {
            if (TryRemapRanges(csharpDocument, documentSymbol))
            {
                documentSymbol.Children = RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children);

                mappedSymbols.Add(documentSymbol);
            }
            else if (documentSymbol.Children is [_, ..] &&
                RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children) is [_, ..] mappedChildren)
            {
                // This range didn't map, but some/all of its children did, so we promote them to this level so we don't
                // lose any information.
                mappedSymbols.AddRange(mappedChildren);
            }
        }

        return mappedSymbols.ToArray();

        bool TryRemapRanges(RazorCSharpDocument csharpDocument, DocumentSymbol documentSymbol)
        {
            if (documentSymbol.Detail == RenderMethodSignature(fileKind))
            {
                // Special case BuildRenderTree to always map to the top of the document
                documentSymbol.Detail = RenderMethodDisplay(fileKind);
                documentSymbol.Range = LspFactory.DefaultRange;
                documentSymbol.SelectionRange = LspFactory.DefaultRange;

                return true;
            }
            else if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, documentSymbol.Range, out var newRange) &&
                _documentMappingService.TryMapToRazorDocumentRange(csharpDocument, documentSymbol.SelectionRange, out var newSelectionRange))
            {
                documentSymbol.Range = newRange;
                documentSymbol.SelectionRange = newSelectionRange;

                return true;
            }

            return false;
        }
    }

    private static string RenderMethodSignature(RazorFileKind fileKind)
        => fileKind == RazorFileKind.Legacy
            ? "ExecuteAsync()"
            : "BuildRenderTree(RenderTreeBuilder __builder)";

    private static string RenderMethodDisplay(RazorFileKind fileKind)
        => fileKind == RazorFileKind.Legacy
            ? "ExecuteAsync()"
            : "BuildRenderTree()"; // We hide __builder because it can be misleading to users: https://github.com/dotnet/razor/issues/11960

}
