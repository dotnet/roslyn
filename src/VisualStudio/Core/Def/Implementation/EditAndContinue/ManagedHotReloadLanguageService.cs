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
using Microsoft.CodeAnalysis.Host;
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

        private RemoteDebuggingSessionProxy? _debuggingSession;
        private bool _disabled;

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

        private RemoteDebuggingSessionProxy GetDebuggingSession()
        {
            var debuggingSession = _debuggingSession;
            Contract.ThrowIfNull(debuggingSession);
            return debuggingSession;
        }

        private Solution GetCurrentCompileTimeSolution()
            => _proxy.Workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>().GetCompileTimeSolution(_proxy.Workspace.CurrentSolution);

        public async ValueTask StartSessionAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var openedDocumentIds = _proxy.Workspace.GetOpenDocumentIds().ToImmutableArray();

                _debuggingSession = await _proxy.StartDebuggingSessionAsync(
                    solution,
                    _debuggerService,
                    captureMatchingDocuments: openedDocumentIds,
                    captureAllMatchingDocuments: false,
                    reportDiagnostics: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }

            // the service failed, error has been reported - disable further operations
            _disabled = _debuggingSession == null;
        }

        public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return new ManagedHotReloadUpdates(ImmutableArray<ManagedHotReloadUpdate>.Empty, ImmutableArray<ManagedHotReloadDiagnostic>.Empty);
            }

            try
            {
                var solution = GetCurrentCompileTimeSolution();
                var (moduleUpdates, diagnosticData, rudeEdits) = await GetDebuggingSession().EmitSolutionUpdateAsync(solution, s_solutionActiveStatementSpanProvider, _diagnosticService, _diagnosticUpdateSource, cancellationToken).ConfigureAwait(false);

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
            if (_disabled)
            {
                return;
            }

            try
            {
                await GetDebuggingSession().CommitSolutionUpdateAsync(_diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                _disabled = true;
            }
        }

        public async ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                await GetDebuggingSession().DiscardSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                _disabled = true;
            }
        }

        public async ValueTask EndSessionAsync(CancellationToken cancellationToken)
        {
            if (_disabled)
            {
                return;
            }

            try
            {
                await GetDebuggingSession().EndDebuggingSessionAsync(_diagnosticUpdateSource, _diagnosticService, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                _disabled = true;
            }
        }
    }
}
