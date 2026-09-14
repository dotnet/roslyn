// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Logging;

[ExportCSharpVisualBasicStatelessLspService(typeof(WorkspaceStructureLogHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceStructureLogHandler() : ILspServiceRequestHandler<WorkspaceStructureLogParams, WorkspaceStructureLogResponse>
{
    internal const string MethodName = "workspace/_roslyn_workspaceStructureLog";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<WorkspaceStructureLogResponse> HandleRequestAsync(WorkspaceStructureLogParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var solution = context.Solution;
        Contract.ThrowIfNull(solution);

        var progressManager = context.GetRequiredLspService<WorkDoneProgressManager>();
        await using var progressReporter = await progressManager.CreateWorkDoneProgressAsync(
            reportProgressToClient: true,
            title: LanguageServerResources.Workspace_structure_log,
            startMessage: LanguageServerResources.Generating_workspace_structure_log,
            endMessage: LanguageServerResources.Workspace_structure_log_generated,
            clientCanCancel: true,
            serverCancellationToken: cancellationToken).ConfigureAwait(false);

        var progress = new SynchronousProgress<(int current, int total)>(value =>
        {
            progressReporter.Report(new WorkDoneProgressReport
            {
                Percentage = value.total == 0 ? 100 : value.current * 100 / value.total,
            });
        });

        var document = await new WorkspaceStructureLogger().BuildWorkspaceStructureAsync(
            solution,
            solution.WorkspaceKind,
            progress,
            progressReporter.CancellationToken).ConfigureAwait(false);

        var tempPath = Path.Combine(Path.GetTempPath(), $"RoslynWorkspaceLog-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.xml");
        using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            document.Save(stream);

        return new WorkspaceStructureLogResponse(ProtocolConversions.CreateAbsoluteDocumentUri(tempPath));
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
