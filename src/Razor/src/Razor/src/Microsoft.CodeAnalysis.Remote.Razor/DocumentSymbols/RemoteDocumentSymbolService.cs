// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentSymbols;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteDocumentSymbolService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDocumentSymbolService
{
    internal sealed class Factory : FactoryBase<IRemoteDocumentSymbolService>
    {
        protected override IRemoteDocumentSymbolService CreateService(in ServiceArgs args)
            => new RemoteDocumentSymbolService(in args);
    }

    private readonly IDocumentSymbolService _documentSymbolService = args.ExportProvider.GetExportedValue<IDocumentSymbolService>();
    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<SumType<DocumentSymbol[], SymbolInformation[]>?> GetDocumentSymbolsAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, bool useHierarchicalSymbols, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetDocumentSymbolsAsync(context, useHierarchicalSymbols, cancellationToken),
            cancellationToken);

    private async ValueTask<SumType<DocumentSymbol[], SymbolInformation[]>?> GetDocumentSymbolsAsync(RemoteDocumentContext context, bool useHierarchicalSymbols, CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var csharpSymbols = await ExternalHandlers.DocumentSymbols.GetDocumentSymbolsAsync(
            generatedDocument,
            useHierarchicalSymbols,
            supportsVSExtensions: _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
            cancellationToken).ConfigureAwait(false);

        // Roslyn uses an internal "RoslynDocumentSymbol" type, which throws when serialized after we've mapped it, so we have to
        // convert things back to DocumentSymbol. We only need to do the first level though, as our remapping will take care of
        // the children.
        if (csharpSymbols.TryGetFirst(out var roslynDocumentSymbols))
        {
            csharpSymbols = ConvertDocumentSymbols(roslynDocumentSymbols);
        }

        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        return _documentSymbolService.GetDocumentSymbols(context.Snapshot.FileKind, context.Uri, csharpDocument, csharpSymbols);
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
