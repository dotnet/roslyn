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
            { sbyte.MinValue, "MinValue" },
            { sbyte.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<short, string>> Int16SpecialValues = new Dictionary<short, string>()
        {
            { short.MinValue, "MinValue" },
            { short.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<ushort, string>> UInt16SpecialValues = new Dictionary<ushort, string>()
        {
            { ushort.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<int, string>> Int32SpecialValues = new Dictionary<int, string>()
        {
            { int.MinValue, "MinValue" },
            { int.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<uint, string>> UInt32SpecialValues = new Dictionary<uint, string>()
        {
            { uint.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<long, string>> Int64SpecialValues = new Dictionary<long, string>()
        {
            { long.MinValue, "MinValue" },
            { long.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<ulong, string>> UInt64SpecialValues = new Dictionary<ulong, string>()
        {
            { ulong.MaxValue, "MaxValue" },
        };

        public static readonly IEnumerable<KeyValuePair<float, string>> SingleSpecialValues = new Dictionary<float, string>()
        {
            { float.MinValue, "MinValue" },
            { float.MaxValue, "MaxValue" },
            { float.Epsilon, "Epsilon" },
            { float.NaN, "NaN" },
            { float.NegativeInfinity, "NegativeInfinity" },
            { float.PositiveInfinity, "PositiveInfinity" },
        };

        public static readonly IEnumerable<KeyValuePair<double, string>> DoubleSpecialValues = new Dictionary<double, string>()
        {
            { double.MinValue, "MinValue" },
            { double.MaxValue, "MaxValue" },
            { double.Epsilon, "Epsilon" },
            { double.NaN, "NaN" },
            { double.NegativeInfinity, "NegativeInfinity" },
            { double.PositiveInfinity, "PositiveInfinity" },
        };

        public static readonly IEnumerable<KeyValuePair<decimal, string>> DecimalSpecialValues = new Dictionary<decimal, string>()
        {
            { decimal.MinValue, "MinValue" },
            { decimal.MaxValue, "MaxValue" },
        };
    }
}
