﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    [Shared]
    [Export(typeof(ManagedHotReloadLanguageService))]
    [Export(typeof(IManagedHotReloadLanguageService))]
    [Export(typeof(IEditAndContinueSolutionProvider))]
    [ExportMetadata("UIContext", EditAndContinueUIContext.EncCapableProjectExistsInWorkspaceUIContextString)]
    internal sealed class ManagedHotReloadLanguageService : IManagedHotReloadLanguageService, IEditAndContinueSolutionProvider
    {
        private sealed class DebuggerService : IManagedEditAndContinueDebuggerService
        {
            private readonly Lazy<IManagedHotReloadService> _hotReloadService;

            public DebuggerService(Lazy<IManagedHotReloadService> hotReloadService)
            {
                _hotReloadService = hotReloadService;
            }

            public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

            public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
                => Task.FromResult(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available));

            public Task<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
                => _hotReloadService.Value.GetCapabilitiesAsync(cancellationToken).AsTask();

            public Task PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        private readonly EditAndContinueLanguageService _encService;

        /// <summary>
        /// Import <see cref="IHostWorkspaceProvider"/> and <see cref="IManagedHotReloadService"/> lazily so that the host does not need to implement them
        /// unless it implements debugger components.
        /// </summary>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ManagedHotReloadLanguageService(
            Lazy<IHostWorkspaceProvider> workspaceProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            Lazy<IManagedHotReloadService> hotReloadService)
        {
            _encService = new EditAndContinueLanguageService(workspaceProvider, new Lazy<IManagedEditAndContinueDebuggerService>(() => new DebuggerService(hotReloadService)), diagnosticService, diagnosticUpdateSource);
        }

        public EditAndContinueLanguageService Service => _encService;

        public ValueTask StartSessionAsync(CancellationToken cancellationToken)
            => _encService.StartSessionAsync(cancellationToken);

        public ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
            => _encService.GetHotReloadUpdatesAsync(cancellationToken);

        public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
            => _encService.CommitUpdatesAsync(cancellationToken);

        public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
            => _encService.DiscardUpdatesAsync(cancellationToken);

        public ValueTask EndSessionAsync(CancellationToken cancellationToken)
            => _encService.EndSessionAsync(cancellationToken);

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
