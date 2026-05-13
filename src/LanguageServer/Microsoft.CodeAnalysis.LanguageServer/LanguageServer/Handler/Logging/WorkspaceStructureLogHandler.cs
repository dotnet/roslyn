// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer.Handler.Logging;

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
        Contract.ThrowIfNull(context.Solution);

        var document = await WorkspaceStructureLogger.BuildWorkspaceStructureAsync(
            context.Solution,
            context.Solution.WorkspaceKind,
            progress: null,
            createAdditionalProjectElementsAsync: null,
            cancellationToken).ConfigureAwait(false);

        var tempPath = Path.Combine(Path.GetTempPath(), $"RoslynWorkspaceLog-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xml");
        document.Save(tempPath);

        return new WorkspaceStructureLogResponse(ProtocolConversions.CreateAbsoluteDocumentUri(tempPath));
    }
}
