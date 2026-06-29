// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
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
            context => GetCodeLensAsync(context, textDocumentIdentifier, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens[]?> GetCodeLensAsync(
        RemoteDocumentContext context,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
    {
        var snapshot = context.Snapshot;
        var csharpDocument = await GetCSharpDocumentAsync(context, cancellationToken).ConfigureAwait(false);
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(csharpDocument.IsDeclarationDocument, cancellationToken).ConfigureAwait(false);
        var globalOptions = generatedDocument.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        var csharpCodeLens = await CodeLensHandler.GetCodeLensAsync(textDocumentIdentifier, generatedDocument, globalOptions, cancellationToken).ConfigureAwait(false);
        if (csharpCodeLens is null)
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

        static async ValueTask<RazorCSharpDocument> GetCSharpDocumentAsync(RemoteDocumentContext context, CancellationToken cancellationToken)
        {
            var codeDocument = await context.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
            // CodeLens items are only produced for the class members in a generated C# file, so they will always be in the
            // decl document if it exists.
            if (codeDocument.GetCSharpDocument(declarationDocument: true) is { } declCSharpDocument)
            {
                return declCSharpDocument;
            }

            return codeDocument.GetRequiredCSharpDocument(declarationDocument: false);
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
            context => ResolveCodeLensAsync(context, codeLens, cancellationToken),
            cancellationToken);

    private async ValueTask<LspCodeLens?> ResolveCodeLensAsync(RemoteDocumentContext context, LspCodeLens codeLens, CancellationToken cancellationToken)
    {
        var snapshot = context.Snapshot;
        var razorData = RazorCodeLensResolveData.Unwrap(codeLens);
        if (razorData.OriginalData is { } originalData)
        {
            codeLens.Data = originalData;
        }

        // CodeLens shows information about fields, properties, methods etc. which, for components, all appear in a declaration document. For legacy documents
        // there is no declaration document, so those things appear in the implementation document. For simplicity, we'll just attempt to resolve from the declaration
        // document and fallback to the implementation document when it's null.
        var generatedDocument = await snapshot.TryGetGeneratedDocumentAsync(declarationDocument: true, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            generatedDocument = await snapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        }

        return await CodeLensResolveHandler.ResolveCodeLensAsync(codeLens, generatedDocument, cancellationToken).ConfigureAwait(false);
    }
}
