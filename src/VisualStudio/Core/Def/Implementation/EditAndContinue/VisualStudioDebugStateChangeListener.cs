// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.UI.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IDebugStateChangeListener))]
    [ExportMetadata("UIContext", Guids.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class VisualStudioDebugStateChangeListener : IDebugStateChangeListener
    {
        private readonly Workspace _workspace;
        private readonly IDebuggingWorkspaceService _debuggingService;
        private readonly IActiveStatementTrackingService _activeStatementTrackingService;

        // EnC service or null if EnC is disabled for the debug session.
        private IEditAndContinueWorkspaceService? _encService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDebugStateChangeListener(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
            _debuggingService = workspace.Services.GetRequiredService<IDebuggingWorkspaceService>();
            _activeStatementTrackingService = workspace.Services.GetRequiredService<IActiveStatementTrackingService>();
        }

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public void StartDebugging(DebugSessionOptions options)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);

            if ((options & DebugSessionOptions.EditAndContinueDisabled) == 0)
            {
                _encService = _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
                _encService.StartDebuggingSession(_workspace.CurrentSolution);
            }
            else
            {
                _encService = null;
            }
        }

        public void EnterBreakState(IManagedActiveStatementProvider activeStatementProvider)
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);

            _encService?.StartEditSession(async cancellationToken =>
            {
                var infos = await activeStatementProvider.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                return infos.SelectAsArray(ModuleUtilities.ToActiveStatementDebugInfo);
            });

            _activeStatementTrackingService.StartTracking();
        }

        public void ExitBreakState()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);
            _activeStatementTrackingService.EndTracking();
            _encService?.EndEditSession();
        }

        public void StopDebugging()
        {
            _debuggingService.OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Design);
            _encService?.EndDebuggingSession();
        }
    }
}
