// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

                var fallbackOptions = GetClientOptionsProvider(callbackId);

                var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(
                    symbol, solution, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

                return new SerializableRenameLocations(
                    options,
                    renameLocations.Locations.Select(loc => SerializableRenameLocation.Dehydrate(loc)).ToArray(),
                    renameLocations.ImplicitLocations.IsDefault ? null : renameLocations.ImplicitLocations.Select(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)).ToArray(),
                    renameLocations.ReferencedSymbols.IsDefault ? null : renameLocations.ReferencedSymbols.Select(sym => SerializableSymbolAndProjectId.Dehydrate(solution, sym, cancellationToken)).ToArray());
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
                if (locations == null)
                    return null;

                var result = await ConflictResolver.ResolveHeavyweightConflictsInCurrentProcessAsync(
                    locations, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
