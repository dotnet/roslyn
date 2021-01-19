﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IEditAndContinueManagedModuleUpdateProvider)), Shared]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class VisualStudioManagedModuleUpdateProvider : IEditAndContinueManagedModuleUpdateProvider
    {
        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly EditAndContinueDiagnosticUpdateSource _emitDiagnosticsUpdateSource;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioManagedModuleUpdateProvider(
            VisualStudioWorkspace workspace,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _emitDiagnosticsUpdateSource = diagnosticUpdateSource;
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
        }

        private SolutionActiveStatementSpanProvider GetActiveStatementSpanProvider(Solution solution)
            => new SolutionActiveStatementSpanProvider((documentId, cancellationToken) =>
                _activeStatementTrackingService.GetSpansAsync(solution.GetRequiredDocument(documentId), cancellationToken));

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
        /// </summary>
        public async Task<bool> HasChangesAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                return await _proxy.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return true;
            }
        }

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                var (summary, deltas) = await _proxy.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, _emitDiagnosticsUpdateSource, cancellationToken).ConfigureAwait(false);
                return new ManagedModuleUpdates(summary.ToModuleUpdateStatus(), deltas.SelectAsArray(ModuleUtilities.ToModuleUpdate).ToReadOnlyCollection());
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, new ReadOnlyCollection<DkmManagedModuleUpdate>(Array.Empty<DkmManagedModuleUpdate>()));
            }
        }

#pragma warning disable VSTHRD102 // TODO: Implement internal logic asynchronously
        public void CommitUpdates()
            => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await _proxy.CommitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                }
            });

        public void DiscardUpdates()
            => ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await _proxy.DiscardSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                }
            });
#pragma warning restore
    }
}
