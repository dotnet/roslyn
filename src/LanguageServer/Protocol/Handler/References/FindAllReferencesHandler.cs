// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(FindAllReferencesHandler)), Shared]
[Method(LSP.Methods.TextDocumentReferencesName)]
internal sealed class FindAllReferencesHandler : ILspServiceDocumentRequestHandler<ReferenceParams, VSInternalReferenceResult?>
{
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FindAllReferencesHandler(
        IMetadataAsSourceFileService metadataAsSourceFileService,
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        IGlobalOptionService globalOptions)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.LanguageServer);
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(ReferenceParams request) => request.TextDocument;

    public async Task<VSInternalReferenceResult?> HandleRequestAsync(
        ReferenceParams referenceParams,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = await context.GetRequiredDocumentAsync(cancellationToken).ConfigureAwait(false);
        var workspace = await context.GetRequiredWorkspaceAsync(cancellationToken).ConfigureAwait(false);

        var linePosition = ProtocolConversions.PositionToLinePosition(referenceParams.Position);
        var clientCapabilities = context.GetRequiredClientCapabilities();

        var supportsVsExtensions = clientCapabilities.HasVisualStudioLspCapability();
        using var progress = BufferedProgress.Create<ReferenceItem[], VSInternalReferenceResult?>(
            referenceParams.PartialResultToken, ToResult);

        var includeDeclaration = referenceParams.Context.IncludeDeclaration;
        var hasReferences = await FindReferencesAsync(progress, workspace, document, linePosition, clientCapabilities.HasVisualStudioLspCapability(), includeDeclaration, _globalOptions, _metadataAsSourceFileService, _asyncListener, cancellationToken).ConfigureAwait(false);

        var references = progress.GetFlattenedValues();
        if (!hasReferences)
            await context.GetRequiredLspService<RequestTelemetryLogger>().ReportEmptySymbolResultAsync(LSP.Methods.TextDocumentReferencesName, document, referenceParams.Position, cancellationToken).ConfigureAwait(false);

        return references is null ? null : ToResult(references);

        VSInternalReferenceResult ToResult(ReferenceItem[] items)
            => supportsVsExtensions
                ? new(items.Select(item => (VSInternalReferenceItem)item.Value).ToArray())
                : new(items.Select(item => (LSP.Location)item.Value).ToArray());
    }

    internal static async Task<bool> FindReferencesAsync(
        IProgress<ReferenceItem[]> progress,
        Workspace workspace,
        Document document,
        LinePosition linePosition,
        bool supportsVSExtensions,
        bool includeDeclaration,
        IGlobalOptionService globalOptions,
        IMetadataAsSourceFileService metadataAsSourceFileService,
        IAsynchronousOperationListener asyncListener,
        CancellationToken cancellationToken)
    {
        var findUsagesService = document.GetRequiredLanguageService<IFindUsagesLSPService>();
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        var findUsagesContext = new FindUsagesLSPContext(
            progress, workspace, document, position, metadataAsSourceFileService, asyncListener, globalOptions, supportsVSExtensions, includeDeclaration, cancellationToken);

        // Finds the references for the symbol at the specific position in the document, reporting them via streaming to the LSP client.
        var classificationOptions = globalOptions.GetClassificationOptionsProvider();
        await findUsagesService.FindReferencesAsync(findUsagesContext, document, position, classificationOptions, cancellationToken).ConfigureAwait(false);
        await findUsagesContext.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
        return findUsagesContext.HasReportedReferences;
    }
}
