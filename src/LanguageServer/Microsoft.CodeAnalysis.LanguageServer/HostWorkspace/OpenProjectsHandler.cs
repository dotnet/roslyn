// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OpenProjectHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OpenProjectHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OpenProjectHandler(
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            lspServices.GetRequiredService<WorkDoneProgressManager>());
}

[Method(OpenProjectName)]
internal sealed class OpenProjectHandler : ILspService, ILspServiceNotificationHandler<OpenProjectHandler.NotificationParams>
{
    internal const string OpenProjectName = "project/open";

    private readonly LanguageServerProjectSystem _projectSystem;
    private readonly WorkDoneProgressManager _workDoneProgressManager;

    public OpenProjectHandler(LanguageServerProjectSystem projectSystem, WorkDoneProgressManager workDoneProgressManager)
    {
        _projectSystem = projectSystem;
        _workDoneProgressManager = workDoneProgressManager;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    async Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var projectsLength = request.Projects.Length;
        var loadingMessage = string.Format(LanguageServerResources.Loading_0_projects, projectsLength);
        await using var progressReporter = await _workDoneProgressManager.CreateWorkDoneProgressAsync(
            reportProgressToClient: true,
            title: loadingMessage,
            startMessage: loadingMessage,
            endMessage: string.Format(LanguageServerResources.Loaded_0_projects, projectsLength),
            clientCanCancel: false,
            serverCancellationToken: cancellationToken);

        var projectPaths = request.Projects.SelectAsArray(p => p.GetDocumentFilePathFromUri());
        await _projectSystem.OpenProjectsAsync(projectPaths, progressReporter);
    }

    internal sealed class NotificationParams
    {
        [JsonPropertyName("projects")]
        public required DocumentUri[] Projects { get; set; }
    }
}
