// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal sealed partial class TrivialTemporaryStorageService : ITemporaryStorageServiceInternal
{
    public static readonly TrivialTemporaryStorageService Instance = new();

    private TrivialTemporaryStorageService()
    {
    }

    public ITemporaryStorageTextHandle WriteToTemporaryStorage(SourceText text, CancellationToken cancellationToken)
    {
        var identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString("N"), Offset: 0, Size: text.Length);
        var handle = new TrivialStorageTextHandle(identifier, text);
        return handle;
    }

    public Task<ITemporaryStorageTextHandle> WriteToTemporaryStorageAsync(SourceText text, CancellationToken cancellationToken)
        => Task.FromResult(WriteToTemporaryStorage(text, cancellationToken));

    public ITemporaryStorageStreamHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        var newStream = new MemoryStream();
        stream.CopyTo(newStream);
        newStream.Position = 0;

        var identifier = new TemporaryStorageIdentifier(Guid.NewGuid().ToString("N"), Offset: 0, Size: stream.Length);
        var handle = new TrivialStorageStreamHandle(identifier, newStream);
        return handle;
    }
}
