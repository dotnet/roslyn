// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.CodeLens;
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
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetCodeLensAsync(snapshot, textDocumentIdentifier, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens[]?> GetCodeLensAsync(
        RemoteDocumentSnapshot snapshot,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        using var results = new PooledArrayBuilder<LspCodeLens>();
        using var _ = HashSetPool<(LspRange Range, string? CommandIdentifier, string? Title)>.GetPooledObject(out var seenCodeLenses);

        if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declarationDocument)
        {
            await AddCodeLensAsync(declarationDocument, cancellationToken).ConfigureAwait(false);
        }

        await AddCodeLensAsync(codeDocument.GetRequiredCSharpDocument(declarationDocument: false), cancellationToken).ConfigureAwait(false);

        return results.ToArrayAndClear();

        async ValueTask AddCodeLensAsync(RazorCSharpDocument csharpDocument, CancellationToken cancellationToken)
        {
            var inDeclDocument = csharpDocument.IsDeclarationDocument;
            var generatedDocument = await snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);
            var globalOptions = generatedDocument.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

            var csharpCodeLens = await CodeLensHandler.GetCodeLensAsync(textDocumentIdentifier, generatedDocument, globalOptions, cancellationToken).ConfigureAwait(false);

            foreach (var codeLens in csharpCodeLens)
            {
                if (!_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, codeLens.Range, out var razorRange))
                {
                    continue;
                }

                codeLens.Range = razorRange;
                if (!seenCodeLenses.Add((codeLens.Range, codeLens.Command?.CommandIdentifier, codeLens.Command?.Title)))
                {
                    continue;
                }

                RazorCodeLensResolveData.Wrap(codeLens, textDocumentIdentifier, inDeclDocument);
                results.Add(codeLens);
            }
        }
    }

    public ValueTask<LspCodeLens?> ResolveCodeLensAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        LspCodeLens codeLens,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => ResolveCodeLensAsync(snapshot, codeLens, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens?> ResolveCodeLensAsync(RemoteDocumentSnapshot snapshot, LspCodeLens codeLens, CancellationToken cancellationToken)
    {
        var razorData = RazorCodeLensResolveData.Unwrap(codeLens);
        if (razorData.OriginalData is { } originalData)
        {
            codeLens.Data = originalData;
        }

        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(razorData.InDeclDocument, cancellationToken).ConfigureAwait(false);

        return await CodeLensResolveHandler.ResolveCodeLensAsync(codeLens, generatedDocument, cancellationToken).ConfigureAwait(false);
    }
}
