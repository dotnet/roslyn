// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

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

        public ValueTask KeepAliveAsync(
            Checksum solutionChecksum,
            CancellationToken cancellationToken)
        {
            // First get the solution, ensuring that it is currently pinned.
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                // Wait for our caller to tell us to cancel.  That way we can release this solution and allow it
                // to be collected if not needed anymore.
                //
                // This was provided by stoub as an idiomatic way to wait indefinitely until a cancellation token triggers.
                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
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

                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var result = await Renamer.RenameSymbolAsync(
                    solution, symbol, newName, options, fallbackOptions, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);

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

                var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(
                    symbol, solution, options, GetClientOptionsProvider(callbackId), cancellationToken).ConfigureAwait(false);

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
                    symbol, solution, GetClientOptionsProvider(callbackId), serializableLocations, cancellationToken).ConfigureAwait(false);
                if (locations is null)
                    return null;

                var result = await ConflictResolver.ResolveSymbolicLocationConflictsInCurrentProcessAsync(
                    locations, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
