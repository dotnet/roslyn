// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteSignatureHelpService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteSignatureHelpService
{
    internal sealed class Factory : FactoryBase<IRemoteSignatureHelpService>
    {
        protected override IRemoteSignatureHelpService CreateService(in ServiceArgs args)
            => new RemoteSignatureHelpService(in args);
    }

    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<LspSignatureHelp?> GetSignatureHelpAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId documentId, Position position, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetSignatureHelpsAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<LspSignatureHelp?> GetSignatureHelpsAsync(RemoteDocumentContext context, Position position, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var linePosition = new LinePosition(position.Line, position.Character);
        var absoluteIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(linePosition);

        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        if (DocumentMappingService.TryMapToCSharpDocumentPosition(codeDocument.GetRequiredCSharpDocument(), absoluteIndex, out var mappedPosition, out _))
        {
            var supportsVisualStudioExtensions = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions;
            return await ExternalHandlers.SignatureHelp.GetSignatureHelpAsync(generatedDocument, mappedPosition, supportsVisualStudioExtensions, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }
}
