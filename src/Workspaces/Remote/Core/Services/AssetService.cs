// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service provide a way to get roslyn objects from checksum
    /// 
    /// TODO: change this service to workspace service
    /// </summary>
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

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            object @object;
            if (_assets.TryGetValue(checksum, out @object))
            {
                return (T)@object;
            }

            // TODO: what happen if service doesn't come back. timeout?
            await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

            if (!_assets.TryGetValue(checksum, out @object))
            {
                Contract.Fail("how this can happen?");
            }

            return (T)@object;
        }

        public async Task RequestAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            // the service doesn't care which asset source it uses to get the asset. if there are multiple
            // channel created (multiple caller to code analysis service), we will have multiple asset sources
            // 
            // but, there must be one that knows about the asset with the checksum that is pinned for this
            //      particular call
            foreach (var kv in _assetSources.ToArray())
            {
                var serviceId = kv.Key;
                var source = kv.Value;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // ask one of asset source for data
                    await source.RequestAssetAsync(serviceId, checksum, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // request is either cancelled or connection to the asset source has closed
                    Contract.ThrowIfFalse(ex is OperationCanceledException || ex is IOException || ex is ObjectDisposedException);

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
            Contract.ThrowIfFalse(_assetSources.TryAdd(serviceId, assetSource));
        }

        public void UnregisterAssetSource(int serviceId)
        {
            AssetSource dummy;
            _assetSources.TryRemove(serviceId, out dummy);
        }
    }
}

