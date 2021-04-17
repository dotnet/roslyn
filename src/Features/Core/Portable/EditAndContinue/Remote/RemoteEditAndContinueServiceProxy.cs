// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Facade used to call remote <see cref="IRemoteEditAndContinueService"/> methods.
    /// Encapsulates all RPC logic as well as dispatching to the local service if the remote service is disabled.
    /// THe facade is useful for targeted testing of serialization/deserialization of EnC service calls.
    /// </summary>
    internal readonly struct RemoteEditAndContinueServiceProxy : IActiveStatementSpanProvider
    {
        [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteEditAndContinueService)), Shared]
        internal sealed class CallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteEditAndContinueService.ICallback
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CallbackDispatcher()
            {
            }

            public ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId? documentId, string filePath, CancellationToken cancellationToken)
                => ((ActiveStatementSpanProviderCallback)GetCallback(callbackId)).GetSpansAsync(documentId, filePath, cancellationToken);

            public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).GetActiveStatementsAsync(cancellationToken);

            public ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).GetAvailabilityAsync(mvid, cancellationToken);

            public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).GetCapabilitiesAsync(cancellationToken);

            public ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).PrepareModuleForUpdateAsync(mvid, cancellationToken);
        }

        private sealed class ActiveStatementSpanProviderCallback
        {
            private readonly ActiveStatementSpanProvider _provider;

            public ActiveStatementSpanProviderCallback(ActiveStatementSpanProvider provider)
                => _provider = provider;

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(DocumentId? documentId, string filePath, CancellationToken cancellationToken)
            {
                try
                {
                    return await _provider(documentId, filePath, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return ImmutableArray<ActiveStatementSpan>.Empty;
                }
            }
        }

        private sealed class EditSessionCallback
        {
            private readonly IManagedEditAndContinueDebuggerService _debuggerService;

            public EditSessionCallback(IManagedEditAndContinueDebuggerService debuggerService)
            {
                _debuggerService = debuggerService;
            }

            public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return ImmutableArray<ManagedActiveStatementDebugInfo>.Empty;
                }
            }

            public async ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.InternalError, e.Message);
                }
            }

            public async ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    await _debuggerService.PrepareModuleForUpdateAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    // nop
                }
            }

            public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return ImmutableArray<string>.Empty;
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

        public async ValueTask<IDisposable?> StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                await GetLocalService().StartDebuggingSessionAsync(solution, debuggerService, captureMatchingDocuments, cancellationToken).ConfigureAwait(false);
                return LocalConnection.Instance;
            }

            // need to keep the providers alive until the edit session ends:
            var connection = client.CreateConnection<IRemoteEditAndContinueService>(
                callbackTarget: new EditSessionCallback(debuggerService));

            await connection.TryInvokeAsync(
                solution,
                async (service, solutionInfo, callbackId, cancellationToken) => await service.StartDebuggingSessionAsync(solutionInfo, callbackId, captureMatchingDocuments, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private sealed class LocalConnection : IDisposable
        {
            public static readonly LocalConnection Instance = new LocalConnection();

            public void Dispose()
            {
            }
        }

        public async ValueTask BreakStateEnteredAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().BreakStateEntered(out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.BreakStateEnteredAsync(cancellationToken),
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
                    (service, cancallationToken) => service.EndDebuggingSessionAsync(cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(Workspace, documentIds: documentsToReanalyze);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();
        }

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var diagnosticData = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DiagnosticData>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetDocumentDiagnosticsAsync(solutionInfo, callbackId, document.Id, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
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

        public async ValueTask<bool> HasChangesAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.HasChangesAsync(solutionInfo, callbackId, sourceFilePath, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : true;
        }

        public async ValueTask<(
                ManagedModuleUpdates updates,
                ImmutableArray<DiagnosticData> diagnostics,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>)> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            CancellationToken cancellationToken)
        {
            ManagedModuleUpdates moduleUpdates;
            ImmutableArray<DiagnosticData> diagnosticData;
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var results = await GetLocalService().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                moduleUpdates = results.ModuleUpdates;
                diagnosticData = results.GetDiagnosticData(solution);
                rudeEdits = results.RudeEdits;
            }
            else
            {
                var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, EmitSolutionUpdateResults.Data>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, callbackId, cancellationToken),
                    callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    moduleUpdates = result.Value.ModuleUpdates;
                    diagnosticData = result.Value.Diagnostics;
                    rudeEdits = result.Value.RudeEdits;
                }
                else
                {
                    moduleUpdates = new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
                    diagnosticData = ImmutableArray<DiagnosticData>.Empty;
                    rudeEdits = ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty;
                }
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // clear all reported rude edits:
            diagnosticService.Reanalyze(Workspace, documentIds: rudeEdits.Select(d => d.DocumentId));

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(Workspace, solution, diagnosticData);

            return (moduleUpdates, diagnosticData, rudeEdits);
        }

        public async ValueTask CommitSolutionUpdateAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().CommitSolutionUpdate(out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.CommitSolutionUpdateAsync(cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(Workspace, documentIds: documentsToReanalyze);
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
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, LinePositionSpan?>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetCurrentActiveStatementPositionAsync(solutionInfo, callbackId, instructionId, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().IsActiveStatementInExceptionRegionAsync(solution, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool?>(
                solution,
                (service, solutionInfo, cancellationToken) => service.IsActiveStatementInExceptionRegionAsync(solutionInfo, instructionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetBaseActiveStatementSpansAsync(solution, documentIds, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetBaseActiveStatementSpansAsync(solutionInfo, documentIds, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ImmutableArray<ActiveStatementSpan>>.Empty;
        }

        public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ActiveStatementSpan>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, callbackId, document.Id, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : default;
        }

        public async ValueTask OnSourceFileUpdatedAsync(Document document, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                await GetLocalService().OnSourceFileUpdatedAsync(document, cancellationToken).ConfigureAwait(false);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
               document.Project.Solution,
               (service, solutionInfo, cancellationToken) => service.OnSourceFileUpdatedAsync(solutionInfo, document.Id, cancellationToken),
               cancellationToken).ConfigureAwait(false);
        }
    }
}
