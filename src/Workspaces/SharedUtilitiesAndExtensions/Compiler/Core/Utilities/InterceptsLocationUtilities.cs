// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <param name="ContentHash">Content hash of the original document the containing the invocation to be intercepted.
/// (See <see cref="SourceText.GetContentHash()"/>)</param>
/// <param name="Position">The position in the file of the invocation that was intercepted.  This is the absolute
/// start of the name token being invoked (e.g. <c>this.$$Goo(x, y, z)</c>) (see <see
/// cref="SyntaxToken.FullSpan"/>).</param>
internal record struct InterceptsLocationData(ImmutableArray<byte> ContentHash, int Position)
{
    public readonly bool Equals(InterceptsLocationData other)
        => Position == other.Position && ImmutableArrayComparer<byte>.Instance.Equals(ContentHash, other.ContentHash);

    public override readonly int GetHashCode()
        => Hash.Combine(ImmutableArrayComparer<byte>.Instance.GetHashCode(ContentHash), Position);
}

internal static class InterceptsLocationUtilities
{
    public static ImmutableArray<InterceptsLocationData> GetInterceptsLocationData(ImmutableArray<AttributeData> attributes)
    {
        using var result = TemporaryArray<InterceptsLocationData>.Empty;

        foreach (var attribute in attributes)
        {
            if (TryGetInterceptsLocationData(attribute, out var data))
                result.Add(data);
        }

        return result.ToImmutableAndClear();
    }

    public static bool TryGetInterceptsLocationData(AttributeData attribute, out InterceptsLocationData result)
    {
        if (attribute is
            {
                AttributeClass.Name: "InterceptsLocationAttribute",
                ConstructorArguments: [{ Value: int version }, { Value: string attributeData }]
            })
        {
            return TryGetInterceptsLocationData(version, attributeData, out result);
        }

        result = default;
        return false;
    }

    public static bool TryGetInterceptsLocationData(int version, string attributeData, out InterceptsLocationData result)
    {
        if (version == 1)
            return TryGetInterceptsLocationDataVersion1(attributeData, out result);

        // Add more supported versions here in the future if the compiler adds any.

        result = default;
        return false;
    }

    private static bool TryGetInterceptsLocationDataVersion1(string attributeData, out InterceptsLocationData result)
    {
        result = default;

        if (!Base64Utilities.TryGetDecodedLength(attributeData, out var decodedLength))
            return false;

        // V1 format:
        // - 16 bytes of target file content hash (xxHash128)
        // - int32 position (little endian)
        // - utf-8 display filename
        const int HashIndex = 0;
        const int HashSize = 16;
        const int PositionIndex = HashIndex + HashSize;
        const int PositionSize = sizeof(int);
        const int DisplayNameIndex = PositionIndex + PositionSize;
        const int MinLength = DisplayNameIndex;
        if (decodedLength < MinLength)
            return false;

        var rentedArray = decodedLength < 1024
            ? null
            : System.Buffers.ArrayPool<byte>.Shared.Rent(decodedLength);

        try
        {
            var bytes = rentedArray is null
                ? stackalloc byte[decodedLength]
                : rentedArray.AsSpan(0, decodedLength);

            if (!Base64Utilities.TryFromBase64Chars(attributeData.AsSpan(), bytes, out _))
                return false;

            var contentHash = bytes[HashIndex..HashSize].ToImmutableArray();
            var position = BinaryPrimitives.ReadInt32LittleEndian(bytes[PositionIndex..]);

            result = new(contentHash, position);
            return true;
        }
        finally
        {
            if (rentedArray is not null)
                System.Buffers.ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }
}
