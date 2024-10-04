// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Facade used to call remote <see cref="IRemoteEditAndContinueService"/> methods.
/// Encapsulates all RPC logic as well as dispatching to the local service if the remote service is disabled.
/// THe facade is useful for targeted testing of serialization/deserialization of EnC service calls.
/// </summary>
internal readonly partial struct RemoteEditAndContinueServiceProxy(SolutionServices services)
{
    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteEditAndContinueService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CallbackDispatcher() : RemoteServiceCallbackDispatcher, IRemoteEditAndContinueService.ICallback
    {
        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId? documentId, string filePath, CancellationToken cancellationToken)
            => ((ActiveStatementSpanProviderCallback)GetCallback(callbackId)).GetSpansAsync(documentId, filePath, cancellationToken);

        public ValueTask<string?> TryGetMatchingSourceTextAsync(RemoteServiceCallbackId callbackId, string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
            => ((DebuggingSessionCallback)GetCallback(callbackId)).TryGetMatchingSourceTextAsync(filePath, requiredChecksum, checksumAlgorithm, cancellationToken);

        public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            => ((DebuggingSessionCallback)GetCallback(callbackId)).GetActiveStatementsAsync(cancellationToken);

        public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
            => ((DebuggingSessionCallback)GetCallback(callbackId)).GetAvailabilityAsync(mvid, cancellationToken);

        public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            => ((DebuggingSessionCallback)GetCallback(callbackId)).GetCapabilitiesAsync(cancellationToken);

        public ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
            => ((DebuggingSessionCallback)GetCallback(callbackId)).PrepareModuleForUpdateAsync(mvid, cancellationToken);
    }

    private sealed class DebuggingSessionCallback(IManagedHotReloadService debuggerService, IPdbMatchingSourceTextProvider sourceTextProvider)
    {
        private readonly IManagedHotReloadService _debuggerService = debuggerService;
        private readonly IPdbMatchingSourceTextProvider _sourceTextProvider = sourceTextProvider;

        public async ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
        {
            try
            {
                return await _sourceTextProvider.TryGetMatchingSourceTextAsync(filePath, requiredChecksum, checksumAlgorithm, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _debuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return [];
            }
        }

        public async ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            try
            {
                return await _debuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.InternalError, e.Message);
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
                return [];
            }
        }
    }

    private IEditAndContinueService GetLocalService()
        => services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

    public async ValueTask<RemoteDebuggingSessionProxy?> StartDebuggingSessionAsync(
        Solution solution,
        IManagedHotReloadService debuggerService,
        IPdbMatchingSourceTextProvider sourceTextProvider,
        ImmutableArray<DocumentId> captureMatchingDocuments,
        bool captureAllMatchingDocuments,
        bool reportDiagnostics,
        CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            var sessionId = await GetLocalService().StartDebuggingSessionAsync(solution, debuggerService, sourceTextProvider, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics, cancellationToken).ConfigureAwait(false);
            return new RemoteDebuggingSessionProxy(solution.Services, LocalConnection.Instance, sessionId);
        }

        // need to keep the providers alive until the session ends:
        var connection = client.CreateConnection<IRemoteEditAndContinueService>(
            callbackTarget: new DebuggingSessionCallback(debuggerService, sourceTextProvider));

        var sessionIdOpt = await connection.TryInvokeAsync(
            solution,
            async (service, solutionInfo, callbackId, cancellationToken) => await service.StartDebuggingSessionAsync(solutionInfo, callbackId, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        if (sessionIdOpt.HasValue)
        {
            return new RemoteDebuggingSessionProxy(solution.Services, connection, sessionIdOpt.Value);
        }

        connection.Dispose();
        return null;
    }

    public async ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
    {
        // filter out documents that are not synchronized to remote process before we attempt remote invoke:
        if (!RemoteSupportedLanguages.IsSupported(document.Project.Language))
        {
            return [];
        }

        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            var diagnostics = await GetLocalService().GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            return diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(document.Project.Solution, diagnostic, document.Project));
        }

        var diagnosticData = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DiagnosticData>>(
            document.Project.Solution,
            (service, solutionInfo, callbackId, cancellationToken) => service.GetDocumentDiagnosticsAsync(solutionInfo, callbackId, document.Id, cancellationToken),
            callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
            cancellationToken).ConfigureAwait(false);

        return diagnosticData.HasValue ? diagnosticData.Value : [];
    }

    public async ValueTask SetFileLoggingDirectoryAsync(string? logDirectory, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            GetLocalService().SetFileLoggingDirectory(logDirectory);
        }
        else
        {
            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancellationToken) => service.SetFileLoggingDirectoryAsync(logDirectory, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class LocalConnection : IDisposable
    {
        public static readonly LocalConnection Instance = new();

        public void Dispose()
        {
        }
    }
}
