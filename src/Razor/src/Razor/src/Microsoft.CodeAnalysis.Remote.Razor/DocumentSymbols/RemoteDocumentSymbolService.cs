// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteDocumentSymbolService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDocumentSymbolService
{
    internal sealed class Factory : FactoryBase<IRemoteDocumentSymbolService>
    {
        protected override IRemoteDocumentSymbolService CreateService(in ServiceArgs args)
            => new RemoteDocumentSymbolService(in args);
    }

    private readonly IDocumentSymbolService _documentSymbolService = args.ExportProvider.GetExportedValue<IDocumentSymbolService>();
    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<SumType<DocumentSymbol[], SymbolInformation[]>?> GetDocumentSymbolsAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, bool useHierarchicalSymbols, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetDocumentSymbolsAsync(snapshot, useHierarchicalSymbols, cancellationToken),
            cancellationToken);

    private async ValueTask<SumType<DocumentSymbol[], SymbolInformation[]>?> GetDocumentSymbolsAsync(RemoteDocumentSnapshot snapshot, bool useHierarchicalSymbols, CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var implementationDocument = codeDocument.GetRequiredCSharpDocument(declarationDocument: false);
        var implementationSymbols = await GetCSharpSymbolsAsync(implementationDocument, cancellationToken).ConfigureAwait(false);

        var declarationDocument = codeDocument.GetCSharpDocument(declarationDocument: true);
        SumType<DocumentSymbol[], SymbolInformation[]>? declarationSymbols = null;
        if (declarationDocument is not null)
        {
            declarationSymbols = await GetCSharpSymbolsAsync(declarationDocument, cancellationToken).ConfigureAwait(false);
        }

        return _documentSymbolService.GetDocumentSymbols(
            snapshot.FileKind,
            snapshot.Uri,
            implementationDocument,
            implementationSymbols,
            declarationDocument,
            declarationSymbols);

        async ValueTask<SumType<DocumentSymbol[], SymbolInformation[]>> GetCSharpSymbolsAsync(
            RazorCSharpDocument csharpDocument,
            CancellationToken cancellationToken)
        {
            var generatedDocument = await snapshot
                .GetGeneratedDocumentAsync(csharpDocument.IsDeclarationDocument, cancellationToken)
                .ConfigureAwait(false);

            var csharpSymbols = await DocumentSymbolsHandler.GetDocumentSymbolsAsync(
                generatedDocument,
                useHierarchicalSymbols,
                supportsVSExtensions: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
                cancellationToken).ConfigureAwait(false);

            // Roslyn uses an internal "RoslynDocumentSymbol" type, which throws when serialized after we've mapped it, so we have to
            // convert things back to DocumentSymbol before remapping it.
            if (csharpSymbols.TryGetFirst(out var roslynDocumentSymbols))
            {
                csharpSymbols = ConvertDocumentSymbols(roslynDocumentSymbols);
            }

            return csharpSymbols;
        }
    }

    private static DocumentSymbol[] ConvertDocumentSymbols(DocumentSymbol[] roslynDocumentSymbols)
    {
        var converted = new DocumentSymbol[roslynDocumentSymbols.Length];
        for (var i = 0; i < roslynDocumentSymbols.Length; i++)
        {
            var symbol = roslynDocumentSymbols[i];
            converted[i] = new DocumentSymbol
            {
                Children = symbol.Children is { } children
                    ? ConvertDocumentSymbols(children)
                    : null,
#pragma warning disable CS0618 // Type or member is obsolete
                Deprecated = symbol.Deprecated,
#pragma warning restore CS0618 // Type or member is obsolete
                Detail = symbol.Detail,
                Kind = symbol.Kind,
                Name = symbol.Name,
                Range = symbol.Range,
                SelectionRange = symbol.SelectionRange,
                Tags = symbol.Tags
            };
        }

        return converted;
    }
}
