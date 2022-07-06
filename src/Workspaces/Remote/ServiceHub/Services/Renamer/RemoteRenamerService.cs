// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteRenamerService : BrokeredServiceBase, IRemoteRenamerService
    {
        internal sealed class Factory : FactoryBase<IRemoteRenamerService, IRemoteRenamerService.ICallback>
        {
            protected override IRemoteRenamerService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteRenamerService.ICallback> callback)
                => new RemoteRenamerService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteRenamerService.ICallback> _callback;

        public RemoteRenamerService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteRenamerService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        // TODO: Use generic IRemoteOptionsCallback<TOptions> once https://github.com/microsoft/vs-streamjsonrpc/issues/789 is fixed
        private CodeCleanupOptionsProvider GetClientOptionsProvider(RemoteServiceCallbackId callbackId)
            => new ClientCodeCleanupOptionsProvider(
                (callbackId, language, cancellationToken) => _callback.InvokeAsync((callback, cancellationToken) => callback.GetOptionsAsync(callbackId, language, cancellationToken), cancellationToken), callbackId);

        public ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string newName,
            SymbolRenameOptions options,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return null;

                var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);
                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var result = await Renamer.RenameSymbolAsync(
                    solution, symbol, newName, options, fallbackOptions,
                    nonConflictSymbols, cancellationToken).ConfigureAwait(false);

                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SymbolRenameOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return null;

                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var result = await RenameLocations.FindLocationsAsync(
                    symbol, solution, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

                return result.Dehydrate(solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);

                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var rehydratedSet = await RenameLocations.TryRehydrateAsync(solution, fallbackOptions, renameLocationSet, cancellationToken).ConfigureAwait(false);
                if (rehydratedSet == null)
                    return null;

                var result = await ConflictResolver.ResolveConflictsAsync(
                    rehydratedSet,
                    replacementText,
                    nonConflictSymbols,
                    cancellationToken).ConfigureAwait(false);
                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private static async Task<ImmutableHashSet<ISymbol>?> GetNonConflictSymbolsAsync(Solution solution, ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds, CancellationToken cancellationToken)
        {
            if (nonConflictSymbolIds.IsDefault)
            {
                return null;
            }

            var builder = ImmutableHashSet.CreateBuilder<ISymbol>();
            foreach (var id in nonConflictSymbolIds)
            {
                var symbol = await id.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (symbol != null)
                    builder.Add(symbol);
            }

            return builder.ToImmutable();
        }
    }
}
