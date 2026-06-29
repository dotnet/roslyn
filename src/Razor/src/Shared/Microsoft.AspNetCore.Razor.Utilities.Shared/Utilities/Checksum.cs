// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

/// <summary>
///  Checksum of data can be used later to see whether two data are same or not
///  without actually comparing data itself.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = HashSize)]
internal readonly partial record struct Checksum(
    [field: FieldOffset(0)] long Data1,
    [field: FieldOffset(8)] long Data2) : IComparable<Checksum>
{
    /// <summary>
    ///  The intended size of the <see cref="Checksum"/> in bytes.
    /// </summary>
    private const int HashSize = 16;

#if !NET
    // Small (HashSize length), per-thread array to use when converting to base64 on non-.NET.
    [ThreadStatic]
    private static byte[]? s_bytes;
#endif

    /// <summary>
    ///  Represents a default/null/invalid Checksum, equivalent to <c>default(Checksum)</c>. This value
    ///  contains all zeroes, which is considered infinitesimally unlikely to ever happen from hashing data
    ///  (including when hashing null/empty/zero data inputs).
    /// </summary>
    public static readonly Checksum Null = default;

    public static Checksum From(byte[] bytes)
        => From(bytes.AsSpan());

    public static Checksum From(ImmutableArray<byte> bytes)
        => From(bytes.AsSpan());

    public static Checksum From(ReadOnlySpan<byte> bytes)
    {
        ArgHelper.ThrowIfLessThan(bytes.Length, HashSize);

        if (!MemoryMarshal.TryRead(bytes, out Checksum result))
        {
            return ThrowHelper.ThrowInvalidOperationException<Checksum>("Could not read hash data");
        }

        return result;
    }

    public string ToBase64String()
    {
#if NET
        Span<byte> bytes = stackalloc byte[HashSize];
        WriteTo(bytes);
        return Convert.ToBase64String(bytes);
#else
        var bytes = s_bytes ??= new byte[HashSize];
        WriteTo(bytes.AsSpan());
        return Convert.ToBase64String(bytes);
#endif
    }

    public static Checksum FromBase64String(string value)
        => From(Convert.FromBase64String(value));

    public void WriteTo(Span<byte> destination)
    {
        ArgHelper.ThrowIfDestinationTooShort(destination, HashSize);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
    }

    public bool Equals(Checksum other)
        => Data1 == other.Data1 &&
           Data2 == other.Data2;

    public override int GetHashCode()
    {
        // The checksum is already a hash. Just read a 4-byte value to get a well-distributed hash code.
        return (int)Data1;
    }

    public int CompareTo(Checksum other)
    {
        var result = Data1.CompareTo(other.Data1);
        return result != 0 ? result : Data2.CompareTo(other.Data2);
    }

    public override string ToString()
        => ToBase64String();
}
