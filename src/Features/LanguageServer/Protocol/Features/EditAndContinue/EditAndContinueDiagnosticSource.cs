// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class EditAndContinueDiagnosticSource
{
    private sealed class ClosedDocumentSource(TextDocument document, ImmutableArray<DiagnosticData> diagnostics) : AbstractWorkspaceDocumentDiagnosticSource(document)
    {
        public override bool IsLiveSource()
            => true;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken)
            => Task.FromResult(diagnostics);
    }

    private sealed class ProjectSource(Project project, ImmutableArray<DiagnosticData> diagnostics) : AbstractProjectDiagnosticSource(project)
    {
        public override bool IsLiveSource()
            => true;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, CancellationToken cancellationToken)
            => Task.FromResult(diagnostics);
    }

    public static async ValueTask AddWorkspaceDiagnosticSourcesAsync(ArrayBuilder<IDiagnosticSource> sources, Solution solution, Func<Document, bool> isDocumentOpen, CancellationToken cancellationToken)
    {
        if (solution.Services.GetRequiredService<IEditAndContinueWorkspaceService>().SessionTracker is not { IsSessionActive: true } sessionStateTracker)
        {
            return;
        }

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
    }

    public static async Task<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(Document designTimeDocument, CancellationToken cancellationToken)
    {
        var designTimeSolution = designTimeDocument.Project.Solution;
        var services = designTimeSolution.Services;

        // avoid creating and synchronizing compile-time solution if Hot Reload/EnC session is not active
        if (services.GetRequiredService<IEditAndContinueWorkspaceService>().SessionTracker is not { IsSessionActive: true } sessionStateTracker)
        {
            return [];
        }

        var applyDiagnostics = sessionStateTracker.ApplyChangesDiagnostics.WhereAsArray(static (data, id) => data.DocumentId == id, designTimeDocument.Id);

        var compileTimeSolution = services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(designTimeSolution);

        var compileTimeDocument = await CompileTimeSolutionProvider.TryGetCompileTimeDocumentAsync(designTimeDocument, compileTimeSolution, cancellationToken).ConfigureAwait(false);
        if (compileTimeDocument == null)
        {
            return applyDiagnostics;
        }

        // EnC services should never be called on a design-time solution.

        var proxy = new RemoteEditAndContinueServiceProxy(services);
        var spanLocator = services.GetService<IActiveStatementSpanLocator>();

        var activeStatementSpanProvider = (spanLocator != null)
            ? new ActiveStatementSpanProvider((documentId, filePath, cancellationToken) => spanLocator.GetSpansAsync(compileTimeSolution, documentId, filePath, cancellationToken))
            : static (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        var rudeEditDiagnostics = await proxy.GetDocumentDiagnosticsAsync(compileTimeDocument, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

        // TODO: remove
        // We pretend the diagnostic is in the original document, but use the mapped line span.
        // Razor will ignore the column (which will be off because #line directives can't currently map columns) and only use the line number.
        rudeEditDiagnostics = rudeEditDiagnostics.SelectAsArray(data => (designTimeDocument != compileTimeDocument) ? RemapLocation(designTimeDocument, data) : data);

        return applyDiagnostics.AddRange(rudeEditDiagnostics);
    }

    private static DiagnosticData RemapLocation(Document designTimeDocument, DiagnosticData data)
    {
        Debug.Assert(data.DataLocation != null);
        Debug.Assert(designTimeDocument.FilePath != null);

        // If the location in the generated document is in a scope of user-visible #line mapping use the mapped span,
        // otherwise (if it's hidden) display the diagnostic at the start of the file.
        var span = data.DataLocation.UnmappedFileSpan != data.DataLocation.MappedFileSpan ? data.DataLocation.MappedFileSpan.Span : default;
        var location = new DiagnosticDataLocation(new FileLinePositionSpan(designTimeDocument.FilePath, span));

        return data.WithLocations(location, additionalLocations: []);
    }
}
