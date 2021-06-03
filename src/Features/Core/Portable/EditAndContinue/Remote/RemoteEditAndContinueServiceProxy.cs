// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Facade used to call remote <see cref="IRemoteEditAndContinueService"/> methods.
    /// Encapsulates all RPC logic as well as dispatching to the local service if the remote service is disabled.
    /// THe facade is useful for targeted testing of serialization/deserialization of EnC service calls.
    /// </summary>
    internal readonly partial struct RemoteEditAndContinueServiceProxy
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

        public async ValueTask<RemoteDebuggingSessionProxy?> StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var session = await GetLocalService().StartDebuggingSessionAsync(solution, debuggerService, captureMatchingDocuments, cancellationToken).ConfigureAwait(false);
                return new RemoteDebuggingSessionProxy(Workspace, LocalConnection.Instance, session.Id);
            }

            // need to keep the providers alive until the edit session ends:
            var connection = client.CreateConnection<IRemoteEditAndContinueService>(
                callbackTarget: new EditSessionCallback(debuggerService));

            var sessionId = await connection.TryInvokeAsync(
                solution,
                async (service, solutionInfo, callbackId, cancellationToken) => await service.StartDebuggingSessionAsync(solutionInfo, callbackId, captureMatchingDocuments, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            if (sessionId.HasValue)
            {
                return new RemoteDebuggingSessionProxy(Workspace, connection, sessionId.Value);
            }

            connection.Dispose();
            return null;
        }

        private sealed class LocalConnection : IDisposable
        {
            public static readonly LocalConnection Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
