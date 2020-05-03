// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IManagedActiveStatementTracker)), Shared]
    internal sealed class VisualStudioActiveStatementTracker : IManagedActiveStatementTracker
    {
        private readonly IEditAndContinueWorkspaceService _encService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioActiveStatementTracker(VisualStudioWorkspace workspace)
            => _encService = workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

        public async Task<VsTextSpan?> GetCurrentActiveStatementPositionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
            => (await _encService.GetCurrentActiveStatementPositionAsync(new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset), cancellationToken).ConfigureAwait(false))?.ToVsTextSpan();

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken)
            => _encService.IsActiveStatementInExceptionRegionAsync(new ActiveInstructionId(moduleId, methodToken, methodVersion, ilOffset), cancellationToken);
    }
}
