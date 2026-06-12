// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;

[Export(typeof(IDocumentSymbolService)), Shared]
[method: ImportingConstructor]
internal sealed class DocumentSymbolService(IDocumentMappingService documentMappingService) : IDocumentSymbolService
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;

    public SumType<DocumentSymbol[], SymbolInformation[]>? GetDocumentSymbols(RazorFileKind fileKind, DocumentUri razorDocumentUri, RazorCSharpDocument csharpDocument, SumType<DocumentSymbol[], SymbolInformation[]> csharpSymbols)
    {
        if (csharpSymbols.TryGetFirst(out var documentSymbols))
        {
            return RemapDocumentSymbols(fileKind, csharpDocument, documentSymbols, synthesizeRenderMethodIfEmpty: true);
        }
        else if (csharpSymbols.TryGetSecond(out var symbolInformations))
        {
            using var _ = ListPool<SymbolInformation>.GetPooledObject(out var mappedSymbols);
            var foundRenderMethodSymbol = false;

            foreach (var symbolInformation in symbolInformations)
            {
                // SymbolInformation is obsolete, but things still return it so we have to handle it
#pragma warning disable CS0618 // Type or member is obsolete
                if (symbolInformation.Name == RenderMethodSignature(fileKind))
                {
                    foundRenderMethodSymbol = true;
                    symbolInformation.Name = RenderMethodDisplay(fileKind);
                    symbolInformation.Location.Range = LspFactory.DefaultRange;
                    symbolInformation.Location.DocumentUri = razorDocumentUri;
                    mappedSymbols.Add(symbolInformation);
                }
                else if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, symbolInformation.Location.Range, out var newRange))
                {
                    symbolInformation.Location.Range = newRange;
                    symbolInformation.Location.DocumentUri = razorDocumentUri;
                    mappedSymbols.Add(symbolInformation);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (!foundRenderMethodSymbol)
            {
                mappedSymbols.Insert(0, CreateRenderMethodSymbolInformation(fileKind, razorDocumentUri));
            }

            return mappedSymbols.ToArray();
        }
        else
        {
            Debug.Fail("Unsupported response type");
            throw new InvalidOperationException();
        }
    }

    private DocumentSymbol[]? RemapDocumentSymbols(RazorFileKind fileKind, RazorCSharpDocument csharpDocument, DocumentSymbol[]? documentSymbols, bool synthesizeRenderMethodIfEmpty)
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
                documentSymbol.Children = RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children, synthesizeRenderMethodIfEmpty: false);

                mappedSymbols.Add(documentSymbol);
            }
            else if (documentSymbol.Children is [_, ..] &&
                RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children, synthesizeRenderMethodIfEmpty: false) is [_, ..] mappedChildren)
            {
                // This range didn't map, but some/all of its children did, so we promote them to this level so we don't
                // lose any information.
                mappedSymbols.AddRange(mappedChildren);
            }
        }

        if (synthesizeRenderMethodIfEmpty && mappedSymbols.Count == 0)
        {
            mappedSymbols.Add(CreateRenderMethodDocumentSymbol(fileKind));
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

    private static SymbolInformation CreateRenderMethodSymbolInformation(RazorFileKind fileKind, DocumentUri razorDocumentUri)
    {
        // SymbolInformation is obsolete, but things still return it so we have to handle it
#pragma warning disable CS0618 // Type or member is obsolete
        return new SymbolInformation
        {
            Name = RenderMethodDisplay(fileKind),
            Kind = Roslyn.LanguageServer.Protocol.SymbolKind.Method,
            Location = new()
            {
                DocumentUri = razorDocumentUri,
                Range = LspFactory.DefaultRange,
            },
        };
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static DocumentSymbol CreateRenderMethodDocumentSymbol(RazorFileKind fileKind)
        => new()
        {
            Name = RenderMethodDisplay(fileKind),
            Kind = Roslyn.LanguageServer.Protocol.SymbolKind.Method,
            Range = LspFactory.DefaultRange,
            SelectionRange = LspFactory.DefaultRange,
        };

}
