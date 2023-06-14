// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class RemoteDebuggingSessionProxy(Workspace workspace, IDisposable? connection, DebuggingSessionId sessionId) : IActiveStatementSpanProvider, IDisposable
    {
        public void Dispose()
        {
            connection?.Dispose();
        }

        private IEditAndContinueService GetLocalService()
            => workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

        public async ValueTask BreakStateOrCapabilitiesChangedAsync(IDiagnosticAnalyzerService diagnosticService, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, bool? inBreakState, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().BreakStateOrCapabilitiesChanged(sessionId, inBreakState, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.BreakStateOrCapabilitiesChangedAsync(sessionId, inBreakState, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(workspace, projectIds: null, documentIds: documentsToReanalyze, highPriority: false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: false);
        }

        public async ValueTask EndDebuggingSessionAsync(Solution compileTimeSolution, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndDebuggingSession(sessionId, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.EndDebuggingSessionAsync(sessionId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            var designTimeDocumentsToReanalyze = await CompileTimeSolutionProvider.GetDesignTimeDocumentsAsync(
                compileTimeSolution, documentsToReanalyze, designTimeSolution: workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

            // clear all reported rude edits:
            diagnosticService.Reanalyze(workspace, projectIds: null, documentIds: designTimeDocumentsToReanalyze, highPriority: false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: true);

            Dispose();
        }

        public async ValueTask<(
                ModuleUpdates updates,
                ImmutableArray<DiagnosticData> diagnostics,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits,
                DiagnosticData? syntaxError)> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            CancellationToken cancellationToken)
        {
            ModuleUpdates moduleUpdates;
            ImmutableArray<DiagnosticData> diagnosticData;
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits;
            DiagnosticData? syntaxError;

            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    var results = await GetLocalService().EmitSolutionUpdateAsync(sessionId, solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    moduleUpdates = results.ModuleUpdates;
                    diagnosticData = results.Diagnostics.ToDiagnosticData(solution);
                    rudeEdits = results.RudeEdits;
                    syntaxError = results.GetSyntaxErrorData(solution);
                }
                else
                {
                    var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, EmitSolutionUpdateResults.Data>(
                        solution,
                        (service, solutionInfo, callbackId, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, callbackId, sessionId, cancellationToken),
                        callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        moduleUpdates = result.Value.ModuleUpdates;
                        diagnosticData = result.Value.Diagnostics;
                        rudeEdits = result.Value.RudeEdits;
                        syntaxError = result.Value.SyntaxError;
                    }
                    else
                    {
                        moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, ImmutableArray<ManagedHotReloadUpdate>.Empty);
                        diagnosticData = ImmutableArray<DiagnosticData>.Empty;
                        rudeEdits = ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty;
                        syntaxError = null;
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                diagnosticData = GetInternalErrorDiagnosticData(solution, e);
                rudeEdits = ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty;
                moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, ImmutableArray<ManagedHotReloadUpdate>.Empty);
                syntaxError = null;
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: false);

            // clear all reported rude edits:
            diagnosticService.Reanalyze(workspace, projectIds: null, documentIds: rudeEdits.Select(d => d.DocumentId), highPriority: false);

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(workspace, solution, diagnosticData, rudeEdits);

            return (moduleUpdates, diagnosticData, rudeEdits, syntaxError);
        }

        private static ImmutableArray<DiagnosticData> GetInternalErrorDiagnosticData(Solution solution, Exception e)
        {
            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.InternalError);

            var diagnostic = Diagnostic.Create(
                descriptor,
                Location.None,
                string.Format(descriptor.MessageFormat.ToString(), "", e.Message));

            return ImmutableArray.Create(DiagnosticData.Create(solution, diagnostic, project: null));
        }

        public async ValueTask CommitSolutionUpdateAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().CommitSolutionUpdate(sessionId, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.CommitSolutionUpdateAsync(sessionId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(workspace, projectIds: null, documentIds: documentsToReanalyze, highPriority: false);
        }

        public async ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().DiscardSolutionUpdate(sessionId);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancellationToken) => service.DiscardSolutionUpdateAsync(sessionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetBaseActiveStatementSpansAsync(sessionId, solution, documentIds, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetBaseActiveStatementSpansAsync(solutionInfo, sessionId, documentIds, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ImmutableArray<ActiveStatementSpan>>.Empty;
        }

        public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            // filter out documents that are not synchronized to remote process before we attempt remote invoke:
            if (!RemoteSupportedLanguages.IsSupported(document.Project.Language))
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetAdjustedActiveStatementSpansAsync(sessionId, document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ActiveStatementSpan>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, callbackId, sessionId, document.Id, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ActiveStatementSpan>.Empty;
        }
    }
}
