// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

            public ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((DocumentActiveStatementSpanProviderCallback)GetCallback(callbackId)).GetSpansAsync(cancellationToken);

            public ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
                => ((SolutionActiveStatementSpanProviderCallback)GetCallback(callbackId)).GetSpansAsync(documentId, cancellationToken);

            public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).GetActiveStatementsAsync(cancellationToken);

            public ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).GetAvailabilityAsync(mvid, cancellationToken);

            public ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((EditSessionCallback)GetCallback(callbackId)).PrepareModuleForUpdateAsync(mvid, cancellationToken);
        }

        private sealed class DocumentActiveStatementSpanProviderCallback
        {
            private readonly DocumentActiveStatementSpanProvider _documentProvider;

            public DocumentActiveStatementSpanProviderCallback(DocumentActiveStatementSpanProvider documentProvider)
                => _documentProvider = documentProvider;

            public async ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _documentProvider(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }
        }

        private sealed class SolutionActiveStatementSpanProviderCallback
        {
            private readonly SolutionActiveStatementSpanProvider _solutionProvider;

            public SolutionActiveStatementSpanProviderCallback(SolutionActiveStatementSpanProvider solutionProvider)
                => _solutionProvider = solutionProvider;

            /// <summary>
            /// Remote API.
            /// </summary>
            public async ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                try
                {
                    return await _solutionProvider(documentId, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    return ImmutableArray<TextSpan>.Empty;
                }
            }
        }

        private sealed class EditSessionCallback
        {
            private readonly IManagedEditAndContinueDebuggerService _managedModuleInfoProvider;

            public EditSessionCallback(IManagedEditAndContinueDebuggerService debuggeeModuleMetadataProvider)
            {
                _managedModuleInfoProvider = debuggeeModuleMetadataProvider;
            }

            public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _managedModuleInfoProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    return ImmutableArray<ManagedActiveStatementDebugInfo>.Empty;
                }
            }

            public async ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    return await _managedModuleInfoProvider.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    return new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.InternalError, e.Message);
                }
            }

            public async ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    await _managedModuleInfoProvider.PrepareModuleForUpdateAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
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
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<IDisposable?> StartEditSessionAsync(
            IDiagnosticAnalyzerService diagnosticService,
            IManagedEditAndContinueDebuggerService debuggerService,
            CancellationToken cancellationToken)
        {
            IDisposable result;
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().StartEditSession(debuggerService, out documentsToReanalyze);
                result = LocalConnection.Instance;
            }
            else
            {
                // need to keep the providers alive until the edit session ends:
                var connection = client.CreateConnection<IRemoteEditAndContinueService>(
                    callbackTarget: new EditSessionCallback(debuggerService));

                var documentsToReanalyzeOpt = await connection.TryInvokeAsync(
                    (service, callbackId, cancellationToken) => service.StartEditSessionAsync(callbackId, cancellationToken),
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
                (service, solutionInfo, callbackId, cancellationToken) => service.GetDocumentDiagnosticsAsync(solutionInfo, callbackId, document.Id, cancellationToken),
                callbackTarget: new DocumentActiveStatementSpanProviderCallback(activeStatementSpanProvider),
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

        public async ValueTask<bool> HasChangesAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.HasChangesAsync(solutionInfo, callbackId, sourceFilePath, cancellationToken),
                callbackTarget: new SolutionActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : true;
        }

        public async ValueTask<ManagedModuleUpdates> EmitSolutionUpdateAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, CancellationToken cancellationToken)
        {
            ManagedModuleUpdates updates;
            ImmutableArray<DiagnosticData> diagnosticsByProject;

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                (updates, diagnosticsByProject) = await GetLocalService().EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, (ManagedModuleUpdates, ImmutableArray<DiagnosticData>)>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, callbackId, cancellationToken),
                    callbackTarget: new SolutionActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    (updates, diagnosticsByProject) = result.Value;
                }
                else
                {
                    updates = new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
                    diagnosticsByProject = ImmutableArray<DiagnosticData>.Empty;
                }
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics();

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(Workspace, solution, diagnosticsByProject);

            return updates;
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
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, SolutionActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace.Services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, LinePositionSpan?>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetCurrentActiveStatementPositionAsync(solutionInfo, callbackId, instructionId, cancellationToken),
                callbackTarget: new SolutionActiveStatementSpanProviderCallback(activeStatementSpanProvider),
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

        public async ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetBaseActiveStatementSpansAsync(solution, documentIds, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetBaseActiveStatementSpansAsync(solutionInfo, documentIds, cancellationToken),
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
                (service, solutionInfo, callbackId, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, callbackId, document.Id, cancellationToken),
                callbackTarget: new DocumentActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : default;
        }

        public async ValueTask OnSourceFileUpdatedAsync(Document document, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().OnSourceFileUpdated(document);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
               document.Project.Solution,
               (service, solutionInfo, cancellationToken) => service.OnSourceFileUpdatedAsync(solutionInfo, document.Id, cancellationToken),
               cancellationToken).ConfigureAwait(false);
        }
    }
}
