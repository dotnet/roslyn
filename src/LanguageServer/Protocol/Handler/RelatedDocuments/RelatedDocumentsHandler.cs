// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.RelatedDocuments;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RelatedDocumentsHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RelatedDocumentsHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new RelatedDocumentsHandler();
}

[Method(VSInternalMethods.CopilotRelatedDocumentsName)]
internal sealed class RelatedDocumentsHandler
    : ILspServiceRequestHandler<VSInternalRelatedDocumentParams, VSInternalRelatedDocumentReport[]?>,
      ITextDocumentIdentifierHandler<VSInternalRelatedDocumentParams, TextDocumentIdentifier>
{

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalRelatedDocumentParams requestParams)
        => requestParams.TextDocument;

    public async Task<VSInternalRelatedDocumentReport[]?> HandleRequestAsync(
        VSInternalRelatedDocumentParams requestParams, RequestContext context, CancellationToken cancellationToken)
    {
        context.TraceInformation($"{this.GetType()} started getting related documents");

        var solution = context.Solution;
        var document = context.Document;
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(document);

        context.TraceInformation($"Processing: {document.FilePath}");

        var relatedDocumentsService = document.GetLanguageService<IRelatedDocumentsService>();
        if (relatedDocumentsService == null)
        {
            context.TraceInformation($"Ignoring document '{document.FilePath}' because it does not support related documents");
            return [];
        }

        // The progress object we will stream reports to.
        using var progress = BufferedProgress.Create(requestParams.PartialResultToken);

        var linePosition = requestParams.Position is null
            ? new LinePosition(0, 0)
            : ProtocolConversions.PositionToLinePosition(requestParams.Position);

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var position = text.Lines.GetPosition(linePosition);

        await relatedDocumentsService.GetRelatedDocumentIdsAsync(
            document,
            position,
            (relatedDocumentIds, cancellationToken) =>
            {
                // As the related docs services reports document ids to us, stream those immediately through our
                // progress reporter.
                progress.Report(new VSInternalRelatedDocumentReport
                {
                    FilePaths = relatedDocumentIds.Select(id => solution.GetRequiredDocument(id).FilePath).WhereNotNull().ToArray(),
                });

                return ValueTaskFactory.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);

        // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
        // collecting and return that.
        context.TraceInformation($"{this.GetType()} finished getting related documents");
        return progress.GetFlattenedValues();
    }
}
