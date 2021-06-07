// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Shared]
    [Export(typeof(IManagedHotReloadLanguageService))]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedHotReloadLanguageService : IManagedHotReloadLanguageService
    {
        private sealed class DebuggerService : IManagedEditAndContinueDebuggerService
        {
            private readonly IManagedHotReloadService _hotReloadService;

            public DebuggerService(IManagedHotReloadService hotReloadService)
            {
                _hotReloadService = hotReloadService;
            }

            public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

            public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
                => Task.FromResult(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available));

            public Task<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
                => _hotReloadService.GetCapabilitiesAsync(cancellationToken).AsTask();

            public Task PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
            (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly DebuggerService _debuggerService;

        private IDisposable? _debuggingSessionConnection;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedHotReloadLanguageService(
            VisualStudioWorkspace workspace,
            IManagedHotReloadService hotReloadService,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _debuggerService = new DebuggerService(hotReloadService);
            _diagnosticService = diagnosticService;
            _diagnosticUpdateSource = diagnosticUpdateSource;
        }

        public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                _debuggingSessionConnection = await _proxy.StartDebuggingSessionAsync(solution, _debuggerService, captureMatchingDocuments: false, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                var (moduleUpdates, diagnosticData, rudeEdits) = await _proxy.EmitSolutionUpdateAsync(solution, s_solutionActiveStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);

                var updates = moduleUpdates.Updates.SelectAsArray(
                    update => new ManagedHotReloadUpdate(update.Module, update.ILDelta, update.MetadataDelta));

                var diagnostics = await EmitSolutionUpdateResults.GetHotReloadDiagnosticsAsync(solution, diagnosticData, rudeEdits, cancellationToken).ConfigureAwait(false);

                return new ManagedHotReloadUpdates(updates, diagnostics);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.InternalError);

                // TODO: better error
                var diagnostic = new ManagedHotReloadDiagnostic(
                    descriptor.Id,
                    string.Format(descriptor.MessageFormat.ToString(), "", e.Message),
                    ManagedHotReloadDiagnosticSeverity.Error,
                    filePath: "",
                    span: default);

                return new ManagedHotReloadUpdates(ImmutableArray<ManagedHotReloadUpdate>.Empty, ImmutableArray.Create(diagnostic));
            }
        }

        public async ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _proxy.CommitSolutionUpdateAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

        public async ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _proxy.DiscardSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

        public async ValueTask EndSessionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _proxy.EndDebuggingSessionAsync(_diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfNull(_debuggingSessionConnection);
                _debuggingSessionConnection.Dispose();
                _debuggingSessionConnection = null;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }
    }
}
