// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Represents a handle to data stored to temporary storage (generally a memory mapped file).  As long as this handle is
/// alive, the data should remain in storage and can be readable from any process using the information provided in <see
/// cref="Identifier"/>.  Use <see cref="ITemporaryStorageServiceInternal.WriteToTemporaryStorage"/> to write the data
/// to temporary storage and get a handle to it.  Use <see
/// cref="ITemporaryStorageServiceInternal.ReadFromTemporaryStorageService"/> to read the data back in any process.
/// </summary>
internal sealed class TemporaryStorageHandle(MemoryMappedFile? memoryMappedFile, TemporaryStorageIdentifier identifier)
{
#pragma warning disable IDE0052 // Remove unread private members
    /// <summary>
    /// This field is intentionally not read.  It exists just to root the memory mapped file and keep it alive as long
    /// as this handle is alive.
    /// </summary>
    private readonly MemoryMappedFile? _memoryMappedFile = memoryMappedFile;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly TemporaryStorageIdentifier? _identifier = identifier;

    public TemporaryStorageIdentifier Identifier => _identifier ?? throw new InvalidOperationException("Handle has already been disposed");
}
