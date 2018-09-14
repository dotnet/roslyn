// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// When we are generating literals, we sometimes want to emit code vs. the numeric literal. This class
    /// gives the constants for all ones we want to convert
    /// </summary>
    internal static class LiteralSpecialValues
    {
        // Let's not have special values for byte. byte.MaxValue seems overkill versus 255.
        public static readonly IEnumerable<KeyValuePair<byte, string>> ByteSpecialValues = new Dictionary<byte, string>();

        public static readonly IEnumerable<KeyValuePair<sbyte, string>> SByteSpecialValues = new Dictionary<sbyte, string>()
        {
            { sbyte.MinValue, nameof(sbyte.MinValue) },
            { sbyte.MaxValue, nameof(sbyte.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<short, string>> Int16SpecialValues = new Dictionary<short, string>()
        {
            { short.MinValue, nameof(short.MinValue) },
            { short.MaxValue, nameof(short.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<ushort, string>> UInt16SpecialValues = new Dictionary<ushort, string>()
        {
            { ushort.MaxValue, nameof(ushort.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<int, string>> Int32SpecialValues = new Dictionary<int, string>()
        {
            { int.MinValue, nameof(int.MinValue) },
            { int.MaxValue, nameof(int.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<uint, string>> UInt32SpecialValues = new Dictionary<uint, string>()
        {
            { uint.MaxValue, nameof(uint.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<long, string>> Int64SpecialValues = new Dictionary<long, string>()
        {
            { long.MinValue, nameof(long.MinValue) },
            { long.MaxValue, nameof(long.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<ulong, string>> UInt64SpecialValues = new Dictionary<ulong, string>()
        {
            { ulong.MaxValue, nameof(ulong.MaxValue) },
        };

        public static readonly IEnumerable<KeyValuePair<float, string>> SingleSpecialValues = new Dictionary<float, string>()
        {
            { float.MinValue, nameof(float.MinValue) },
            { float.MaxValue, nameof(float.MaxValue) },
            { float.Epsilon, nameof(float.Epsilon) },
            { float.NaN, nameof(float.NaN) },
            { float.NegativeInfinity, nameof(float.NegativeInfinity) },
            { float.PositiveInfinity, nameof(float.PositiveInfinity) },
        };

        public static readonly IEnumerable<KeyValuePair<double, string>> DoubleSpecialValues = new Dictionary<double, string>()
        {
            { double.MinValue, nameof(double.MinValue) },
            { double.MaxValue, nameof(double.MaxValue) },
            { double.Epsilon, nameof(double.Epsilon) },
            { double.NaN, nameof(double.NaN) },
            { double.NegativeInfinity, nameof(double.NegativeInfinity) },
            { double.PositiveInfinity, nameof(double.PositiveInfinity) },
        };

        public static readonly IEnumerable<KeyValuePair<decimal, string>> DecimalSpecialValues = new Dictionary<decimal, string>()
        {
            { decimal.MinValue, nameof(decimal.MinValue) },
            { decimal.MaxValue, nameof(decimal.MaxValue) },
        };
    }
}
