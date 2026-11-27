// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

/// <summary>
/// Refresh queue for source generated documents. Detects when source generator output may have changed
/// (via execution version or dependent version checks) and sends per-URI refresh notifications to the client
/// using the LSP 3.18 <c>workspace/textDocumentContent/refresh</c> mechanism.
/// </summary>
internal sealed class SourceGeneratorRefreshQueue(
    IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
    LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
    LspWorkspaceManager lspWorkspaceManager,
    IClientLanguageServerManager notificationManager)
    : AbstractTextDocumentContentRefreshQueue(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
{
    protected override string Scheme => SourceGeneratedDocumentUri.Scheme;

    protected override async Task<bool> ShouldEnqueueRefreshNotificationAsync(WorkspaceChangeEventArgs e, CancellationToken cancellationToken)
    {
        var projectId = e.ProjectId ?? e.DocumentId?.ProjectId;
        if (projectId is not null)
        {
            // We have a specific changed project - do some additional checks to see if
            // source generators possibly changed.

            var oldProject = e.OldSolution.GetProject(projectId);
            var newProject = e.NewSolution.GetProject(projectId);

            // If the project has been added/removed, we need to update the generated files.
            if (oldProject is null || newProject is null)
            {
                return true;
            }

            // Trivial check.  see if the SG version of these projects changed.  If so, we definitely want to update generated files.
            if (e.OldSolution.GetSourceGeneratorExecutionVersion(projectId) !=
                e.NewSolution.GetSourceGeneratorExecutionVersion(projectId))
            {
                return true;
            }

            var configuration = e.NewSolution.Services.GetRequiredService<IWorkspaceConfigurationService>().Options;
            // When running in balanced mode, we do not need to refresh generated documents if only a document changed and the execution version did not.
            if (e.Kind is WorkspaceChangeKind.DocumentChanged &&
                configuration.SourceGeneratorExecution == SourceGeneratorExecutionPreference.Balanced)
            {
                return false;
            }

            // More expensive check - see if the dependent versions are different.
            if (await oldProject.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false) !=
                await newProject.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }
        else
        {
            // We don't have a specific project change - if this is a solution change we need to queue a refresh anyway.
            if (e.Kind is WorkspaceChangeKind.SolutionChanged or WorkspaceChangeKind.SolutionAdded or WorkspaceChangeKind.SolutionRemoved or WorkspaceChangeKind.SolutionReloaded or WorkspaceChangeKind.SolutionCleared)
            {
                return true;
            }
        }

        return false;
    }
}
