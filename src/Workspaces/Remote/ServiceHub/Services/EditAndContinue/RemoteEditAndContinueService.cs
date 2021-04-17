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
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
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

        private sealed class ManagedEditAndContinueDebuggerService : IManagedEditAndContinueDebuggerService
        {
            private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public ManagedEditAndContinueDebuggerService(RemoteCallback<IRemoteEditAndContinueService.ICallback> callback, RemoteServiceCallbackId callbackId)
            {
                _callback = callback;
                _callbackId = callbackId;
            }

            Task<ImmutableArray<ManagedActiveStatementDebugInfo>> IManagedEditAndContinueDebuggerService.GetActiveStatementsAsync(CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.GetActiveStatementsAsync(_callbackId, cancellationToken), cancellationToken).AsTask();

            Task<ManagedEditAndContinueAvailability> IManagedEditAndContinueDebuggerService.GetAvailabilityAsync(Guid moduleVersionId, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.GetAvailabilityAsync(_callbackId, moduleVersionId, cancellationToken), cancellationToken).AsTask();

            Task<ImmutableArray<string>> IManagedEditAndContinueDebuggerService.GetCapabilitiesAsync(CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.GetCapabilitiesAsync(_callbackId, cancellationToken), cancellationToken).AsTask();

            Task IManagedEditAndContinueDebuggerService.PrepareModuleForUpdateAsync(Guid moduleVersionId, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.PrepareModuleForUpdateAsync(_callbackId, moduleVersionId, cancellationToken), cancellationToken).AsTask();
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
        public ValueTask StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, bool captureMatchingDocuments, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var debuggerService = new ManagedEditAndContinueDebuggerService(_callback, callbackId);
                await GetService().StartDebuggingSessionAsync(solution, debuggerService, captureMatchingDocuments, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> BreakStateEnteredAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().BreakStateEntered(out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().EndDebuggingSession(out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);

                var diagnostics = await GetService().GetDocumentDiagnosticsAsync(document, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                return diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(diagnostic, document));
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, string? sourceFilePath, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                return await GetService().HasChangesAsync(solution, CreateActiveStatementSpanProvider(callbackId), sourceFilePath, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<EmitSolutionUpdateResults.Data> EmitSolutionUpdateAsync(
            PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var service = GetService();
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                try
                {
                    var results = await service.EmitSolutionUpdateAsync(solution, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                    return results.Dehydrate(solution);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    var updates = new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);
                    var diagnostic = Diagnostic.Create(descriptor, Location.None, new[] { e.Message });
                    var diagnostics = ImmutableArray.Create(DiagnosticData.Create(diagnostic, solution.Options));

                    return new EmitSolutionUpdateResults.Data(updates, diagnostics, ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> CommitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().CommitSolutionUpdate(out var documentsToReanalyze);
                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().DiscardSolutionUpdate();
                return default;
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().GetBaseActiveStatementSpansAsync(solution, documentIds, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                return await GetService().GetAdjustedActiveStatementSpansAsync(document, CreateActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(PinnedSolutionInfo solutionInfo, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().IsActiveStatementInExceptionRegionAsync(solution, instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().GetCurrentActiveStatementPositionAsync(solution, CreateActiveStatementSpanProvider(callbackId), instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask OnSourceFileUpdatedAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                // TODO: Non-C#/VB documents are not currently serialized to remote workspace.
                // https://github.com/dotnet/roslyn/issues/47341
                var document = solution.GetDocument(documentId);
                if (document != null)
                {
                    await GetService().OnSourceFileUpdatedAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
