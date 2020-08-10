// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
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
        public Task<ImmutableArray<DocumentId>> StartEditSessionAsync(CancellationToken cancellationToken)
        {
            return RunService(() =>
            {
                GetService().StartEditSession(
                    activeStatementsProvider: async cancellationToken =>
                    {
                        var result = await EndPoint.InvokeAsync<ImmutableArray<ActiveStatementDebugInfo.Data>>(
                            nameof(IRemoteEditAndContinueService.IStartEditSessionCallback.GetActiveStatementsAsync),
                            Array.Empty<object>(),
                            cancellationToken).ConfigureAwait(false);

                        return result.SelectAsArray(item => item.Deserialize());
                    },
                    debuggeeModuleMetadataProvider: this,
                    out var documentsToReanalyze);

                return Task.FromResult(documentsToReanalyze);
            }, cancellationToken);
        }

        Task<(int errorCode, string? errorMessage)?> IDebuggeeModuleMetadataProvider.GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            return EndPoint.InvokeAsync<(int, string?)?>(
                nameof(IRemoteEditAndContinueService.IStartEditSessionCallback.GetEncAvailabilityAsync),
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
        public Task<ImmutableArray<DocumentId>> EndEditSessionAsync(CancellationToken cancellationToken)
        {
            return RunService(() =>
            {
                GetService().EndEditSession(out var documentsToReanalyze);
                return Task.FromResult(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(CancellationToken cancellationToken)
        {
            return RunService(() =>
            {
                GetService().EndDebuggingSession(out var documentsToReanalyze);
                return Task.FromResult(documentsToReanalyze);
            }, cancellationToken);
        }

        /// <summary>
        /// Remote API.
        /// </summary>
        public Task<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunService(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                var diagnostics = await GetService().GetDocumentDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
                return diagnostics.SelectAsArray(diagnostic => DiagnosticData.Create(diagnostic, document));
            }, cancellationToken);
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
        public Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas.Data> Deltas, ImmutableArray<DiagnosticData> Diagnostics)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var service = GetService();
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                try
                {
                    var result = await service.EmitSolutionUpdateAsync(solution, cancellationToken).ConfigureAwait(false);

                    return (result.Summary, result.Deltas.SelectAsArray(d => d.Serialize()), ToDiagnosticData(solution, result.Diagnostics));
                }
                catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(e, cancellationToken))
                {
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);
                    var diagnostic = Diagnostic.Create(descriptor, Location.None, new[] { e.Message });
                    var diagnostics = ImmutableArray.Create(DiagnosticData.Create(diagnostic, solution.Options));

                    return (SolutionUpdateStatus.Blocked, ImmutableArray<Deltas.Data>.Empty, diagnostics);
                }
            }, cancellationToken);
        }

        private static ImmutableArray<DiagnosticData> ToDiagnosticData(Solution solution, ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnosticsByProject)
        {
            var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var result);

            foreach (var (projectId, diagnostics) in diagnosticsByProject)
            {
                var project = solution.GetProject(projectId);

                foreach (var diagnostic in diagnostics)
                {
                    var document = solution.GetDocument(diagnostic.Location.SourceTree);
                    if (document != null)
                    {
                        result.Add(DiagnosticData.Create(diagnostic, document));
                    }
                    else if (project != null)
                    {
                        result.Add(DiagnosticData.Create(diagnostic, project));
                    }
                    else
                    {
                        result.Add(DiagnosticData.Create(diagnostic, solution.Options));
                    }
                }
            }

            return result.ToImmutable();
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

        public Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>?> GetDocumentActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                var result = await GetService().GetDocumentActiveStatementSpansAsync(document, cancellationToken).ConfigureAwait(false);
                return result.IsDefault ? (ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>?)null : result;
            }, cancellationToken);
        }

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            return RunServiceAsync(() =>
                GetService().IsActiveStatementInExceptionRegionAsync(new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset), cancellationToken), cancellationToken);
        }

        public Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var instructionId = new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset);
                return await GetService().GetCurrentActiveStatementPositionAsync(solution, instructionId, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task OnSourceFileUpdatedAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            RunService(() => GetService().OnSourceFileUpdated(documentId), cancellationToken);
            return Task.CompletedTask;
        }
    }
}
