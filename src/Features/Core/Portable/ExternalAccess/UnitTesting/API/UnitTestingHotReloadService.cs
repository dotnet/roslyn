﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal sealed class UnitTestingHotReloadService
    {
        private sealed class DebuggerService : IManagedHotReloadService
        {
            private readonly ImmutableArray<string> _capabilities;

            public DebuggerService(ImmutableArray<string> capabilities)
            {
                _capabilities = capabilities;
            }

            public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

            public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.Available));

            public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(_capabilities);

            public ValueTask PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
                => ValueTaskFactory.CompletedTask;
        }

        public readonly struct Update
        {
            public readonly Guid ModuleId;
            public readonly ImmutableArray<byte> ILDelta;
            public readonly ImmutableArray<byte> MetadataDelta;
            public readonly ImmutableArray<byte> PdbDelta;
            public readonly ImmutableArray<int> UpdatedMethods;
            public readonly ImmutableArray<int> UpdatedTypes;

            public Update(
                Guid moduleId,
                ImmutableArray<byte> ilDelta,
                ImmutableArray<byte> metadataDelta,
                ImmutableArray<byte> pdbDelta,
                ImmutableArray<int> updatedMethods,
                ImmutableArray<int> updatedTypes)
            {
                ModuleId = moduleId;
                ILDelta = ilDelta;
                MetadataDelta = metadataDelta;
                PdbDelta = pdbDelta;
                UpdatedMethods = updatedMethods;
                UpdatedTypes = updatedTypes;
            }
        }

        private static readonly ActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
            (_, _, _) => ValueTaskFactory.FromResult(ImmutableArray<ActiveStatementSpan>.Empty);

        private static readonly ImmutableArray<Update> EmptyUpdate = ImmutableArray.Create<Update>();
        private static readonly ImmutableArray<Diagnostic> EmptyDiagnostic = ImmutableArray.Create<Diagnostic>();

        private readonly IEditAndContinueWorkspaceService _encService;
        private DebuggingSessionId _sessionId;

        public UnitTestingHotReloadService(HostWorkspaceServices services)
            => _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>();

        /// <summary>
        /// Starts the watcher.
        /// </summary>
        /// <param name="solution">Solution that represents sources that match the built binaries on disk.</param>
        /// <param name="capabilities">Array of capabilities retrieved from the runtime to dictate supported rude edits.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartSessionAsync(Solution solution, ImmutableArray<string> capabilities, CancellationToken cancellationToken)
        {
            var newSessionId = await _encService.StartDebuggingSessionAsync(
                solution,
                new DebuggerService(capabilities),
                captureMatchingDocuments: ImmutableArray<DocumentId>.Empty,
                captureAllMatchingDocuments: true,
                reportDiagnostics: false,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfFalse(_sessionId == default, "Session already started");
            _sessionId = newSessionId;
        }

        /// <summary>
        /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call 
        /// where <paramref name="commitUpdates"/> was `true` or the one passed to <see cref="StartSessionAsync(Solution, ImmutableArray{string}, CancellationToken)"/>
        /// for the first invocation.
        /// </summary>
        /// <param name="solution">Solution snapshot.</param>
        /// <param name="commitUpdates">commits changes if true, discards if false</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Updates (one for each changed project) and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
        /// </returns>
        public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, bool commitUpdates, CancellationToken cancellationToken)
        {
            var sessionId = _sessionId;
            Contract.ThrowIfFalse(sessionId != default, "Session has not started");

            var results = await _encService
                .EmitSolutionUpdateAsync(sessionId, solution, s_solutionActiveStatementSpanProvider, cancellationToken)
                .ConfigureAwait(false);

            if (results.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                if (commitUpdates)
                {
                    _encService.CommitSolutionUpdate(sessionId, out _);
                }
                else
                {
                    _encService.DiscardSolutionUpdate(sessionId);
                }
            }

            if (results.SyntaxError is not null)
            {
                // We do not need to acquire any updates or other
                // diagnostics if there is a syntax error.
                return (EmptyUpdate, EmptyDiagnostic.Add(results.SyntaxError));
            }

            var updates = results.ModuleUpdates.Updates.SelectAsArray(
                update => new Update(
                    update.Module,
                    update.ILDelta,
                    update.MetadataDelta,
                    update.PdbDelta,
                    update.UpdatedMethods,
                    update.UpdatedTypes));

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
            private readonly UnitTestingHotReloadService _instance;

            internal TestAccessor(UnitTestingHotReloadService instance)
                => _instance = instance;

            public DebuggingSessionId SessionId
                => _instance._sessionId;
        }
    }
}
