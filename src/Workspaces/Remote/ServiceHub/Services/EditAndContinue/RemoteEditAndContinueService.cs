// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class RemoteEditAndContinueService : ServiceBase, IRemoteEditAndContinueService, IDebuggeeModuleMetadataProvider
    {
        public RemoteEditAndContinueService(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        private static IEditAndContinueWorkspaceService GetService()
            => SolutionService.PrimaryWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                GetService().StartDebuggingSession(solution);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task StartEditSessionAsync(CancellationToken cancellationToken)
        {
            RunService(() =>
            {
                GetService().StartEditSession(
                    activeStatementsProvider: cancellationToken =>
                        EndPoint.InvokeAsync<ImmutableArray<ActiveStatementDebugInfo>>(
                            nameof(IRemoteEditAndContinueService.IStartEditSessionCallback.GetActiveStatementsAsync),
                            Array.Empty<object>(),
                            cancellationToken),
                    debuggeeModuleMetadataProvider: this);
            }, cancellationToken);

            return Task.CompletedTask;
        }

        Task<(int errorCode, string? errorMessage)?> IDebuggeeModuleMetadataProvider.GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            return EndPoint.InvokeAsync<(int, string?)?>(
                nameof(IRemoteEditAndContinueService.IStartEditSessionCallback.GetActiveStatementsAsync),
                new object[] { mvid },
                cancellationToken);
        }

        // TODO: fire and forget
        Task IDebuggeeModuleMetadataProvider.PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
        {
            return EndPoint.InvokeAsync(
                nameof(IRemoteEditAndContinueService.IStartEditSessionCallback.PrepareModuleForUpdateAsync),
                new object[] { mvid },
                cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task EndEditSessionAsync(CancellationToken cancellationToken)
        {
            RunService(() => GetService().EndEditSession(), cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task EndDebuggingSessionAsync(CancellationToken cancellationToken)
        {
            RunService(() => GetService().EndDebuggingSession(), cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, string? sourceFilePath, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().HasChangesAsync(solution, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var service = GetService();
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                try
                {
                    return await service.EmitSolutionUpdateAsync(solution, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e, cancellationToken))
                {
                    service.ReportApplyChangesException(solution, e.Message);
                    return (SolutionUpdateStatus.Blocked, ImmutableArray<Deltas>.Empty);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task CommitUpdateAsync(CancellationToken cancellationToken)
        {
            RunService(() => GetService().CommitSolutionUpdate(), cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task DiscardUpdatesAsync(CancellationToken cancellationToken)
        {
            RunService(() => GetService().DiscardSolutionUpdate(), cancellationToken);
            return Task.CompletedTask;
        }

        public Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(() => GetService().GetBaseActiveStatementSpansAsync(documentIds, cancellationToken), cancellationToken);
        }

        public Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                return await GetService().GetDocumentActiveStatementSpansAsync(document, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(() => GetService().IsActiveStatementInExceptionRegionAsync(instructionId, cancellationToken), cancellationToken);
        }

        public Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, ActiveInstructionId instructionId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                return await GetService().GetCurrentActiveStatementPositionAsync(solution, instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
