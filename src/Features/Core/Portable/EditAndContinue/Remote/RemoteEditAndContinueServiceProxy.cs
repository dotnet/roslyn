// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public Workspace Workspace => _workspace;

        private sealed class DocumentActiveStatementProviderCallback : IRemoteEditAndContinueService.IDocumentActiveStatementProviderCallback
        {
            private readonly DocumentActiveStatementSpanProvider _provider;

            public DocumentActiveStatementProviderCallback(DocumentActiveStatementSpanProvider provider)
                => _provider = provider;

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<ImmutableArray<TextSpan>> GetSpansAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _provider(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }
        }

        private sealed class SolutionActiveStatementProviderCallback : IRemoteEditAndContinueService.ISolutionActiveStatementProviderCallback
        {
            private readonly SolutionActiveStatementSpanProvider _provider;

            public SolutionActiveStatementProviderCallback(SolutionActiveStatementSpanProvider provider)
                => _provider = provider;

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<ImmutableArray<TextSpan>> GetSpansAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                try
                {
                    return await _provider(documentId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }
        }

        private IEditAndContinueWorkspaceService GetLocalService()
            => _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        public async Task StartDebuggingSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().StartDebuggingSession(solution);
                return;
            }

            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.StartDebuggingSessionAsync),
                solution,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<IDisposable?> StartEditSessionAsync(
            IDiagnosticAnalyzerService diagnosticService,
            ActiveStatementProvider activeStatementProvider,
            IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider,
            CancellationToken cancellationToken)
        {
            IDisposable result;
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().StartEditSession(activeStatementProvider, debuggeeModuleMetadataProvider, out documentsToReanalyze);
                result = LocalConnection.Instance;
            }
            else
            {
                // need to keep the providers alive until the edit session ends:
                var connection = await client.CreateConnectionAsync(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    callbackTarget: new StartEditSessionCallback(activeStatementProvider, debuggeeModuleMetadataProvider),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = await connection.RunRemoteAsync<ImmutableArray<DocumentId>>(
                    nameof(IRemoteEditAndContinueService.StartEditSessionAsync),
                    solution: null,
                    Array.Empty<object>(),
                    cancellationToken).ConfigureAwait(false);

                result = connection;
            }

            // clear all reported run mode diagnostics:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);

            return result;
        }

        private sealed class LocalConnection : IDisposable
        {
            public static readonly LocalConnection Instance = new LocalConnection();

            public void Dispose()
            {
            }
        }

        private sealed class StartEditSessionCallback : IRemoteEditAndContinueService.IStartEditSessionCallback
        {
            private readonly ActiveStatementProvider _activeStatementProvider;
            private readonly IDebuggeeModuleMetadataProvider _managedModuleInfoProvider;

            public StartEditSessionCallback(ActiveStatementProvider activeStatementProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider)
            {
                _activeStatementProvider = activeStatementProvider;
                _managedModuleInfoProvider = debuggeeModuleMetadataProvider;
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<ImmutableArray<ActiveStatementDebugInfo.Data>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                try
                {
                    var infos = await _activeStatementProvider(cancellationToken).ConfigureAwait(false);
                    return infos.SelectAsArray(info => info.Serialize());
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<ActiveStatementDebugInfo.Data>.Empty;
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    return await _managedModuleInfoProvider.GetEncAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // TODO: better error code?
                    return (-1, e.Message);
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    await _managedModuleInfoProvider.PrepareModuleForUpdateAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    // nop
                }
            }
        }

        public async Task EndEditSessionAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndEditSession(out documentsToReanalyze);
            }
            else
            {
                documentsToReanalyze = await client.RunRemoteAsync<ImmutableArray<DocumentId>>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.EndEditSessionAsync),
                    solution: null,
                    Array.Empty<object>(),
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);
        }

        public async Task EndDebuggingSessionAsync(EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndDebuggingSession(out documentsToReanalyze);
            }
            else
            {
                documentsToReanalyze = await client.RunRemoteAsync<ImmutableArray<DocumentId>>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.EndDebuggingSessionAsync),
                    solution: null,
                    Array.Empty<object>(),
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // clear diagnostics reported during run mode:
            diagnosticService.Reanalyze(_workspace, documentIds: documentsToReanalyze);
        }

        public async Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var diagnosticData = await client.RunRemoteAsync<ImmutableArray<DiagnosticData>>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetDocumentDiagnosticsAsync),
                document.Project.Solution,
                new object[] { document.Id },
                callbackTarget: new DocumentActiveStatementProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);
            foreach (var data in diagnosticData)
            {
                result.Add(await data.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        public async Task<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string sourceFilePath, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.RunRemoteAsync<bool>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.HasChangesAsync),
                solution,
                new object[] { sourceFilePath },
                callbackTarget: new SolutionActiveStatementProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task<(SolutionUpdateStatus, ImmutableArray<Deltas>)> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, CancellationToken cancellationToken)
        {
            SolutionUpdateStatus status;
            ImmutableArray<Deltas> deltas;
            ImmutableArray<DiagnosticData> diagnosticsByProject;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                (status, deltas, diagnosticsByProject) = await GetLocalService().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                ImmutableArray<Deltas.Data> deltasData;

                (status, deltasData, diagnosticsByProject) = await client.RunRemoteAsync<(SolutionUpdateStatus, ImmutableArray<Deltas.Data>, ImmutableArray<DiagnosticData>)>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.EmitSolutionUpdateAsync),
                    solution,
                    Array.Empty<object>(),
                    callbackTarget: new SolutionActiveStatementProviderCallback(activeStatementSpanProvider),
                    cancellationToken).ConfigureAwait(false);

                deltas = deltasData.SelectAsArray(d => d.Deserialize());
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(_workspace, solution, diagnosticsByProject);

            return (status, deltas);
        }

        public async Task CommitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().CommitSolutionUpdate();
                return;
            }

            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.CommitSolutionUpdateAsync),
                solution: null,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().DiscardSolutionUpdate();
                return;
            }

            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.DiscardSolutionUpdateAsync),
                solution: null,
                Array.Empty<object>(),
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var instructionId = new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset);
                return await GetLocalService().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
            }

            return await client.RunRemoteAsync<LinePositionSpan?>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetCurrentActiveStatementPositionAsync),
                solution: solution,
                new object[] { moduleId, methodToken, methodVersion, ilOffset },
                callbackTarget: new SolutionActiveStatementProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var instructionId = new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset);
                return await GetLocalService().IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken).ConfigureAwait(false);
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
                return await GetLocalService().GetBaseActiveStatementSpansAsync(documentIds, cancellationToken).ConfigureAwait(false);
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

        public async Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.RunRemoteAsync<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>?>(
                WellKnownServiceHubService.RemoteEditAndContinueService,
                nameof(IRemoteEditAndContinueService.GetAdjustedActiveStatementSpansAsync),
                document.Project.Solution,
                new object[] { document.Id },
                callbackTarget: new DocumentActiveStatementProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            // JSON-RPC does not support serialization of default ImmutableArray
            return result ?? default;
        }

        public async Task OnSourceFileUpdatedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().OnSourceFileUpdated(documentId);
                return;
            }

            await client.RunRemoteAsync(
               WellKnownServiceHubService.RemoteEditAndContinueService,
               nameof(IRemoteEditAndContinueService.OnSourceFileUpdatedAsync),
               solution: null,
               new object[] { documentId },
               callbackTarget: null,
               cancellationToken).ConfigureAwait(false);
        }
    }
}
