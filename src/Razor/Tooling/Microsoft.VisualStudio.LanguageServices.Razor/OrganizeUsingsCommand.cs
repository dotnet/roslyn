// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IInterceptedCommand))]
[method: ImportingConstructor]
internal sealed class OrganizeUsingsCommand(IRemoteServiceInvoker remoteServiceInvoker) : IInterceptedCommand
{
    // Roslyn C# command group (guidCSharpGrpId) — used for "Remove and Sort Usings"
    private static readonly Guid s_cSharpGroupGuid = new("5d7e7f65-a63f-46ee-84f1-990b2cab23f9");
    private const uint SortUsingsCommandId = 0x1922;                  // cmdidCSharpOrganizeSortUsings (edit menu)
    private const uint RemoveAndSortUsingsCommandId = 0x1923;         // cmdidCSharpOrganizeRemoveAndSort (edit menu)
    private const uint ContextRemoveAndSortUsingsCommandId = 0x1913;  // cmdidContextOrganizeRemoveAndSort (context menu)

    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    public bool QueryStatus(Guid pguidCmdGroup, uint nCmdID)
    {
        return pguidCmdGroup == s_cSharpGroupGuid &&
            (nCmdID == RemoveAndSortUsingsCommandId ||
             nCmdID == ContextRemoveAndSortUsingsCommandId ||
             nCmdID == SortUsingsCommandId);
    }

    public Task<ImmutableArray<TextChange>> ExecuteAsync(
        Solution solution,
        DocumentId documentId,
        uint nCmdID,
        CancellationToken cancellationToken)
    {
        // Sort usings is a bit simpler, so we can just execute it directly
        if (nCmdID == SortUsingsCommandId)
        {
            return _remoteServiceInvoker.TryInvokeAsync<IRemoteRemoveAndSortUsingsService, ImmutableArray<TextChange>>(
                    solution,
                    (service, solutionInfo, ct) => service.GetSortUsingsEditsAsync(solutionInfo, documentId, ct),
                    cancellationToken).AsTask();
        }

        return ExecuteRemoveAndSortUsingsAsync(solution, documentId, cancellationToken);
    }

    private async Task<ImmutableArray<TextChange>> ExecuteRemoveAndSortUsingsAsync(Solution solution, DocumentId documentId, CancellationToken cancellationToken)
    {
        // To ensure our unused usings cache is populated, we first ask Roslyn for diagnostics, to get IDE005_gen diagnostics,
        // then we request our own diagnostics, to have it filter Roslyn's diagnostics, and make sure our cache is up to date.
        var razorDocument = solution.GetAdditionalDocument(documentId);
        if (razorDocument is null)
        {
            return [];
        }

        var generatedDocument = await razorDocument.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(razorDocument, cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return [];
        }

        // C# diagnostics
        var csharpDiagnostics = await ExternalHandlers.Diagnostics.GetDocumentDiagnosticsAsync(generatedDocument, supportsVisualStudioExtensions: true, cancellationToken).ConfigureAwait(false);

        // Razor diagnostics (to filter and hydrate cache)
        await _remoteServiceInvoker.TryInvokeAsync<IRemoteDiagnosticsService, ImmutableArray<LspDiagnostic>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetDiagnosticsAsync(solutionInfo, razorDocument.Id, [.. csharpDiagnostics], htmlDiagnostics: [], cancellationToken),
            cancellationToken).ConfigureAwait(false);

        // Now do the remove and sort
        return await _remoteServiceInvoker.TryInvokeAsync<IRemoteRemoveAndSortUsingsService, ImmutableArray<TextChange>>(
            solution,
            (service, solutionInfo, ct) => service.GetRemoveAndSortUsingsEditsAsync(solutionInfo, documentId, ct),
            cancellationToken).ConfigureAwait(false);
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal class TestAccessor(OrganizeUsingsCommand command)
    {
        public bool QueryRemoveAndSortUsings()
            => command.QueryStatus(s_cSharpGroupGuid, RemoveAndSortUsingsCommandId);

        public bool QuerySortUsings()
            => command.QueryStatus(s_cSharpGroupGuid, SortUsingsCommandId);

        public Task<ImmutableArray<TextChange>> ExecuteRemoveAndSortUsingsAsync(Solution solution, DocumentId documentId, CancellationToken cancellationToken)
            => command.ExecuteAsync(solution, documentId, RemoveAndSortUsingsCommandId, cancellationToken);

        public Task<ImmutableArray<TextChange>> ExecuteSortUsingsAsync(Solution solution, DocumentId documentId, CancellationToken cancellationToken)
            => command.ExecuteAsync(solution, documentId, SortUsingsCommandId, cancellationToken);
    }
}
