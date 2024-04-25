// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis;

internal sealed partial class TrivialTemporaryStorageService
{
    private sealed class TrivialStorageStreamHandle(
        TemporaryStorageIdentifier storageIdentifier,
        MemoryStream streamCopy) : ITemporaryStorageStreamHandle
    {
        public TemporaryStorageIdentifier Identifier => storageIdentifier;

        public Stream ReadFromTemporaryStorage(CancellationToken cancellationToken)
        {
            // Return a read-only view of the underlying buffer to prevent users from overwriting or directly
            // disposing the backing storage.
            return new MemoryStream(streamCopy.GetBuffer(), 0, (int)streamCopy.Length, writable: false);
        }
    }
}
