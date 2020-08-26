// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Symbols;
using Dbg = Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(Dbg.IManagedActiveStatementTracker)), Shared]
    internal sealed class VisualStudioActiveStatementTracker : Dbg.IManagedActiveStatementTracker
    {
        private readonly RemoteEditAndContinueServiceProxy _proxy;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioActiveStatementTracker(VisualStudioWorkspace workspace)
        {
            _proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
        }

        public async Task<DkmTextSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            try
            {
                var solution = _proxy.Workspace.CurrentSolution;

                var activeStatementSpanProvider = new SolutionActiveStatementSpanProvider(async (documentId, cancellationToken) =>
                {
                    var document = solution.GetRequiredDocument(documentId);
                    return await _activeStatementTrackingService.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);
                });

                var span = await _proxy.GetCurrentActiveStatementPositionAsync(solution, activeStatementSpanProvider, moduleId, methodToken, methodVersion, ilOffset, cancellationToken).ConfigureAwait(false);
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
                return await _proxy.IsActiveStatementInExceptionRegionAsync(moduleId, methodToken, methodVersion, ilOffset, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }
    }
}
