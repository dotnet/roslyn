// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Identifier for a stream of data placed in a segment of a memory mapped file. Can be used to identify that segment
/// across processes (where supported), allowing for efficient sharing of data.
/// </summary>
/// <param name="Name">The name of the segment in the temporary storage.  <see langword="null"/> on platforms that don't
/// support cross process sharing of named memory mapped files.</param>
internal sealed record TemporaryStorageIdentifier(
    string? Name, long Offset, long Size)
{
    public static TemporaryStorageIdentifier ReadFrom(ObjectReader reader)
        => new(
            reader.ReadString(),
            reader.ReadInt64(),
            reader.ReadInt64());

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteInt64(Offset);
        writer.WriteInt64(Size);
    }
}
