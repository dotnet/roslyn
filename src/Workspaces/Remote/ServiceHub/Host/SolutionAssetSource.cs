﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetSource : IAssetSource
    {
        private readonly ServiceBrokerClient _client;

        public SolutionAssetSource(ServiceBrokerClient client)
        {
            _client = client;
        }

        public async ValueTask<ImmutableArray<object>> GetAssetsAsync(Checksum solutionChecksum, ImmutableArray<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
            await TaskScheduler.Default;

            return await RemoteCallback<ISolutionAssetProvider>.InvokeServiceAsync(
                _client,
                SolutionAssetProvider.ServiceDescriptor,
                (callback, cancellationToken) => callback.InvokeAsync(
                    (proxy, pipeWriter, cancellationToken) => proxy.WriteAssetsAsync(pipeWriter, solutionChecksum, checksums, cancellationToken),
                    (pipeReader, cancellationToken) => RemoteHostAssetSerialization.ReadDataAsync(pipeReader, solutionChecksum, checksums.Length, serializerService, cancellationToken),
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
