﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Watch.Api
{
    internal sealed class WatchHotReloadService
    {
        private sealed class DebuggerService : IManagedEditAndContinueDebuggerService
        {
            public static readonly DebuggerService Instance = new();

            public Task<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray<ManagedActiveStatementDebugInfo>.Empty);

            public Task<ManagedEditAndContinueAvailability> GetAvailabilityAsync(Guid module, CancellationToken cancellationToken)
                => Task.FromResult(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available));

            public Task PrepareModuleForUpdateAsync(Guid module, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        public readonly struct Update
        {
            public readonly Guid ModuleId;
            public readonly ImmutableArray<byte> ILDelta;
            public readonly ImmutableArray<byte> MetadataDelta;
            public readonly ImmutableArray<byte> PdbDelta;

            public Update(Guid moduleId, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta)
            {
                ModuleId = moduleId;
                ILDelta = ilDelta;
                MetadataDelta = metadataDelta;
                PdbDelta = pdbDelta;
            }
        }

        private static readonly SolutionActiveStatementSpanProvider s_solutionActiveStatementSpanProvider =
            (_, _) => ValueTaskFactory.FromResult(ImmutableArray<TextSpan>.Empty);

        private readonly IEditAndContinueWorkspaceService _encService;

        public WatchHotReloadService(HostWorkspaceServices services)
            => _encService = services.GetRequiredService<IEditAndContinueWorkspaceService>();

        /// <summary>
        /// Starts the watcher.
        /// </summary>
        /// <param name="solution">Solution that represents sources that match the built binaries on disk.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        public async Task StartSessionAsync(Solution solution, CancellationToken cancellationToken)
            => await _encService.StartDebuggingSessionAsync(solution, captureMatchingDocuments: true, cancellationToken).ConfigureAwait(false);

        public async Task<(ImmutableArray<Update> updates, ImmutableArray<Diagnostic> diagnostics)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            _encService.StartEditSession(DebuggerService.Instance, out _);

            var results = await _encService.EmitSolutionUpdateAsync(solution, s_solutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            if (results.ModuleUpdates.Status == ManagedModuleUpdateStatus.Ready)
            {
                _encService.CommitSolutionUpdate();
            }

            _encService.EndEditSession(out _);

            if (results.ModuleUpdates.Status == ManagedModuleUpdateStatus.Blocked)
            {
                return default;
            }

            var updates = results.ModuleUpdates.Updates.SelectAsArray(
                update => new Update(update.Module, update.ILDelta, update.MetadataDelta, update.PdbDelta));

            var diagnostics = results.Diagnostics.SelectMany(d => d.Diagnostic).ToImmutableArray();

            return (updates, diagnostics);
        }

        public void EndSession()
            => _encService.EndDebuggingSession(out _);
    }
}
