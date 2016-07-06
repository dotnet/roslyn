// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: currently, service hub provide no other way to share services between user service hub services.
    //       only way to do so is using static type
    internal class AssetService
    {
        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        private static readonly Serializer s_serializer = new Serializer(new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: "dummy").Services);

        private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
            new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, object> _assets =
            new ConcurrentDictionary<Checksum, object>(concurrencyLevel: 4, capacity: 10);

        public void Set(Checksum checksum, object @object)
        {
            // TODO: if checksum already exist, add some debug check to verify object is same thing
            //       currently, asset once added never get deleted. need to do lifetime management
            _assets.TryAdd(checksum, @object);
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum)
        {
            // TODO: need to figure out cancellation story.
            //       this require cancellation from both caller and provider of asset
            object @object;
            if (_assets.TryGetValue(checksum, out @object))
            {
                return (T)@object;
            }

            // TODO: what happen if service doesn't come back. timeout?
            await RequestAssetAsync(checksum).ConfigureAwait(false);

            if (!_assets.TryGetValue(checksum, out @object))
            {
                Contract.Fail("how this can happen?");
            }

            return (T)@object;
        }

        public async Task RequestAssetAsync(Checksum checksum)
        {
            // there must be one that knows about object with the checksum
            foreach (var kv in _assetSources)
            {
                var serviceId = kv.Key;
                var source = kv.Value;

                try
                {
                    // request asset to source
                    // we do this wierd stuff since service hub doesn't allow service to open new stream to client
                    await source.RequestAssetAsync(serviceId, checksum).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // TODO: we need better way than this
                    // connection is closed from other side.
                    continue;
                }

                break;
            }
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            return s_serializer.Deserialize<T>(kind, reader, cancellationToken);
        }

        public void RegisterAssetSource(int serviceId, AssetSource assetSource)
        {
            // TODO: do some lifetime management for assets we got
            Contract.ThrowIfFalse(_assetSources.TryAdd(serviceId, assetSource));
        }

        public void UnregisterAssetSource(int serviceId)
        {
            AssetSource dummy;
            _assetSources.TryRemove(serviceId, out dummy);
        }
    }
}

