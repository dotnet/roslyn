// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Handles a request from the client to refresh source generators.
/// No specific generators are refreshed; rather, all generators are refreshed in all registered workspaces.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(WorkspaceRefreshSourceGeneratorsHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class WorkspaceRefreshSourceGeneratorsHandler(LspWorkspaceRegistrationService workspaceRegistrationService) : ILspServiceNotificationHandler<RefreshSourceGeneratorsParams>
{
    public const string MethodName = "workspace/_roslyn_refreshSourceGenerators";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => false;

    public Task HandleNotificationAsync(RefreshSourceGeneratorsParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        foreach (var workspace in workspaceRegistrationService.GetAllRegistrations())
        {
            workspace.EnqueueUpdateSourceGeneratorVersion(projectId: null, request.ForceRegeneration);
        }

        return Task.CompletedTask;
    }
}

internal record RefreshSourceGeneratorsParams(
    [property: JsonPropertyName("forceRegeneration")] bool ForceRegeneration
);
