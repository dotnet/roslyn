// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class DocumentSymbols
{
    public static Task<SumType<DocumentSymbol[], SymbolInformation[]>> GetDocumentSymbolsAsync(Document document, bool useHierarchicalSymbols, CancellationToken cancellationToken)
    {
        // The symbol information service in Roslyn lives in EditorFeatures and has VS dependencies. for glyph images,
        // so isn't available in OOP. The default implementation is available in OOP, but not in the Roslyn MEF composition,
        // so we have to provide our own.
        return DocumentSymbolsHandler.GetDocumentSymbolsAsync(document, useHierarchicalSymbols, RazorLspSymbolInformationCreationService.Instance, cancellationToken);
    }

    private sealed class RazorLspSymbolInformationCreationService : ILspSymbolInformationCreationService
    {
        public static readonly RazorLspSymbolInformationCreationService Instance = new();

        public SymbolInformation Create(string name, string? containerName, LSP.SymbolKind kind, LSP.Location location, Glyph glyph)
#pragma warning disable CS0618 // SymbolInformation is obsolete
            => new()
            {
                Name = name,
                ContainerName = containerName,
                Kind = kind,
                Location = location,
            };
#pragma warning restore CS0618
    }
}
