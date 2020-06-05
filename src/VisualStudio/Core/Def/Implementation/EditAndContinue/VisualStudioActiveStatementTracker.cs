// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Symbols;

using Dbg = Microsoft.VisualStudio.Debugger.UI.Interfaces;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(Dbg.IManagedActiveStatementTracker)), Shared]
    internal sealed class VisualStudioActiveStatementTracker : Dbg.IManagedActiveStatementTracker
    {
        private readonly Workspace _workspace;
        private readonly IEditAndContinueWorkspaceService _encService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioActiveStatementTracker(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
        }

        public async Task<DkmTextSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
        {
            var instructionId = new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset);
            var span = await _encService.GetCurrentActiveStatementPositionAsync(_workspace.CurrentSolution, instructionId, cancellationToken).ConfigureAwait(false);
            return span?.ToDebuggerSpan();
        }

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
            => _encService.IsActiveStatementInExceptionRegionAsync(new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset), cancellationToken);
    }
}
