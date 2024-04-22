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
    public readonly MemoryMappedFile? MemoryMappedFile = memoryMappedFile;
    private readonly TemporaryStorageIdentifier? _identifier = identifier;

    public TemporaryStorageIdentifier Identifier => _identifier ?? throw new InvalidOperationException("Handle has already been disposed");
}
