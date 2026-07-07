// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OpenSolutionHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OpenSolutionHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OpenSolutionHandler(
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            lspServices.GetRequiredService<WorkDoneProgressManager>());
}

[Method(OpenSolutionName)]
internal sealed class OpenSolutionHandler : ILspService, ILspServiceNotificationHandler<OpenSolutionHandler.NotificationParams>
{
    internal const string OpenSolutionName = "solution/open";

    private readonly LanguageServerProjectSystem _projectSystem;
    private readonly WorkDoneProgressManager _workDoneProgressManager;

    public OpenSolutionHandler(LanguageServerProjectSystem projectSystem, WorkDoneProgressManager workDoneProgressManager)
    {
        _projectSystem = projectSystem;
        _workDoneProgressManager = workDoneProgressManager;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    async Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var solutionPath = request.Solution.GetDocumentFilePathFromUri();

        var loadingMessage = string.Format(LanguageServerResources.Loading_0, solutionPath);
        await using var progressReporter = await _workDoneProgressManager.CreateWorkDoneProgressAsync(
            reportProgressToClient: true,
            title: loadingMessage,
            startMessage: loadingMessage,
            endMessage: string.Format(LanguageServerResources.Loaded_0, solutionPath),
            clientCanCancel: false,
            serverCancellationToken: cancellationToken);

        await _projectSystem.OpenSolutionAsync(solutionPath, progressReporter);
    }

    internal sealed class NotificationParams
    {
        [JsonPropertyName("solution")]
        public required DocumentUri Solution { get; set; }
    }
}
