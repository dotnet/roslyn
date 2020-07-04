// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class RemoteEditAndContinueServiceProxy : IActiveStatementSpanProvider
    {
        private readonly Workspace _workspace;

        public RemoteEditAndContinueServiceProxy(Workspace workspace)
        {
            _workspace = workspace;
        }

        public async Task StartDebuggingSessionAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.StartDebuggingSessionAsync),
                _workspace.CurrentSolution,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<RemoteServiceConnection?> StartEditSessionAsync(IRemoteEditAndContinueService.IStartEditSessionCallback callback, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            // need to keep the providers alive until the edit session ends:
            var connection = await client.CreateConnectionAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                callbackTarget: callback,
                cancellationToken).ConfigureAwait(false);

            await connection.RunRemoteAsync(
                nameof(IRemoteEditAndContinueService.StartEditSessionAsync),
                solution: null,
                Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);

            return connection;
        }

        public Task EndEditSessionAsync(CancellationToken cancellationToken)
            => NotifyRemoteServiceAsync(nameof(IRemoteEditAndContinueService.EndEditSessionAsync), cancellationToken);

        public Task EndDebuggingSessionAsync(CancellationToken cancellationToken)
            => NotifyRemoteServiceAsync(nameof(IRemoteEditAndContinueService.EndDebuggingSessionAsync), cancellationToken);

        public async Task<bool> HasChangesAsync(string sourceFilePath, CancellationToken cancellationToken)
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

        public async Task<(SolutionUpdateStatus, ImmutableArray<Deltas>)> EmitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return (SolutionUpdateStatus.Blocked, ImmutableArray<Deltas>.Empty);
            }

            var (status, deltas) = await client.RunRemoteAsync<(SolutionUpdateStatus, ImmutableArray<Deltas.Data>)>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.EmitSolutionUpdateAsync),
                solution,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            return (status, deltas.SelectAsArray(d => d.Deserialize()));
        }

        public async Task CommitUpdatesAsync(CancellationToken cancellationToken)
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

        public async Task DiscardUpdatesAsync(CancellationToken cancellationToken)
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

        private async Task NotifyRemoteServiceAsync(string targetName, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                targetName,
                solution: null,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.RunRemoteAsync<LinePositionSpan?>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetCurrentActiveStatementPositionAsync),
                solution: _workspace.CurrentSolution,
                new object[] { moduleId, methodToken, methodVersion, ilOffset },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.RunRemoteAsync<bool?>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.IsActiveStatementInExceptionRegionAsync),
                solution: null,
                new object[] { moduleId, methodToken, methodVersion, ilOffset },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return default;
            }

            var result = await client.RunRemoteAsync<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetBaseActiveStatementSpansAsync),
                solution: null,
                new object[] { documentIds },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return default;
            }

            var result = await client.RunRemoteAsync<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetDocumentActiveStatementSpansAsync),
                document.Project.Solution,
                new object[] { document.Id },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
