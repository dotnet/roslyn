// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SQLite.Interop;
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

        public async Task<RemoteServiceConnection?> StartEditSessionAsync(IDiagnosticAnalyzerService diagnosticService, IRemoteEditAndContinueService.IStartEditSessionCallback callback, CancellationToken cancellationToken)
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

            var documentsToReanalyze = await connection.RunRemoteAsync<ImmutableArray<DocumentId>>(
                nameof(IRemoteEditAndContinueService.StartEditSessionAsync),
                solution: null,
                Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);

            // clear all reported run mode diagnostics:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);

            return connection;
        }

        public async Task EndEditSessionAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            var documentsToReanalyze = await client.RunRemoteAsync<ImmutableArray<DocumentId>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.EndEditSessionAsync),
                solution: null,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);
        }

        public async Task EndDebuggingSessionAsync(EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            var documentsToReanalyze = await client.RunRemoteAsync<ImmutableArray<DocumentId>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.EndDebuggingSessionAsync),
                solution: null,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // clear diagnostics reported during run mode:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);
        }

        public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var diagnosticData = await client.RunRemoteAsync<ImmutableArray<DiagnosticData>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetDocumentDiagnosticsAsync),
                solution,
                new object[] { document.Id },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);
            foreach (var data in diagnosticData)
            {
                result.Add(await data.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

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

        public async Task<(SolutionUpdateStatus, ImmutableArray<Deltas>)> EmitSolutionUpdateAsync(EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return (SolutionUpdateStatus.Blocked, ImmutableArray<Deltas>.Empty);
            }

            var (status, deltas, diagnosticsByProject) = await client.RunRemoteAsync<(SolutionUpdateStatus, ImmutableArray<Deltas.Data>, ImmutableArray<DiagnosticData>)>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.EmitSolutionUpdateAsync),
                solution,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(_workspace, solution, diagnosticsByProject);

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
