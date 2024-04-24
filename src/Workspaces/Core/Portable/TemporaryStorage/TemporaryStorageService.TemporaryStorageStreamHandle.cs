// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

internal sealed partial class TemporaryStorageService
{
    public sealed class TemporaryStorageStreamHandle(
        TemporaryStorageService storageService, MemoryMappedFile memoryMappedFile, TemporaryStorageIdentifier identifier) : ITemporaryStorageStreamHandle
    {
        public TemporaryStorageIdentifier Identifier => identifier;

        Stream ITemporaryStorageStreamHandle.ReadFromTemporaryStorage(CancellationToken cancellationToken)
            => ReadFromTemporaryStorage(cancellationToken);

        public UnmanagedMemoryStream ReadFromTemporaryStorage(CancellationToken cancellationToken)
        {
            var storage = new TemporaryStreamStorage(
                storageService, memoryMappedFile, this.Identifier.Name, this.Identifier.Offset, this.Identifier.Size);
            return storage.ReadStream(cancellationToken);
        }
    }
}
