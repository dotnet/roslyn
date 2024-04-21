// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
    /// will not longer be readable if the returned <see cref="TemporaryStorageHandle"/> is disposed.
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
    /// Note: The stream provided mus support <see cref="Stream.Length"/>.  The stream will also be reset to <see
    /// cref="Stream.Position"/> <code>0</code> within this method.  The caller does not need to reset the stream
    /// itself.
    /// </remarks>
    TemporaryStorageHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the data indicated to by the <paramref name="storageIdentifier"/> into a stream.  This stream can be
    /// created in a different process than the one that wrote the data originally.
    /// </summary>
    Stream ReadFromTemporaryStorageService(TemporaryStorageIdentifier storageIdentifier, CancellationToken cancellationToken);

    ITemporaryTextStorageInternal CreateTemporaryTextStorage();
}
