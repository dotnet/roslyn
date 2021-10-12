// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Shared]
    [Export(typeof(ManagedEditAndContinueLanguageService))]
    [Export(typeof(IManagedEditAndContinueLanguageService))]
    [Export(typeof(IEditAndContinueSolutionProvider))]
    [ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedEditAndContinueLanguageService : IManagedEditAndContinueLanguageService, IEditAndContinueSolutionProvider
    {
        private readonly EditAndContinueLanguageService _encService;

        /// <summary>
        /// Import <see cref="IHostWorkspaceProvider"/> and <see cref="IManagedEditAndContinueDebuggerService"/> lazily so that the host does not need to implement them
        /// unless it implements debugger components.
        /// </summary>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedEditAndContinueLanguageService(
            Lazy<IHostWorkspaceProvider> workspaceProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            Lazy<IManagedEditAndContinueDebuggerService> debuggerService)
        {
            _encService = new EditAndContinueLanguageService(workspaceProvider, debuggerService, diagnosticService, diagnosticUpdateSource);
        }

        public EditAndContinueLanguageService Service => _encService;

        /// <summary>
        /// Called by the debugger when a debugging session starts and managed debugging is being used.
        /// </summary>
        public Task StartDebuggingAsync(DebugSessionFlags flags, CancellationToken cancellationToken)
        {
            if (flags.HasFlag(DebugSessionFlags.EditAndContinueDisabled))
            {
                _encService.Disable();
                return Task.CompletedTask;
            }

            return _encService.StartSessionAsync(cancellationToken).AsTask();
        }

        public Task EnterBreakStateAsync(CancellationToken cancellationToken)
            => _encService.EnterBreakStateAsync(cancellationToken).AsTask();

        public Task ExitBreakStateAsync(CancellationToken cancellationToken)
            => _encService.ExitBreakStateAsync(cancellationToken).AsTask();

        public Task CommitUpdatesAsync(CancellationToken cancellationToken)
            => _encService.CommitUpdatesAsync(cancellationToken).AsTask();

        public Task DiscardUpdatesAsync(CancellationToken cancellationToken)
            => _encService.DiscardUpdatesAsync(cancellationToken).AsTask();

        public Task StopDebuggingAsync(CancellationToken cancellationToken)
            => _encService.EndSessionAsync(cancellationToken).AsTask();

        public Task<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
            => _encService.HasChangesAsync(sourceFilePath, cancellationToken).AsTask();

        public Task<ManagedModuleUpdates> GetManagedModuleUpdatesAsync(CancellationToken cancellationToken)
            => _encService.GetEditAndContinueUpdatesAsync(cancellationToken).AsTask();

        public Task<SourceSpan?> GetCurrentActiveStatementPositionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
            => _encService.GetCurrentActiveStatementPositionAsync(instruction, cancellationToken).AsTask();

        public Task<bool?> IsActiveStatementInExceptionRegionAsync(ManagedInstructionId instruction, CancellationToken cancellationToken)
            => _encService.IsActiveStatementInExceptionRegionAsync(instruction, cancellationToken).AsTask();

        public event Action<Solution> SolutionCommitted
        {
            add
            {
                _encService.SolutionCommitted += value;
            }
            remove
            {
                _encService.SolutionCommitted -= value;
            }
        }
    }
}
