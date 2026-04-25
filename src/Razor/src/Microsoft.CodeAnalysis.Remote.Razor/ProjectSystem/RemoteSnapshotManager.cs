// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

[Shared]
[Export(typeof(RemoteSnapshotManager))]
[method: ImportingConstructor]
internal sealed class RemoteSnapshotManager(IFilePathService filePathService, ITelemetryReporter telemetryReporter)
{
    private static readonly ConditionalWeakTable<Solution, RemoteSolutionSnapshot> s_solutionToSnapshotMap = new();

    public IFilePathService FilePathService { get; } = filePathService;
    public ITelemetryReporter TelemetryReporter { get; } = telemetryReporter;

    public RemoteSolutionSnapshot GetSnapshot(Solution solution)
    {
        return s_solutionToSnapshotMap.GetValue(solution, s => new RemoteSolutionSnapshot(s, this));
    }

    public RemoteProjectSnapshot GetSnapshot(Project project)
    {
        return GetSnapshot(project.Solution).GetProject(project);
    }

    public RemoteDocumentSnapshot GetSnapshot(TextDocument document)
    {
        return GetSnapshot(document.Project).GetDocument(document);
    }

    public Task<RazorCodeDocument?> TryGetRazorCodeDocumentAsync(Solution solution, Uri generatedDocumentUri, CancellationToken cancellationToken)
    {
        if (!solution.TryGetSourceGeneratedDocumentIdentity(generatedDocumentUri, out var identity) ||
            !solution.TryGetProject(identity.DocumentId.ProjectId, out var project))
        {
            return SpecializedTasks.Null<RazorCodeDocument>();
        }

        var snapshot = GetSnapshot(project);
        return snapshot.TryGetCodeDocumentForGeneratedDocumentAsync(identity, cancellationToken);
    }
}
