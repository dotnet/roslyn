// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Host;

internal sealed partial class TemporaryStorageService
{
    public sealed class TemporaryStorageStreamHandle(
        MemoryMappedFile memoryMappedFile,
        TemporaryStorageIdentifier identifier)
        : ITemporaryStorageStreamHandle
    {
        public TemporaryStorageIdentifier Identifier => identifier;

        public Stream ReadFromTemporaryStorage(CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadStream, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var info = new MemoryMappedInfo(memoryMappedFile, Identifier.Name, Identifier.Offset, Identifier.Size);
                return info.CreateReadableStream();
            }
        }
    }
}
