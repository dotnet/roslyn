// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A collection of value set factory instances for built-in types.
    /// </summary>
    internal static partial class ValueSetFactory
    {
        internal static readonly IValueSetFactory<byte> ForByte = NumericValueSetFactory<byte, ByteTC>.Instance;
        internal static readonly IValueSetFactory<sbyte> ForSByte = NumericValueSetFactory<sbyte, SByteTC>.Instance;
        internal static readonly IValueSetFactory<char> ForChar = NumericValueSetFactory<char, CharTC>.Instance;
        internal static readonly IValueSetFactory<short> ForShort = NumericValueSetFactory<short, ShortTC>.Instance;
        internal static readonly IValueSetFactory<ushort> ForUShort = NumericValueSetFactory<ushort, UShortTC>.Instance;
        internal static readonly IValueSetFactory<int> ForInt = NumericValueSetFactory<int, IntTC>.Instance;
        internal static readonly IValueSetFactory<uint> ForUInt = NumericValueSetFactory<uint, UIntTC>.Instance;
        internal static readonly IValueSetFactory<long> ForLong = NumericValueSetFactory<long, LongTC>.Instance;
        internal static readonly IValueSetFactory<ulong> ForULong = NumericValueSetFactory<ulong, ULongTC>.Instance;
        internal static readonly IValueSetFactory<bool> ForBool = BoolValueSetFactory.Instance;
        internal static readonly IValueSetFactory<float> ForFloat = FloatingValueSetFactory<float, SingleTC>.Instance;
        internal static readonly IValueSetFactory<double> ForDouble = FloatingValueSetFactory<double, DoubleTC>.Instance;
        internal static readonly IValueSetFactory<string> ForString = EnumeratedValueSetFactory<string, StringTC>.Instance;
        internal static readonly IValueSetFactory<decimal> ForDecimal = DecimalValueSetFactory.Instance;
        internal static readonly IValueSetFactory<int> ForNint = NintValueSetFactory.Instance;
        internal static readonly IValueSetFactory<uint> ForNuint = NuintValueSetFactory.Instance;

        public static IValueSetFactory? ForSpecialType(SpecialType specialType, bool isNative = false)
        {
            switch (specialType)
            {
                case SpecialType.System_Byte:
                    return ForByte;
                case SpecialType.System_SByte:
                    return ForSByte;
                case SpecialType.System_Char:
                    return ForChar;
                case SpecialType.System_Int16:
                    return ForShort;
                case SpecialType.System_UInt16:
                    return ForUShort;
                case SpecialType.System_Int32:
                    return ForInt;
                case SpecialType.System_UInt32:
                    return ForUInt;
                case SpecialType.System_Int64:
                    return ForLong;
                case SpecialType.System_UInt64:
                    return ForULong;
                case SpecialType.System_Boolean:
                    return ForBool;
                case SpecialType.System_Single:
                    return ForFloat;
                case SpecialType.System_Double:
                    return ForDouble;
                case SpecialType.System_String:
                    return ForString;
                case SpecialType.System_Decimal:
                    return ForDecimal;
                case SpecialType.System_IntPtr when isNative:
                    return ForNint;
                case SpecialType.System_UIntPtr when isNative:
                    return ForNuint;
                default:
                    return null;
            }
        }

        public static IValueSetFactory? ForType(TypeSymbol type)
        {
            type = type.EnumUnderlyingTypeOrSelf();
            return ForSpecialType(type.SpecialType, type.IsNativeIntegerType);
        }
    }
}
