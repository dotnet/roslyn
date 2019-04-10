// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class VisualStudioManagedModuleUpdateProvider : IEditAndContinueManagedModuleUpdateProvider
    {
        private readonly IEditAndContinueWorkspaceService _encService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioManagedModuleUpdateProvider(VisualStudioWorkspace workspace)
        {
            _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
        }

        public Task<ManagedModuleUpdateStatus> GetStatusAsync(CancellationToken cancellationToken)
            => GetStatusAsync(null, cancellationToken);

        /// <summary>
        /// Returns the state of the changes made to the source. 
        /// The EnC manager calls this to determine whether there are any changes to the source 
        /// and if so whether there are any rude edits.
        /// </summary>
        public async Task<ManagedModuleUpdateStatus> GetStatusAsync(string sourceFilePath, CancellationToken cancellationToken)
        {
            try
            {
                return (await _encService.GetSolutionUpdateStatusAsync(sourceFilePath, cancellationToken).ConfigureAwait(false)).ToModuleUpdateStatus();
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
