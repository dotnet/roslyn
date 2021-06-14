// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Watch.Api
{
    internal sealed class WatchHotReloadService
    {
        private sealed class DebuggerService : IManagedEditAndContinueDebuggerService
        {
            private readonly ImmutableArray<string> _capabilities;

            public DebuggerService(ImmutableArray<string> capabilities)
                => _capabilities = capabilities;

            public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

            public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
                => Task.FromResult(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available));

            public Task<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken) => Task.FromResult(_capabilities);

            public Task PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        public readonly struct Update
        {
            public readonly Guid ModuleId;
            public readonly ImmutableArray<byte> ILDelta;
            public readonly ImmutableArray<byte> MetadataDelta;
            public readonly ImmutableArray<byte> PdbDelta;
            public readonly ImmutableArray<int> UpdatedTypes;

            public Update(Guid moduleId, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta, ImmutableArray<int> updatedTypes)
            {
                ModuleId = moduleId;
                ILDelta = ilDelta;
                MetadataDelta = metadataDelta;
                PdbDelta = pdbDelta;
                UpdatedTypes = updatedTypes;
            }
        }

        private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
            (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        private readonly IEditAndContinueWorkspaceService _encService;
        private DebuggingSessionId _sessionId;
        private readonly ImmutableArray<string> _capabilities;

        public WatchHotReloadService(HostWorkspaceServices services, ImmutableArray<string> capabilities)
            => (_encService, _capabilities) = (services.GetRequiredService<IEditAndContinueWorkspaceService>(), capabilities);

        /// <summary>
        /// Starts the watcher.
        /// </summary>
        /// <param name="solution">Solution that represents sources that match the built binaries on disk.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
        {
            var newSessionId = await _encService.StartDebuggingSessionAsync(
                solution,
                new DebuggerService(_capabilities),
                captureMatchingDocuments: ImmutableArray<DocumentId>.Empty,
                captureAllMatchingDocuments: true,
                reportDiagnostics: false,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(_sessionId == default, "Session already started");
            _sessionId = newSessionId;
        }

        /// <summary>
        /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call or
        /// the one passed to <see cref="StartSessionAsync(Solution, CancellationToken)"/> for the first invocation.
        /// </summary>
        /// <param name="solution">Solution snapshot.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
        /// </returns>
        public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            var results = await _encService.EmitSolutionUpdateAsync(sessionId, solution, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            if (results.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                _encService.CommitSolutionUpdate(sessionId, out _);
            }

            var updates = results.ModuleUpdates.Updates.SelectAsArray(
                update => new Update(update.Module, update.ILDelta, update.MetadataDelta, update.PdbDelta, update.UpdatedTypes));

            var diagnostics = await results.GetAllDiagnosticsAsync(solution, cancellationToken).ConfigureAwait(false);

            return (updates, diagnostics);
        }

        public void EndSession()
        {
            Contract.ThrowIfFalse(_sessionId != default, "Session has not started");
            _encService.EndDebuggingSession(_sessionId, out _);
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly WatchHotReloadService _instance;

            internal TestAccessor(WatchHotReloadService instance)
                => _instance = instance;

            public DebuggingSessionId SessionId
                => _instance._sessionId;
        }
    }
}
