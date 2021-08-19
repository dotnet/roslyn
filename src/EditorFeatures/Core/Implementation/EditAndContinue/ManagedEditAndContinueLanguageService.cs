// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Shared]
    [Export(typeof(IManagedEditAndContinueLanguageService))]
    [ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedEditAndContinueLanguageService : IManagedEditAndContinueLanguageService
    {
        private readonly Lazy<IManagedEditAndContinueDebuggerService> _debuggerService;
        private readonly EditAndContinueLanguageService _encService;

        /// <summary>
        /// Import <see cref="IHostWorkspaceProvider"/> and <see cref="IManagedEditAndContinueDebuggerService"/> lazily so that the host does not need to implement them
        /// unless it implements debugger components.
        /// </summary>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedEditAndContinueLanguageService(EditAndContinueLanguageService encService, Lazy<IManagedEditAndContinueDebuggerService> debuggerService)
        {
            _encService = encService;
            _debuggerService = debuggerService;
        }

        private IDebuggingWorkspaceService GetDebuggingService()
            => _encService.WorkspaceServices.GetRequiredService<IDebuggingWorkspaceService>();

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public Task StartDebuggingAsync(DebugSessionFlags flags, CancellationToken cancellationToken)
        {
            GetDebuggingService().OnBeforeDebuggingStateChanged(DebuggingState.Design, DebuggingState.Run);

            if (flags.HasFlag(DebugSessionFlags.EditAndContinueDisabled))
            {
                _encService.Disable();
                return Task.CompletedTask;
            }

            return _encService.StartSessionAsync(_debuggerService.Value, cancellationToken).AsTask();
        }

        public Task EnterBreakStateAsync(CancellationToken cancellationToken)
        {
            GetDebuggingService().OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Break);
            return _encService.EnterBreakStateAsync(cancellationToken).AsTask();
        }

        public Task ExitBreakStateAsync(CancellationToken cancellationToken)
        {
            GetDebuggingService().OnBeforeDebuggingStateChanged(DebuggingState.Break, DebuggingState.Run);
            _encService.ExitBreakState();
            return Task.CompletedTask;
        }

        public Task CommitUpdatesAsync(CancellationToken cancellationToken)
            => _encService.CommitUpdatesAsync(cancellationToken).AsTask();

        public Task DiscardUpdatesAsync(CancellationToken cancellationToken)
            => _encService.DiscardUpdatesAsync(cancellationToken).AsTask();

        public Task StopDebuggingAsync(CancellationToken cancellationToken)
        {
            GetDebuggingService().OnBeforeDebuggingStateChanged(DebuggingState.Run, DebuggingState.Design);
            return _encService.EndSessionAsync(cancellationToken).AsTask();
        }

        public Task<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
            => _encService.HasChangesAsync(sourceFilePath, cancellationToken).AsTask();

        public async Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
            => (await _encService.GetUpdatesAsync(trackActiveStatements: true, cancellationToken).ConfigureAwait(false)).updates;

        public Task<SourceSpan?> GetCurrentActiveStatementPositionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
            => _encService.GetCurrentActiveStatementPositionAsync(instruction, cancellationToken);

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
            => _encService.IsActiveStatementInExceptionRegionAsync(instruction, cancellationToken);
    }
}
