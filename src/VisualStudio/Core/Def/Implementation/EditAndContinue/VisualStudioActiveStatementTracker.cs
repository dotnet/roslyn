// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Utilities;
using Dbg = Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(Dbg.IManagedActiveStatementTracker)), Shared]
    internal sealed class VisualStudioActiveStatementTracker : Dbg.IManagedActiveStatementTracker
    {
        private readonly Workspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioActiveStatementTracker(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        public async Task<DkmTextSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return null;
                }

                var span = await client.RunRemoteAsync<LinePositionSpan?>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.GetCurrentActiveStatementPositionAsync),
                    solution: _workspace.CurrentSolution,
                    new object[] { new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset) },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return span?.ToDebuggerSpan();
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }

        public async Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    return null;
                }

                var result = await client.RunRemoteAsync<bool?>(
                    WellKnownServiceHubService.RemoteEditAndContinueService,
                    nameof(IRemoteEditAndContinueService.IsActiveStatementInExceptionRegionAsync),
                    solution: null,
                    new object[] { moduleId, methodToken, methodVersion, ilOffset },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }
    }
}
