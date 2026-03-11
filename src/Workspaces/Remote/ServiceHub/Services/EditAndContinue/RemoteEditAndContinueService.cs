// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class RemoteEditAndContinueService : BrokeredServiceBase, IRemoteEditAndContinueService
{
    internal sealed class Factory : FactoryBase<IRemoteEditAndContinueService, IRemoteEditAndContinueService.ICallback>
    {
        protected override IRemoteEditAndContinueService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEditAndContinueService.ICallback> callback)
            => new RemoteEditAndContinueService(arguments, callback);
    }

    private sealed class ManagedEditAndContinueDebuggerService : IManagedHotReloadService
    {
        private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;
        private readonly RemoteServiceCallbackId _callbackId;

        public ManagedEditAndContinueDebuggerService(RemoteCallback<IRemoteEditAndContinueService.ICallback> callback, RemoteServiceCallbackId callbackId)
        {
            _callback = callback;
            _callbackId = callbackId;
        }

        ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> IManagedHotReloadService.GetActiveStatementsAsync(CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.GetActiveStatementsAsync(_callbackId, cancellationToken), cancellationToken);

        ValueTask<ManagedHotReloadAvailability> IManagedHotReloadService.GetAvailabilityAsync(Guid moduleVersionId, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.GetAvailabilityAsync(_callbackId, moduleVersionId, cancellationToken), cancellationToken);

        ValueTask<ImmutableArray<string>> IManagedHotReloadService.GetCapabilitiesAsync(CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.GetCapabilitiesAsync(_callbackId, cancellationToken), cancellationToken);

        ValueTask IManagedHotReloadService.PrepareModuleForUpdateAsync(Guid moduleVersionId, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.PrepareModuleForUpdateAsync(_callbackId, moduleVersionId, cancellationToken), cancellationToken);
    }

    private sealed class SourceTextProvider : IPdbMatchingSourceTextProvider
    {
        private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;
        private readonly RemoteServiceCallbackId _callbackId;

        public SourceTextProvider(RemoteCallback<IRemoteEditAndContinueService.ICallback> callback, RemoteServiceCallbackId callbackId)
        {
            _callback = callback;
            _callbackId = callbackId;
        }

        public ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.TryGetMatchingSourceTextAsync(_callbackId, filePath, requiredChecksum, checksumAlgorithm, cancellationToken), cancellationToken);
    }

    private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;

    public RemoteEditAndContinueService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEditAndContinueService.ICallback> callback)
        : base(arguments)
    {
        _callback = callback;
    }

    private IEditAndContinueService GetService()
        => GetWorkspace().Services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

    private ActiveStatementSpanProvider CreateActiveStatementSpanProvider(RemoteServiceCallbackId callbackId)
        => new((documentId, filePath, cancellationToken) => _callback.InvokeAsync((callback, cancellationToken) => callback.GetSpansAsync(callbackId, documentId, filePath, cancellationToken), cancellationToken));

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, bool reportDiagnostics, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var debuggerService = new ManagedEditAndContinueDebuggerService(_callback, callbackId);
            var sourceTextProvider = new SourceTextProvider(_callback, callbackId);

            var sessionId = GetService().StartDebuggingSession(solution, debuggerService, sourceTextProvider, reportDiagnostics);
            return sessionId;
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask BreakStateOrCapabilitiesChangedAsync(DebuggingSessionId sessionId, bool? inBreakState, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            GetService().BreakStateOrCapabilitiesChanged(sessionId, inBreakState);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask EndDebuggingSessionAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            GetService().EndDebuggingSession(sessionId);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            try
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var diagnostics = await GetService().GetDocumentDiagnosticsAsync(document, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                return diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(diagnostic, document));
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<EmitSolutionUpdateResults.Data> EmitSolutionUpdateAsync(
        Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var service = GetService();

            try
            {
                return (await service.EmitSolutionUpdateAsync(sessionId, solution, runningProjects, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false)).Dehydrate();
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return EmitSolutionUpdateResults.Data.CreateFromInternalError(solution, e.Message, runningProjects);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask CommitSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            GetService().CommitSolutionUpdate(sessionId);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask DiscardSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            GetService().DiscardSolutionUpdate(sessionId);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Checksum solutionChecksum, DebuggingSessionId sessionId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            return await GetService().GetBaseActiveStatementSpansAsync(sessionId, solution, documentIds, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, DocumentId documentId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetRequiredTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
            return await GetService().GetAdjustedActiveStatementSpansAsync(sessionId, document, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask SetFileLoggingDirectoryAsync(string? logDirectory, CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            GetService().SetFileLoggingDirectory(logDirectory);
        }, cancellationToken);
    }
}
