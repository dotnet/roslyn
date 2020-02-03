// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A collection of value set factory instances for built-in types.
    /// </summary>
    internal static partial class ValueSetFactory
    {
        public static readonly IValueSetFactory<byte> ForByte = NumericValueSetFactory<byte, ByteTC>.Instance;
        public static readonly IValueSetFactory<sbyte> ForSByte = NumericValueSetFactory<sbyte, SByteTC>.Instance;
        public static readonly IValueSetFactory<char> ForChar = NumericValueSetFactory<char, CharTC>.Instance;
        public static readonly IValueSetFactory<short> ForShort = NumericValueSetFactory<short, ShortTC>.Instance;
        public static readonly IValueSetFactory<ushort> ForUShort = NumericValueSetFactory<ushort, UShortTC>.Instance;
        public static readonly IValueSetFactory<int> ForInt = NumericValueSetFactory<int, IntTC>.Instance;
        public static readonly IValueSetFactory<uint> ForUInt = NumericValueSetFactory<uint, UIntTC>.Instance;
        public static readonly IValueSetFactory<long> ForLong = NumericValueSetFactory<long, LongTC>.Instance;
        public static readonly IValueSetFactory<ulong> ForULong = NumericValueSetFactory<ulong, ULongTC>.Instance;
        public static readonly IValueSetFactory<bool> ForBool = BoolValueSetFactory.Instance;
        public static readonly IValueSetFactory<float> ForFloat = FloatingValueSetFactory<float, SingleTC>.Instance;
        public static readonly IValueSetFactory<double> ForDouble = FloatingValueSetFactory<double, DoubleTC>.Instance;
        public static readonly IValueSetFactory<string> ForString = EnumeratedValueSetFactory<string, StringTC>.Instance;
        public static readonly IValueSetFactory<decimal> ForDecimal = EnumeratedValueSetFactory<decimal, DecimalTC>.Instance;

        internal static IValueSetFactory? ForSpecialType(SpecialType specialType) => specialType switch
        {
            SpecialType.System_Byte => ForByte,
            SpecialType.System_SByte => ForSByte,
            SpecialType.System_Char => ForChar,
            SpecialType.System_Int16 => ForShort,
            SpecialType.System_UInt16 => ForUShort,
            SpecialType.System_Int32 => ForInt,
            SpecialType.System_UInt32 => ForUInt,
            SpecialType.System_Int64 => ForLong,
            SpecialType.System_UInt64 => ForULong,
            SpecialType.System_Boolean => ForBool,
            SpecialType.System_Single => ForFloat,
            SpecialType.System_Double => ForDouble,
            SpecialType.System_String => ForString,
            SpecialType.System_Decimal => ForDecimal,
            _ => null,
        };
    }
}
