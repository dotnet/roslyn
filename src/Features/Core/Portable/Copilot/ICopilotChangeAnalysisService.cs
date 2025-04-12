// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotChangeAnalysisService : IWorkspaceService
{
    /// <summary>
    /// Kicks of work to analyze a change that copilot suggested making to a document. <paramref name="document"/> is
    /// the state of the document prior to the edits, and <paramref name="changes"/> are the changes Copilot wants to
    /// make to it.  <paramref name="changes"/> must be sorted and normalized before calling this.
    /// </summary>
    Task AnalyzeChangeAsync(Document document, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
}

/// <summary>Remote version of <see cref="ICopilotChangeAnalysisService"/></summary>
internal interface IRemoteCopilotChangeAnalysisService : IWorkspaceService
{
    /// <inheritdoc cref="ICopilotChangeAnalysisService.AnalyzeChangeAsync"/>
    ValueTask AnalyzeChangeAsync(Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
}

[ExportWorkspaceServiceFactory(typeof(ICopilotChangeAnalysisService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultCopilotChangeAnalysisServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultCopilotChangeAnalysisService(workspaceServices);

    private sealed class DefaultCopilotChangeAnalysisService(
        HostWorkspaceServices workspaceServices) : ICopilotChangeAnalysisService
    {
        public async Task AnalyzeChangeAsync(
            Document document,
            ImmutableArray<TextChange> changes,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(!changes.IsSorted(static (c1, c2) => c1.Span.Start - c2.Span.Start), "'changes' was not sorted.");
            Contract.ThrowIfTrue(new NormalizedTextSpanCollection(changes.Select(c => c.Span)).Count != changes.Length, "'changes' was not normalized.");
            Contract.ThrowIfTrue(document.Project.Solution.Workspace != workspaceServices.Workspace);

            var client = await RemoteHostClient.TryGetClientAsync(
                workspaceServices.Workspace, cancellationToken).ConfigureAwait(false);

            if (client != null)
            {
                await client.TryInvokeAsync<IRemoteCopilotChangeAnalysisService>(
                    // Don't need to sync the entire solution over.  Just the cone of projects this document it contained within.
                    document.Project,
                    (service, checksum, cancellationToken) => service.AnalyzeChangeAsync(checksum, document.Id, changes, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AnalyzeChangeInCurrentProcessAsync(document, changes, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task AnalyzeChangeInCurrentProcessAsync(
            Document document,
            ImmutableArray<TextChange> changes,
            CancellationToken cancellationToken)
        {
            return Task.WhenAll(AnalyzeChangedRegionsAsync(), AnalyzeUnchangedRegionsAsync());

            // Not yet implemented.  Flesh this out if we think there is value in looking at the rest of the document
            // (or just the regions around the copilot edits) to see how the actual edits impacted them.
            Task AnalyzeUnchangedRegionsAsync()
                => Task.CompletedTask;

            async Task AnalyzeChangedRegionsAsync()
            {

            }
        }
    }
}
