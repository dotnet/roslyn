// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IEditAndContinueManagedModuleUpdateProvider)), Shared]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class VisualStudioManagedModuleUpdateProvider : IEditAndContinueManagedModuleUpdateProvider
    {
        private readonly IEditAndContinueWorkspaceService _encService;
        private readonly Workspace _workspace;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioManagedModuleUpdateProvider(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
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
            var solution = _workspace.CurrentSolution;

            try
            {
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                return await _encService.HasChangesAsync(solution, activeStatementSpanProvider, sourceFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return true;
            }
        }

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            var solution = _workspace.CurrentSolution;

            try
            {
                var activeStatementSpanProvider = GetActiveStatementSpanProvider(solution);
                var (summary, deltas) = await _encService.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                return new ManagedModuleUpdates(summary.ToModuleUpdateStatus(), deltas.SelectAsArray(ModuleUtilities.ToModuleUpdate).ToReadOnlyCollection());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _encService.ReportApplyChangesException(solution, e.Message);
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<DkmManagedModuleUpdate>.Empty.ToReadOnlyCollection());
            }
        }

        public void CommitUpdates()
        {
            try
            {
                _encService.CommitSolutionUpdate();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }

        public void DiscardUpdates()
        {
            try
            {
                _encService.DiscardSolutionUpdate();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
            }
        }
    }
}
