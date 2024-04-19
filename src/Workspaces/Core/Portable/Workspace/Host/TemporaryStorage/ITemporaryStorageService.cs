// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

[Obsolete("API is no longer available")]
public interface ITemporaryStorageService : IWorkspaceService
{
    ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken = default);
    ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken = default);
}

internal interface ITemporaryStorageServiceInternal : IWorkspaceService
{
    TemporaryStorageHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken);
    Stream ReadFromTemporaryStorageService(TemporaryStorageIdentifier storageIdentifier, CancellationToken cancellationToken);

    ITemporaryTextStorageInternal CreateTemporaryTextStorage();
}

/// <summary>
/// Represents a handle to data stored to temporary storage (generally a memory mapped file).  As long as this handle is
/// not disposed, the data should remain in storage and can be readable from any process using the information provided
/// in <see cref="Identifier"/>.  Use <see cref="ITemporaryStorageServiceInternal.WriteToTemporaryStorage"/> to write
/// the data to temporary storage and get a handle to it.  Use <see
/// cref="ITemporaryStorageServiceInternal.ReadFromTemporaryStorageService"/> to read the data back in any process.
/// </summary>
internal sealed class TemporaryStorageHandle : IDisposable
{
    private IDisposable? _underlyingData;
    private TemporaryStorageIdentifier? _identifier;

    public TemporaryStorageIdentifier Identifier =>  _identifier ?? throw new InvalidOperationException("Handle has already been disposed");

    public void Dispose()
    {
        var data = Interlocked.Exchange(ref _underlyingData, null);
        data?.Dispose();
        _identifier = null;
    }
}

/// <summary>
/// Identifier for a stream of data placed in a segment of temporary storage (generally a memory mapped file). Can be
/// used to identify that segment across processes, allowing for efficient sharing of data.
/// </summary>
[DataContract]
internal sealed record TemporaryStorageIdentifier(
    [property: DataMember(Order = 0)] string Name,
    [property: DataMember(Order = 1)] int Offset,
    [property: DataMember(Order = 2)] int Length);
