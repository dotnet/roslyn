// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
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

        private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;

        public RemoteEditAndContinueService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEditAndContinueService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        private IEditAndContinueWorkspaceService GetService()
            => GetWorkspace().Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        private ActiveStatementSpanProvider CreateActiveStatementSpanProvider(RemoteServiceCallbackId callbackId)
            => new((documentId, filePath, cancellationToken) => _callback.InvokeAsync((callback, cancellationToken) => callback.GetSpansAsync(callbackId, documentId, filePath, cancellationToken), cancellationToken));

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<DebuggingSessionId> StartDebuggingSessionAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, ImmutableArray<DocumentId> captureMatchingDocuments, bool captureAllMatchingDocuments, bool reportDiagnostics, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var debuggerService = new ManagedEditAndContinueDebuggerService(_callback, callbackId);
                var sessionId = await GetService().StartDebuggingSessionAsync(solution, debuggerService, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics, cancellationToken).ConfigureAwait(false);
                return sessionId;
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> BreakStateOrCapabilitiesChangedAsync(DebuggingSessionId sessionId, bool? inBreakState, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().BreakStateOrCapabilitiesChanged(sessionId, inBreakState, out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().EndDebuggingSession(sessionId, out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

                var diagnostics = await GetService().GetDocumentDiagnosticsAsync(document, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                return diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(diagnostic, document));
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<bool> HasChangesAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, string? sourceFilePath, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                return await GetService().HasChangesAsync(sessionId, solution, CreateActiveStatementSpanProvider(callbackId), sourceFilePath, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<EmitSolutionUpdateResults.Data> EmitSolutionUpdateAsync(
            Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var service = GetService();

                try
                {
                    var results = await service.EmitSolutionUpdateAsync(sessionId, solution, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                    return results.Dehydrate(solution);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    var updates = new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);
                    var diagnostic = Diagnostic.Create(descriptor, Location.None, new[] { e.Message });
                    var diagnostics = ImmutableArray.Create(DiagnosticData.Create(diagnostic, project: null));

                    return new EmitSolutionUpdateResults.Data(updates, diagnostics, ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty, syntaxError: null);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> CommitSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().CommitSolutionUpdate(sessionId, out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask DiscardSolutionUpdateAsync(DebuggingSessionId sessionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().DiscardSolutionUpdate(sessionId);
                return default;
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
        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Checksum solutionChecksum, DebuggingSessionId sessionId, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                return await GetService().IsActiveStatementInExceptionRegionAsync(sessionId, solution, instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, DebuggingSessionId sessionId, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                return await GetService().GetCurrentActiveStatementPositionAsync(sessionId, solution, CreateActiveStatementSpanProvider(callbackId), instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask OnSourceFileUpdatedAsync(Checksum solutionChecksum, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, solution =>
            {
                // TODO: Non-C#/VB documents are not currently serialized to remote workspace.
                // https://github.com/dotnet/roslyn/issues/47341
                var document = solution.GetDocument(documentId);
                if (document != null)
                {
                    GetService().OnSourceFileUpdated(document);
                }

                return ValueTaskFactory.CompletedTask;
            }, cancellationToken);
        }
    }
}
