// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Represents a handle to data stored to temporary storage (generally a memory mapped file).  As long as this handle is
/// alive, the data should remain in storage and can be readable from any process using the information provided in <see
/// cref="Identifier"/>.  Use <see cref="ITemporaryStorageServiceInternal.WriteToTemporaryStorage(Stream,
/// CancellationToken)"/> to write the data to temporary storage and get a handle to it.  Use <see
/// cref="ReadFromTemporaryStorage"/> to read the data back in any process.
/// </summary>
internal interface ITemporaryStorageStreamHandle
{
    public TemporaryStorageIdentifier Identifier { get; }

    /// <summary>
    /// Reads the data indicated to by this handle into a stream.  This stream can be created in a different process
    /// than the one that wrote the data originally.
    /// </summary>
    UnmanagedMemoryStream ReadFromTemporaryStorage(CancellationToken cancellationToken);
}
