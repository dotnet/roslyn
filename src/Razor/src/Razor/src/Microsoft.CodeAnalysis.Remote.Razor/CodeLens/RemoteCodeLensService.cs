// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal class RemoteCodeLensService(in ServiceArgs args) : RazorDocumentServiceBase(args), IRemoteCodeLensService
{
    internal sealed class Factory : FactoryBase<IRemoteCodeLensService>
    {
        protected override IRemoteCodeLensService CreateService(in ServiceArgs args)
            => new RemoteCodeLensService(in args);
    }

    private readonly IDocumentMappingService _documentMappingService = args.ExportProvider.GetExportedValue<IDocumentMappingService>();

    public ValueTask<LspCodeLens[]?> GetCodeLensAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetCodeLensAsync(context, textDocumentIdentifier, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens[]?> GetCodeLensAsync(
        RemoteDocumentContext context,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
    {
        var snapshot = context.Snapshot;
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var csharpCodeLens = await ExternalAccess.Razor.Cohost.Handlers.CodeLens.GetCodeLensAsync(textDocumentIdentifier, generatedDocument, cancellationToken).ConfigureAwait(false);

        if (csharpCodeLens is null)
        {
            return null;
        }

        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetCSharpDocument();
        if (csharpDocument is null)
        {
            return null;
        }

        using var results = new PooledArrayBuilder<LspCodeLens>(csharpCodeLens.Length);

        foreach (var codeLens in csharpCodeLens)
        {
            if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, codeLens.Range, out var razorRange))
            {
                codeLens.Range = razorRange;
                results.Add(codeLens);
            }
        }

        return results.ToArrayAndClear();
    }

    public ValueTask<LspCodeLens?> ResolveCodeLensAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        LspCodeLens codeLens,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveCodeLensAsync(context, codeLens, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens?> ResolveCodeLensAsync(RemoteDocumentContext context, LspCodeLens codeLens, CancellationToken cancellationToken)
    {
        var snapshot = context.Snapshot;
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        return await ExternalAccess.Razor.Cohost.Handlers.CodeLens.ResolveCodeLensAsync(codeLens, generatedDocument, cancellationToken).ConfigureAwait(false);
    }
}
