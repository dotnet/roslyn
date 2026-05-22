// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime

// Note: This type was introduced in .NET Core 3.0.

#if !NETCOREAPP3_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>Represent a range has start and end indexes.</summary>
/// <remarks>
/// Range is used by the C# compiler to support the range syntax.
/// <code>
/// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
/// int[] subArray1 = someArray[0..2]; // { 1, 2 }
/// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
/// </code>
/// </remarks>
internal readonly struct Range : IEquatable<Range>
{
    /// <summary>Represent the inclusive start index of the Range.</summary>
    public Index Start { get; }

    /// <summary>Represent the exclusive end index of the Range.</summary>
    public Index End { get; }

    /// <summary>Construct a Range object using the start and end indexes.</summary>
    /// <param name="start">Represent the inclusive start index of the range.</param>
    /// <param name="end">Represent the exclusive end index of the range.</param>
    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Indicates whether the current Range object is equal to another object of the same type.</summary>
    /// <param name="value">An object to compare with this object</param>
    public override bool Equals([NotNullWhen(true)] object? value)
        => value is Range r
        && r.Start.Equals(Start)
        && r.End.Equals(End);

    /// <summary>Indicates whether the current Range object is equal to another Range object.</summary>
    /// <param name="other">An object to compare with this object</param>
    public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

    /// <summary>Returns the hash code for this instance.</summary>
    public override int GetHashCode()
        => Hash.Combine(Start.GetHashCode(), End.GetHashCode());

    /// <summary>Converts the value of the current Range object to its equivalent string representation.</summary>
    public override string ToString()
        => Start.ToString() + ".." + End.ToString();

    /// <summary>Create a Range object starting from start index to the end of the collection.</summary>
    public static Range StartAt(Index start) => new(start, Index.End);

    /// <summary>Create a Range object starting from first element in the collection to the end Index.</summary>
    public static Range EndAt(Index end) => new(Index.Start, end);

    /// <summary>Create a Range object starting from first element to the end.</summary>
    public static Range All => new(Index.Start, Index.End);

    /// <summary>Calculate the start offset and length of range object using a collection length.</summary>
    /// <param name="length">The length of the collection that the range will be used with. length has to be a positive value.</param>
    /// <remarks>
    /// For performance reason, we don't validate the input length parameter against negative values.
    /// It is expected Range will be used with collections which always have non negative length/count.
    /// We validate the range is inside the length scope though.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        var startIndex = Start;
        var start = startIndex.IsFromEnd ? length - startIndex.Value : startIndex.Value;

        var endIndex = End;
        var end = endIndex.IsFromEnd ? length - endIndex.Value : endIndex.Value;

        if ((uint)end > (uint)length || (uint)start > (uint)end)
        {
            ThrowArgumentOutOfRange(nameof(length));
        }

        return (start, end - start);
    }

    [DoesNotReturn]
    private static void ThrowArgumentOutOfRange(string? paramName)
        => throw new ArgumentOutOfRangeException(paramName);

    private static class Hash
    {
        public static int Combine(int newKey, int currentKey)
            => unchecked((currentKey * (int)0xA5555529) + newKey);

        public static int Combine(bool newKeyPart, int currentKey)
            => Combine(currentKey, newKeyPart ? 1 : 0);
    }
}

#else

using System;
using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(Range))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#endif
