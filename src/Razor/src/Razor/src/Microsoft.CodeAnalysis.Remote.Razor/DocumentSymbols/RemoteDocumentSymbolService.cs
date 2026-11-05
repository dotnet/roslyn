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

    public ValueTask<TextDocumentDocumentSymbolResult?> GetDocumentSymbolsAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, bool useHierarchicalSymbols, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetDocumentSymbolsAsync(snapshot, useHierarchicalSymbols, cancellationToken),
            cancellationToken);

    private async ValueTask<TextDocumentDocumentSymbolResult?> GetDocumentSymbolsAsync(RemoteDocumentSnapshot snapshot, bool useHierarchicalSymbols, CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var implementationDocument = codeDocument.GetRequiredCSharpDocument(declarationDocument: false);
        var implementationSymbols = await GetCSharpSymbolsAsync(implementationDocument, cancellationToken).ConfigureAwait(false);

        var declarationDocument = codeDocument.GetCSharpDocument(declarationDocument: true);
        TextDocumentDocumentSymbolResult? declarationSymbols = null;
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
            declarationSymbols,
            cancellationToken);

        async ValueTask<TextDocumentDocumentSymbolResult> GetCSharpSymbolsAsync(
            RazorCSharpDocument csharpDocument,
            CancellationToken cancellationToken)
        {
            var generatedDocument = await snapshot
                .GetGeneratedDocumentAsync(csharpDocument.IsDeclarationDocument, cancellationToken)
                .ConfigureAwait(false);

            return await DocumentSymbolsHandler.GetDocumentSymbolsAsync(
                generatedDocument,
                useHierarchicalSymbols,
                supportsVSExtensions: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
