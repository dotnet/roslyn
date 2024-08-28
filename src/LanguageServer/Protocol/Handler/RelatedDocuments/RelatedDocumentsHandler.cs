// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Serialization;
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
    /// <summary>
    /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance
    /// changed. The version key is produced by combining the checksums for project options <see
    /// cref="ProjectState.GetParseOptionsChecksum"/> and <see cref="DocumentStateChecksums.Text"/>
    /// </summary>
    private readonly VersionedPullCache<(Checksum parseOptionsChecksum, Checksum textChecksum)?> _versionedCache = new(nameof(RelatedDocumentsHandler));

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    private static async Task<(Checksum parseOptionsChecksum, Checksum textChecksum)> ComputeChecksumsAsync(Document document, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var parseOptionsChecksum = project.State.GetParseOptionsChecksum();

        var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
        var textChecksum = documentChecksumState.Text;

        return (parseOptionsChecksum, textChecksum);
    }

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalRelatedDocumentParams requestParams)
        => requestParams.TextDocument;

    /// <summary>
    /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files.
    /// </summary>
    private static ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalRelatedDocumentParams requestParams)
        => requestParams.PreviousResultId != null && requestParams.TextDocument != null
            ? [new PreviousPullResult(requestParams.PreviousResultId, requestParams.TextDocument)]
            // The client didn't provide us with a previous result to look for, so we can't lookup anything.
            : null;

    public async Task<VSInternalRelatedDocumentReport[]?> HandleRequestAsync(
        VSInternalRelatedDocumentParams requestParams, RequestContext context, CancellationToken cancellationToken)
    {
        context.TraceInformation($"{this.GetType()} started getting related documents");

        // The progress object we will stream reports to.
        using var progress = BufferedProgress.Create(requestParams.PartialResultToken);

        context.TraceInformation($"PreviousResultId={requestParams.PreviousResultId}");

        var solution = context.Solution;
        var document = context.Document;
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(document);

        context.TraceInformation($"Processing: {document.FilePath}");

        var relatedDocumentsService = document.GetLanguageService<IRelatedDocumentsService>();
        if (relatedDocumentsService == null)
        {
            context.TraceInformation($"Ignoring document '{document.FilePath}' because it does not support related documents");
            return;
        }

        var documentToPreviousParams = new Dictionary<Document, PreviousPullResult>();
        if (requestParams.PreviousResultId != null)
            documentToPreviousParams.Add(document, new PreviousPullResult(requestParams.PreviousResultId, requestParams.TextDocument));

        var newResultId = await _versionedCache.GetNewResultIdAsync(
            documentToPreviousParams,
            document,
            computeVersionAsync: async () => await ComputeChecksumsAsync(document, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        if (newResultId != null)
        {
            context.TraceInformation($"Version was changed for document: {document.FilePath}");

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
                        ResultId = newResultId,
                        FilePaths = relatedDocumentIds.Select(id => solution.GetRequiredDocument(id).FilePath).WhereNotNull().ToArray(),
                    });

                    return ValueTaskFactory.CompletedTask;
                },
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            context.TraceInformation($"Version was unchanged for document: {document.FilePath}");

            // Nothing changed between the last request and this one.  Report a (null-file-paths, same-result-id)
            // response to the client as that means they should just preserve the current related file paths they
            // have for this file.
            progress.Report(new VSInternalRelatedDocumentReport { ResultId = requestParams.PreviousResultId });
        }

        // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
        // collecting and return that.
        context.TraceInformation($"{this.GetType()} finished getting related documents");
        return progress.GetFlattenedValues();
    }
}
