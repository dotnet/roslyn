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

[ExportCSharpVisualBasicLspService(typeof(OpenProjectHandler)), Shared(LspServiceComposition.SharingBoundary)]
[Method(OpenProjectName)]
internal sealed class OpenProjectHandler : ILspService, ILspServiceNotificationHandler<OpenProjectHandler.NotificationParams>
{
    internal const string OpenProjectName = "project/open";

    private readonly LanguageServerProjectSystem _projectSystem;
    private readonly WorkDoneProgressManager _workDoneProgressManager;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OpenProjectHandler(
        LspService<LanguageServerProjectSystem> projectSystem,
        LspService<WorkDoneProgressManager> workDoneProgressManager)
    {
        var projectSystemValue = projectSystem.Value;
        var workDoneProgressManagerValue = workDoneProgressManager.Value;
        _projectSystem = projectSystemValue;
        _workDoneProgressManager = workDoneProgressManagerValue;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    async Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var projectsLength = request.Projects.Length;
        var loadingMessage = LanguageServerResources.Loading_projects;
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
