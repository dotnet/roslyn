// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IEditAndContinueManagedModuleUpdateProvider)), Shared]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class VisualStudioManagedModuleUpdateProvider : IEditAndContinueManagedModuleUpdateProvider
    {
        private readonly Workspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioManagedModuleUpdateProvider(VisualStudioWorkspace workspace)
            => _workspace = workspace;

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
        /// </summary>
        public async Task<bool> HasChangesAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _workspace.CurrentSolution;

                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return true;
                }

                var result = await client.RunRemoteAsync<bool>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.HasChangesAsync),
                    solution,
                    new object[] { sourceFilePath },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return true;
            }
        }

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = _workspace.CurrentSolution;

                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<DkmManagedModuleUpdate>.Empty.ToReadOnlyCollection());
                }

                var (summary, deltas) = await client.RunRemoteAsync<(SolutionUpdateStatus, ImmutableArray<Deltas>)>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.EmitSolutionUpdateAsync),
                    solution,
                    Array.Empty<object>(),
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return new ManagedModuleUpdates(summary.ToModuleUpdateStatus(), deltas.SelectAsArray(ModuleUtilities.ToModuleUpdate).ToReadOnlyCollection());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<DkmManagedModuleUpdate>.Empty.ToReadOnlyCollection());
            }
        }

#pragma warning disable VSTHRD102 // TODO: Implement internal logic asynchronously
        public void CommitUpdates()
            => ThreadHelper.JoinableTaskFactory.Run(() => CommitUpdatesAsync(CancellationToken.None));

        public void DiscardUpdates()
            => ThreadHelper.JoinableTaskFactory.Run(() => DiscardUpdatesAsync(CancellationToken.None));
#pragma warning restore

        private async Task CommitUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return;
                }

                await client.RunRemoteAsync(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.CommitUpdateAsync),
                    solution: null,
                    Array.Empty<object>(),
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }

        private async Task DiscardUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return;
                }

                await client.RunRemoteAsync(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.DiscardUpdatesAsync),
                    solution: null,
                    Array.Empty<object>(),
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }
    }
}
