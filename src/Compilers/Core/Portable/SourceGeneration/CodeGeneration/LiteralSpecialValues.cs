// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    /// <summary>
    /// When we are generating literals, we sometimes want to emit code vs. the numeric literal. This class
    /// gives the constants for all ones we want to convert
    /// </summary>
    internal static class LiteralSpecialValues
    {
        // Let's not have special values for byte. byte.MaxValue seems overkill versus 255.
        public static readonly ImmutableArray<(byte, string)> ByteSpecialValues = ImmutableArray<(byte, string)>.Empty;

        public static readonly ImmutableArray<(sbyte, string)> SByteSpecialValues = ImmutableArray.Create
        (
            (sbyte.MinValue, nameof(sbyte.MinValue)),
            (sbyte.MaxValue, nameof(sbyte.MaxValue))
        );

        public static readonly ImmutableArray<(short, string)> Int16SpecialValues = ImmutableArray.Create
        (
            (short.MinValue, nameof(short.MinValue)),
            (short.MaxValue, nameof(short.MaxValue))
        );

        public static readonly ImmutableArray<(ushort, string)> UInt16SpecialValues = ImmutableArray.Create
        (
            (ushort.MaxValue, nameof(ushort.MaxValue))
        );

        public static readonly ImmutableArray<(int, string)> Int32SpecialValues = ImmutableArray.Create
        (
            (int.MinValue, nameof(int.MinValue)),
            (int.MaxValue, nameof(int.MaxValue))
        );

        public static readonly ImmutableArray<(uint, string)> UInt32SpecialValues = ImmutableArray.Create
        (
            (uint.MaxValue, nameof(uint.MaxValue))
        );

        public static readonly ImmutableArray<(long, string)> Int64SpecialValues = ImmutableArray.Create
        (
            (long.MinValue, nameof(long.MinValue)),
            (long.MaxValue, nameof(long.MaxValue))
        );

        public static readonly ImmutableArray<(ulong, string)> UInt64SpecialValues = ImmutableArray.Create
        (
            (ulong.MaxValue, nameof(ulong.MaxValue))
        );

        public static readonly ImmutableArray<(float, string)> SingleSpecialValues = ImmutableArray.Create
        (
            (float.MinValue, nameof(float.MinValue)),
            (float.MaxValue, nameof(float.MaxValue)),
            (float.Epsilon, nameof(float.Epsilon)),
            (float.NaN, nameof(float.NaN)),
            (float.NegativeInfinity, nameof(float.NegativeInfinity)),
            (float.PositiveInfinity, nameof(float.PositiveInfinity))
        );

        public static readonly ImmutableArray<(double, string)> DoubleSpecialValues = ImmutableArray.Create
        (
            (double.MinValue, nameof(double.MinValue)),
            (double.MaxValue, nameof(double.MaxValue)),
            (double.Epsilon, nameof(double.Epsilon)),
            (double.NaN, nameof(double.NaN)),
            (double.NegativeInfinity, nameof(double.NegativeInfinity)),
            (double.PositiveInfinity, nameof(double.PositiveInfinity))
        );

        public static readonly ImmutableArray<(decimal, string)> DecimalSpecialValues = ImmutableArray.Create
        (
            (decimal.MinValue, nameof(decimal.MinValue)),
            (decimal.MaxValue, nameof(decimal.MaxValue))
        );
    }
}
