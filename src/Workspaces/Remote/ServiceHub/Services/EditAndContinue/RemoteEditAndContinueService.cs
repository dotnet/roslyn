﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class RemoteEditAndContinueService : BrokeredServiceBase, IRemoteEditAndContinueService
    {
        internal sealed class Factory : FactoryBase<IRemoteEditAndContinueService, IRemoteEditAndContinueService.ICallback>
        {
            protected override IRemoteEditAndContinueService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEditAndContinueService.ICallback> callback)
                => new RemoteEditAndContinueService(arguments, callback);
        }

        private sealed class DebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
        {
            private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public DebuggeeModuleMetadataProvider(RemoteCallback<IRemoteEditAndContinueService.ICallback> callback, RemoteServiceCallbackId callbackId)
            {
                _callback = callback;
                _callbackId = callbackId;
            }

            ValueTask<(int errorCode, string? errorMessage)?> IDebuggeeModuleMetadataProvider.GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.GetEncAvailabilityAsync(_callbackId, mvid, cancellationToken), cancellationToken);

            ValueTask IDebuggeeModuleMetadataProvider.PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.PrepareModuleForUpdateAsync(_callbackId, mvid, cancellationToken), cancellationToken);
        }

        private readonly RemoteCallback<IRemoteEditAndContinueService.ICallback> _callback;

        public RemoteEditAndContinueService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteEditAndContinueService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        private IEditAndContinueWorkspaceService GetService()
            => GetWorkspace().Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        private ActiveStatementProvider CreateActiveStatementProvider(RemoteServiceCallbackId callbackId)
            => new(cancellationToken => _callback.InvokeAsync((callback, cancellationToken) => callback.GetActiveStatementsAsync(callbackId, cancellationToken), cancellationToken));

        private SolutionActiveStatementSpanProvider CreateSolutionActiveStatementSpanProvider(RemoteServiceCallbackId callbackId)
            => new((documentId, cancellationToken) => _callback.InvokeAsync((callback, cancellationToken) => callback.GetSpansAsync(callbackId, documentId, cancellationToken), cancellationToken));

        private DocumentActiveStatementSpanProvider CreateDocumentActiveStatementSpanProvider(RemoteServiceCallbackId callbackId)
            => new(cancellationToken => _callback.InvokeAsync((callback, cancellationToken) => callback.GetSpansAsync(callbackId, cancellationToken), cancellationToken));

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                GetService().StartDebuggingSession(solution);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> StartEditSessionAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().StartEditSession(
                    activeStatementsProvider: CreateActiveStatementProvider(callbackId),
                    debuggeeModuleMetadataProvider: new DebuggeeModuleMetadataProvider(_callback, callbackId),
                    out var documentsToReanalyze);

                return new ValueTask<ImmutableArray<DocumentId>>(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<DocumentId>> EndEditSessionAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().EndEditSession(out var documentsToReanalyze);
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

                var diagnostics = await GetService().GetDocumentDiagnosticsAsync(document, CreateDocumentActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
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

                return await GetService().HasChangesAsync(solution, CreateSolutionActiveStatementSpanProvider(callbackId), sourceFilePath, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas, ImmutableArray<DiagnosticData> Diagnostics)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var service = GetService();
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                try
                {
                    return await service.EmitSolutionUpdateAsync(solution, CreateSolutionActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);
                    var diagnostic = Diagnostic.Create(descriptor, Location.None, new[] { e.Message });
                    var diagnostics = ImmutableArray.Create(DiagnosticData.Create(diagnostic, solution.Options));

                    return (SolutionUpdateStatus.Blocked, ImmutableArray<Deltas>.Empty, diagnostics);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask CommitSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().CommitSolutionUpdate();
                return default;
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
        public ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
                GetService().GetBaseActiveStatementSpansAsync(documentIds, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                return await GetService().GetAdjustedActiveStatementSpansAsync(document, CreateDocumentActiveStatementSpanProvider(callbackId), cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
                GetService().IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().GetCurrentActiveStatementPositionAsync(solution, CreateSolutionActiveStatementSpanProvider(callbackId), instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public ValueTask OnSourceFileUpdatedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                GetService().OnSourceFileUpdated(documentId);
                return default;
            }, cancellationToken);
        }
    }
}
