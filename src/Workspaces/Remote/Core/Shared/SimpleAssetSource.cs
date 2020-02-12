﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Shared
{
    /// <summary>
    /// provide asset from given map at the creation
    /// </summary>
    internal sealed class SimpleAssetSource : AssetSource
    {
        private readonly IReadOnlyDictionary<Checksum, object> _map;

        public SimpleAssetSource(AssetStorage assetStorage, IReadOnlyDictionary<Checksum, object> map) :
            base(assetStorage)
        {
            _map = map;
        }

        public override Task<IList<(Checksum, object)>> RequestAssetsAsync(
            int serviceId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            var list = new List<(Checksum, object)>();

            foreach (var checksum in checksums)
            {
                if (_map.TryGetValue(checksum, out var data))
                {
                    list.Add((checksum, data));
                }
                else
                {
                    Contract.Fail($"Unable to find asset for {checksum}");
                }
            }

            return Task.FromResult<IList<(Checksum, object)>>(list);
        }

        public override Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            return SpecializedTasks.False;
        }
    }
}
