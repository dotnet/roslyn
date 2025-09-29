// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

[ExportCSharpVisualBasicStatelessLspService(typeof(SourceGeneratedDocumentGetTextHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratedDocumentGetTextHandler() : ILspServiceDocumentRequestHandler<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>
{
    public const string MethodName = "sourceGeneratedDocument/_roslyn_getText";

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(SourceGeneratorGetTextParams request) => request.TextDocument;

    public async Task<SourceGeneratedDocumentText> HandleRequestAsync(SourceGeneratorGetTextParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;

        if (document is null)
        {
            // The source generated file being asked about is not present.
            // This is a rare case the request queue always gives us a frozen, non-null document for any opened sg document,
            // even if the generator itself was removed and the document no longer exists in the host solution.
            //
            // We can only get a null document here if the sg document has not been opened and
            // the source generated document does not exist in the workspace.
            //
            // Return a value indicating that the document is removed.
            return new SourceGeneratedDocumentText(ResultId: null, Text: null);
        }

        // Nothing here strictly prevents this from working on any other document, but we'll assert we got a source-generated file, since
        // it wouldn't really make sense for the server to be asked for the contents of a regular file. Since this endpoint is intended for
        // source-generated files only, this would indicate that something else has gone wrong.
        Contract.ThrowIfFalse(document is SourceGeneratedDocument);

        var cache = context.GetRequiredLspService<SourceGeneratedDocumentCache>();
        var projectOrDocument = new ProjectOrDocumentId(document.Id);

        using var _ = PooledDictionary<ProjectOrDocumentId, PreviousPullResult>.GetInstance(out var previousPullResults);
        if (request.ResultId is not null)
        {
            previousPullResults.Add(projectOrDocument, new PreviousPullResult(request.ResultId, request.TextDocument));
        }

        var newResult = await cache.GetOrComputeNewDataAsync(previousPullResults, projectOrDocument, document.Project, new SourceGeneratedDocumentGetTextState(document), cancellationToken).ConfigureAwait(false);

        if (newResult is null)
        {
            Contract.ThrowIfNull(request.ResultId, "Attempted to reuse cache entry but given no resultId");
            // The generated document is the same, we can return the same resultId.
            return new SourceGeneratedDocumentText(request.ResultId, Text: null);
        }
        else
        {
            // We may get no text back if the unfrozen source generated file no longer exists.
            var data = newResult.Value.Data?.ToString();
            return new SourceGeneratedDocumentText(newResult.Value.ResultId, data);
        }
    }
}
