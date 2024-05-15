﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A collection of value set factory instances for built-in types.
    /// </summary>
    internal static partial class ValueSetFactory
    {
        internal static readonly IValueSetFactory<byte> ForByte = new NumericValueSetFactory<byte>(ByteTC.Instance);
        internal static readonly IValueSetFactory<sbyte> ForSByte = new NumericValueSetFactory<sbyte>(SByteTC.Instance);
        internal static readonly IValueSetFactory<char> ForChar = new NumericValueSetFactory<char>(CharTC.Instance);
        internal static readonly IValueSetFactory<short> ForShort = new NumericValueSetFactory<short>(ShortTC.Instance);
        internal static readonly IValueSetFactory<ushort> ForUShort = new NumericValueSetFactory<ushort>(UShortTC.Instance);
        internal static readonly IValueSetFactory<int> ForInt = new NumericValueSetFactory<int>(IntTC.DefaultInstance);
        internal static readonly IValueSetFactory<uint> ForUInt = new NumericValueSetFactory<uint>(UIntTC.Instance);
        internal static readonly IValueSetFactory<long> ForLong = new NumericValueSetFactory<long>(LongTC.Instance);
        internal static readonly IValueSetFactory<ulong> ForULong = new NumericValueSetFactory<ulong>(ULongTC.Instance);
        internal static readonly IValueSetFactory<bool> ForBool = BoolValueSetFactory.Instance;
        internal static readonly IValueSetFactory<float> ForFloat = new FloatingValueSetFactory<float>(SingleTC.Instance);
        internal static readonly IValueSetFactory<double> ForDouble = new FloatingValueSetFactory<double>(DoubleTC.Instance);
        internal static readonly IValueSetFactory<string> ForString = new EnumeratedValueSetFactory<string>(StringTC.Instance);
        internal static readonly IValueSetFactory<decimal> ForDecimal = DecimalValueSetFactory.Instance;
        internal static readonly IValueSetFactory<int> ForNint = NintValueSetFactory.Instance;
        internal static readonly IValueSetFactory<uint> ForNuint = NuintValueSetFactory.Instance;
        internal static readonly IValueSetFactory<int> ForLength = NonNegativeIntValueSetFactory.Instance;

        public static IValueSetFactory? ForSpecialType(SpecialType specialType, bool isNative = false)
        {
            return specialType switch
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
                SpecialType.System_IntPtr when isNative => ForNint,
                SpecialType.System_UIntPtr when isNative => ForNuint,
                _ => null,
            };
        }

        public static IValueSetFactory? ForType(TypeSymbol type)
        {
            if (type.IsSpanOrReadOnlySpanChar())
                return ForString;
            type = type.EnumUnderlyingTypeOrSelf();
            return ForSpecialType(type.SpecialType, type.IsNativeIntegerType);
        }

        public static IValueSetFactory? ForInput(BoundDagTemp input)
        {
            if (input.Source is BoundDagPropertyEvaluation { IsLengthOrCount: true })
                return ForLength;
            return ForType(input.Type);
        }
    }
}
