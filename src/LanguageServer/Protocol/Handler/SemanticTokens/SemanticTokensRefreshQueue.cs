// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

/// <summary>
/// Batches requests to refresh the semantic tokens to optimize user experience.
/// </summary>
internal class SemanticTokensRefreshQueue : AbstractRefreshQueue
{
    /// <summary>
    /// Lock over the mutable state that follows.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Mapping from project id to the project-cone-checksum for it we were at when the project for it had its
    /// compilation produced on the oop server.
    /// </summary>
    private readonly Dictionary<ProjectId, Checksum> _projectIdToLastComputedChecksum = [];

    public SemanticTokensRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager) : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
    {
    }

    public async Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken)
    {
        // Determine the checksum for this project cone.  Note: this should be fast in practice because this is the
        // same project-cone-checksum we used to even call into OOP above when we computed semantic tokens.
        var projectChecksum = await project.Solution.CompilationState.GetChecksumAsync(project.Id, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            // If this checksum is the same as the last computed result, no need to continue, we would not produce a
            // different compilation.
            if (_projectIdToLastComputedChecksum.TryGetValue(project.Id, out var lastChecksum) && lastChecksum == projectChecksum)
                return;

            // keep track of this checksum.  That way we don't get into a loop where we send a refresh notification,
            // then we get called back into, causing us to compute the compilation, causing us to send the refresh
            // notification, etc. etc.
            _projectIdToLastComputedChecksum[project.Id] = projectChecksum;

        }

        EnqueueRefreshNotification(documentUri: null);
    }

    protected override void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        Uri? documentUri = null;

        if (e.DocumentId is not null)
        {
            // We enqueue the URI since there's a chance the client is already tracking the
            // document, in which case we don't need to send a refresh notification.
            // We perform the actual check when processing the batch to ensure we have the
            // most up-to-date list of tracked documents.
            if (e.Kind is WorkspaceChangeKind.DocumentChanged)
            {
                var document = e.NewSolution.GetRequiredDocument(e.DocumentId);
                documentUri = document.GetURI();
            }
            else if (e.Kind is WorkspaceChangeKind.AdditionalDocumentChanged)
            {
                var document = e.NewSolution.GetRequiredAdditionalDocument(e.DocumentId);

                // Changes to files with certain extensions (eg: razor) shouldn't trigger semantic a token refresh
                if (DisallowsAdditionalDocumentChangedRefreshes(document.FilePath))
                    return;
            }
            else if (e.Kind is WorkspaceChangeKind.DocumentReloaded)
            {
                var newDocument = e.NewSolution.GetRequiredDocument(e.DocumentId);
                var oldDocument = e.OldSolution.GetDocument(e.DocumentId);

                // If the document's attributes haven't changed, then use the document's URI for
                //   the call to EnqueueSemanticTokenRefreshNotification which will enable the
                //   tracking check before sending the WorkspaceSemanticTokensRefreshName message.
                if (oldDocument?.State.Attributes.Checksum == newDocument.State.Attributes.Checksum)
                    documentUri = newDocument.GetURI();
            }
        }

        EnqueueRefreshNotification(documentUri);
    }

    // Duplicated from Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.LoadedProject.TreatAsIsDynamicFile
    private static bool DisallowsAdditionalDocumentChangedRefreshes(string? filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension is ".cshtml" or ".razor";
    }

    protected override string GetFeatureAttribute() => FeatureAttribute.Classification;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities) => clientCapabilities.Workspace?.SemanticTokens?.RefreshSupport;

    protected override string GetWorkspaceRefreshName() => Methods.WorkspaceSemanticTokensRefreshName;
}
