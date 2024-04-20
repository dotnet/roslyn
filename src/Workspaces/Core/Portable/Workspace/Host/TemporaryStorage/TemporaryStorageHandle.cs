// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

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

    public TemporaryStorageIdentifier Identifier => _identifier ?? throw new InvalidOperationException("Handle has already been disposed");

    public void Dispose()
    {
        var data = Interlocked.Exchange(ref _underlyingData, null);
        data?.Dispose();
        _identifier = null;
    }
}
