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

/// <summary>
/// API to allow a client to write data to memory-mapped-file storage (allowing it to be shared across processes).
/// </summary>
internal interface ITemporaryStorageServiceInternal : IWorkspaceService
{
    /// <summary>
    /// Write the provided <paramref name="stream"/> to a new memory-mapped-file.  Returns a handle to the data that can
    /// be used to identify the data across processes allowing it to be read back in in any process.  Note: the data
    /// will not longer be readonable if the returned <see cref="TemporaryStorageHandle"/> is disposed.
    /// </summary>
    /// <remarks>
    /// This type is used for two purposes.  
    /// <list type="number">
    /// <item>
    /// Dumping metadata to disk.  This then allowing them to be read in by mapping
    /// their data into types like <see cref="AssemblyMetadata"/>.  It also allows them to be read in by our server
    /// process, without having to transmit the data over the wire.  For this use case, we never dispose of the handle,
    /// opting to keep things simple by having the host and server not have to coordinate on the lifetime of the data.
    /// </item>
    /// <item>
    /// Dumping large compiler command lines to disk to purge them from main memory.  Some of these strings are enormous
    /// (many MB large), and will get into the LOH.  This allows us to dump the data, knowing we can perfectly
    /// reconstruct it when needed.  In this case, we do dispose of the handle, as we don't need to keep the data around
    /// when we get the next large compiler command line.
    /// </item>
    /// </list>
    /// </remarks>
    TemporaryStorageHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the data indicated to by the <paramref name="storageIdentifier"/> into a stream.  This stream can be
    /// created in a different process than the one that wrote the data originally.
    /// </summary>
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
internal sealed class TemporaryStorageHandle(IDisposable underlyingData, TemporaryStorageIdentifier identifier) : IDisposable
{
    private IDisposable? _underlyingData = underlyingData;
    private TemporaryStorageIdentifier? _identifier = identifier;

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
    [property: DataMember(Order = 1)] long Offset,
    [property: DataMember(Order = 2)] long Size);
