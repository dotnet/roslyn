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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioManagedModuleUpdateProvider(VisualStudioWorkspace workspace)
            => _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        /// <summary>
        /// Returns true if any changes have been made to the source since the last changes had been applied.
        /// </summary>
        public async Task<bool> HasChangesAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                return await _encService.HasChangesAsync(sourceFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return true;
            }
        }

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var (summary, deltas) = await _encService.EmitSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
                return new ManagedModuleUpdates(summary.ToModuleUpdateStatus(), deltas.SelectAsArray(ModuleUtilities.ToModuleUpdate).ToReadOnlyCollection());
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _encService.ReportApplyChangesException(e.Message);
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
