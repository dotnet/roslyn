// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct RemoteEditAndContinueServiceProxy : IActiveStatementSpanProvider
    {
        private sealed class Callback : IRemoteEditAndContinueService.ICallback
        {
            public static readonly Callback Unused = new();

            private readonly ActiveStatementProvider? _activeStatementProvider;
            private readonly IDebuggeeModuleMetadataProvider? _managedModuleInfoProvider;

            private readonly DocumentActiveStatementSpanProvider? _documentProvider;
            private readonly SolutionActiveStatementSpanProvider? _solutionProvider;

            private Callback()
            {
            }

            public Callback(DocumentActiveStatementSpanProvider documentProvider)
                => _documentProvider = documentProvider;

            public Callback(SolutionActiveStatementSpanProvider solutionProvider)
                => _solutionProvider = solutionProvider;

            public Callback(ActiveStatementProvider activeStatementProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider)
            {
                _activeStatementProvider = activeStatementProvider;
                _managedModuleInfoProvider = debuggeeModuleMetadataProvider;
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_documentProvider);

                try
                {
                    return await _documentProvider(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_solutionProvider);

                try
                {
                    return await _solutionProvider(documentId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_activeStatementProvider);

                try
                {
                    return await _activeStatementProvider(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
                {
                    return ImmutableArray<ActiveStatementDebugInfo>.Empty;
                }
            }

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_managedModuleInfoProvider);

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
            public async ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(_managedModuleInfoProvider);

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

        public readonly Workspace Workspace;

        public RemoteEditAndContinueServiceProxy(Workspace workspace)
        {
            Workspace = workspace;
        }

        private IEditAndContinueWorkspaceService GetLocalService()
            => Workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        public async ValueTask StartDebuggingSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().StartDebuggingSession(solution);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                solution,
                (service, solutionInfo, cancellationToken) => service.StartDebuggingSessionAsync(solutionInfo, cancellationToken),
                callbackTarget: Callback.Unused,
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<IDisposable?> StartEditSessionAsync(
            IDiagnosticAnalyzerService diagnosticService,
            ActiveStatementProvider activeStatementProvider,
            IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider,
            CancellationToken cancellationToken)
        {
            IDisposable result;
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().StartEditSession(activeStatementProvider, debuggeeModuleMetadataProvider, out documentsToReanalyze);
                result = LocalConnection.Instance;
            }
            else
            {
                // need to keep the providers alive until the edit session ends:
                var connection = await client.CreateConnectionAsync<IRemoteEditAndContinueService>(
                    callbackTarget: new Callback(activeStatementProvider, debuggeeModuleMetadataProvider),
                    cancellationToken).ConfigureAwait(false);

                var documentsToReanalyzeOpt = await connection.TryInvokeAsync(
                    (service, cancellationToken) => service.StartEditSessionAsync(cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
                result = connection;
            }

            // clear all reported run mode diagnostics:
            diagnosticService.Reanalyze(Workspace, documentIds: documentsToReanalyze);

            return result;
        }

        private sealed class LocalConnection : IDisposable
        {
            public static readonly LocalConnection Instance = new LocalConnection();

            public void Dispose()
            {
            }
        }

        public async ValueTask EndEditSessionAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndEditSession(out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.EndEditSessionAsync(cancellationToken),
                    callbackTarget: Callback.Unused,
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(Workspace, documentIds: documentsToReanalyze);
        }

        public async ValueTask EndDebuggingSessionAsync(EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndDebuggingSession(out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancellationToken) => service.EndDebuggingSessionAsync(cancellationToken),
                    callbackTarget: Callback.Unused,
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // clear diagnostics reported during run mode:
            diagnosticService.Reanalyze(Workspace, documentIds: documentsToReanalyze);
        }

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var diagnosticData = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DiagnosticData>>(
                document.Project.Solution,
                (service, solutionInfo, cancellationToken) => service.GetDocumentDiagnosticsAsync(solutionInfo, document.Id, cancellationToken),
                callbackTarget: new Callback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            if (!diagnosticData.HasValue)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);
            foreach (var data in diagnosticData.Value)
            {
                result.Add(await data.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutable();
        }

        public async ValueTask<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string sourceFilePath, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool>(
                solution,
                (service, solutionInfo, cancellationToken) => service.HasChangesAsync(solutionInfo, sourceFilePath, cancellationToken),
                callbackTarget: new Callback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : true;
        }

        public async ValueTask<(SolutionUpdateStatus, ImmutableArray<Deltas>)> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, CancellationToken cancellationToken)
        {
            SolutionUpdateStatus status;
            ImmutableArray<Deltas> deltas;
            ImmutableArray<DiagnosticData> diagnosticsByProject;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                (status, deltas, diagnosticsByProject) = await GetLocalService().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, (SolutionUpdateStatus, ImmutableArray<Deltas>, ImmutableArray<DiagnosticData>)>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, cancellationToken),
                    callbackTarget: new Callback(activeStatementSpanProvider),
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    (status, deltas, diagnosticsByProject) = result.Value;
                }
                else
                {
                    status = SolutionUpdateStatus.Blocked;
                    deltas = ImmutableArray<Deltas>.Empty;
                    diagnosticsByProject = ImmutableArray<DiagnosticData>.Empty;
                }
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(Workspace, solution, diagnosticsByProject);

            return (status, deltas);
        }

        public async ValueTask CommitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().CommitSolutionUpdate();
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancellationToken) => service.CommitSolutionUpdateAsync(cancellationToken),
                callbackTarget: Callback.Unused,
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().DiscardSolutionUpdate();
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancellationToken) => service.DiscardSolutionUpdateAsync(cancellationToken),
                callbackTarget: Callback.Unused,
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, LinePositionSpan?>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetCurrentActiveStatementPositionAsync(solutionInfo, instructionId, cancellationToken),
                callbackTarget: new Callback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool?>(
                (service, cancellationToken) => service.IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken),
                callbackTarget: Callback.Unused,
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetBaseActiveStatementSpansAsync(documentIds, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>(
                (service, cancellationToken) => service.GetBaseActiveStatementSpansAsync(documentIds, cancellationToken),
                callbackTarget: Callback.Unused,
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>.Empty;
        }

        public async ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(Document document, DocumentActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>(
                document.Project.Solution,
                (service, solutionInfo, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, document.Id, cancellationToken),
                callbackTarget: new Callback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : default;
        }

        public async ValueTask OnSourceFileUpdatedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().OnSourceFileUpdated(documentId);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
               (service, cancellationToken) => service.OnSourceFileUpdatedAsync(documentId, cancellationToken),
               callbackTarget: Callback.Unused,
               cancellationToken).ConfigureAwait(false);
        }
    }
}
