// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteRenamerService(in BrokeredServiceBase.ServiceConstructionArguments arguments, RemoteCallback<IRemoteRenamerService.ICallback> callback)
        : BrokeredServiceBase(arguments), IRemoteRenamerService
    {
        internal sealed class Factory : FactoryBase<IRemoteRenamerService, IRemoteRenamerService.ICallback>
        {
            protected override IRemoteRenamerService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteRenamerService.ICallback> callback)
                => new RemoteRenamerService(arguments, callback);
        }

        public ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string newName,
            SymbolRenameOptions options,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return null;

                var fallbackOptions = GetClientOptionsProvider<CodeCleanupOptions, IRemoteRenamerService.ICallback>(callback, callbackId).ToCodeCleanupOptionsProvider();

                var result = await Renamer.RenameSymbolAsync(
                    solution, symbol, newName, options, fallbackOptions, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);

                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
            Checksum solutionChecksum,
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

                var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(
                    symbol, solution, options, cancellationToken).ConfigureAwait(false);

                return new SerializableRenameLocations(
                    options,
                    renameLocations.Locations.SelectAsArray(SerializableRenameLocation.Dehydrate),
                    renameLocations.ImplicitLocations.SelectAsArray(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)),
                    renameLocations.ReferencedSymbols.SelectAsArray(sym => SerializableSymbolAndProjectId.Dehydrate(solution, sym, cancellationToken)));
            }, cancellationToken);
        }

        public ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
            Checksum solutionChecksum,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameLocations serializableLocations,
            string replacementText,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var symbol = await symbolAndProjectId.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (symbol is null)
                    return null;

                var locations = await SymbolicRenameLocations.TryRehydrateAsync(
                    symbol, solution, serializableLocations, cancellationToken).ConfigureAwait(false);
                if (locations is null)
                    return null;

                var fallbackOptions = GetClientOptionsProvider<CodeCleanupOptions, IRemoteRenamerService.ICallback>(callback, callbackId).ToCodeCleanupOptionsProvider();

                var result = await ConflictResolver.ResolveSymbolicLocationConflictsInCurrentProcessAsync(
                    locations, replacementText, nonConflictSymbolKeys, fallbackOptions, cancellationToken).ConfigureAwait(false);
                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
