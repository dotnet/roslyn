// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class ContentHashComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
    public static ContentHashComparer Instance { get; } = new ContentHashComparer();

    private ContentHashComparer() { }

    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
    {
        return x.Span.SequenceEqual(y.Span);
    }

    public int GetHashCode(ReadOnlyMemory<byte> obj)
    {
        // We expect the content hash to be well-mixed.
        // Therefore simply reading the first 4 bytes of it results in an adequate hash code.
        return BinaryPrimitives.ReadInt32LittleEndian(obj.Span);
    }
}
