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

        public Task<ManagedModuleUpdateStatus> GetStatusAsync(CancellationToken cancellationToken)
            => GetStatusAsync(null, cancellationToken);

        /// <summary>
        /// Returns the state of the changes made to the source. 
        /// The EnC manager calls this to determine whether there are any changes to the source 
        /// and if so whether there are any rude edits.
        /// 
        /// TODO: Future work in the debugger https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1051385 will replace this with bool HasChangesAsync.
        /// The debugger currently uses <see cref="SolutionUpdateStatus.Ready"/> as a signal to trigger emit of updates 
        /// (i.e. to call <see cref="GetManagedModuleUpdatesAsync(CancellationToken)"/>). 
        /// When <see cref="SolutionUpdateStatus.Blocked"/> is returned updates are not emitted.
        /// Since <see cref="GetManagedModuleUpdatesAsync(CancellationToken)"/> already handles all validation and error reporting 
        /// we either return <see cref="SolutionUpdateStatus.None"/> if there are no changes or <see cref="SolutionUpdateStatus.Ready"/> if there are any changes.
        /// </summary>
        public async Task<ManagedModuleUpdateStatus> GetStatusAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                return (await _encService.HasChangesAsync(sourceFilePath, cancellationToken).ConfigureAwait(false)) ?
                    ManagedModuleUpdateStatus.Ready : ManagedModuleUpdateStatus.None;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return ManagedModuleUpdateStatus.Blocked;
            }
        }

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                var (summary, deltas) = await _encService.EmitSolutionUpdateAsync(cancellationToken).ConfigureAwait(false);
                return new ManagedModuleUpdates(summary.ToModuleUpdateStatus(), deltas.SelectAsArray(ModuleUtilities.ToModuleUpdate));
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                _encService.ReportApplyChangesException(e.Message);
                return new ManagedModuleUpdates(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty);
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
