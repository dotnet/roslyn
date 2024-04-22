// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Identifier for a stream of data placed in a segment of temporary storage (generally a memory mapped file). Can be
/// used to identify that segment across processes, allowing for efficient sharing of data.
/// </summary>
internal sealed record TemporaryStorageIdentifier(
    string Name, long Offset, long Size)
{
    public static TemporaryStorageIdentifier ReadFrom(ObjectReader reader)
        => new(
            reader.ReadRequiredString(),
            reader.ReadInt64(),
            reader.ReadInt64());

    public void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteInt64(Offset);
        writer.WriteInt64(Size);
    }
}
