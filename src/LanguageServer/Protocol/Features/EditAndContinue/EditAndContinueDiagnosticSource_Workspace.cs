// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal static partial class EditAndContinueDiagnosticSource
{
    private sealed class ProjectSource(Project project, ImmutableArray<DiagnosticData> diagnostics) : AbstractProjectDiagnosticSource(project)
    {
        public override bool IsLiveSource()
            => true;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
            => Task.FromResult(diagnostics);
    }

    private sealed class ClosedDocumentSource(TextDocument document, ImmutableArray<DiagnosticData> diagnostics) : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        public override bool IsLiveSource()
            => true;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken)
            => Task.FromResult(diagnostics);
    }

    public static async ValueTask<ImmutableArray<IDiagnosticSource>> CreateWorkspaceDiagnosticSourcesAsync(Solution solution, Func<Document, bool> isDocumentOpen, CancellationToken cancellationToken)
    {
        // Do not report EnC diagnostics for a non-host workspace, or if Hot Reload/EnC session is not active.
        if (solution.WorkspaceKind != WorkspaceKind.Host ||
            solution.Services.GetService<IEditAndContinueWorkspaceService>()?.SessionTracker is not { IsSessionActive: true } sessionStateTracker)
        {
            return [];
        }

        using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var sources);

        var applyDiagnostics = sessionStateTracker.ApplyChangesDiagnostics;

        var dataByDocument = from data in applyDiagnostics
                             where data.DocumentId != null
                             group data by data.DocumentId into documentData
                             select documentData;

        // diagnostics associated with closed documents:
        foreach (var (documentId, diagnostics) in dataByDocument)
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document != null && !isDocumentOpen(document))
            {
                sources.Add(new ClosedDocumentSource(document, diagnostics.ToImmutableArray()));
            }
        }

        // diagnostics not associated with a document:
        sources.AddRange(
            from data in applyDiagnostics
            where data.DocumentId == null && data.ProjectId != null
            group data by data.ProjectId into projectData
            let project = solution.GetProject(projectData.Key)
            where project != null
            select new ProjectSource(project, projectData.ToImmutableArray()));

        return sources.ToImmutable();
    }
}
