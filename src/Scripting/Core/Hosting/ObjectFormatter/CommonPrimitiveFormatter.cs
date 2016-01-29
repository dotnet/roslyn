// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using static ObjectFormatterHelpers;

    internal abstract partial class CommonPrimitiveFormatter
    {
        /// <summary>
        /// String that describes "null" literal in the language.
        /// </summary>
        protected abstract string NullLiteral { get; }

        protected abstract string FormatLiteral(bool value);
        protected abstract string FormatLiteral(string value, bool quote, bool escapeNonPrintable, int numberRadix = NumberRadixDecimal);
        protected abstract string FormatLiteral(char value, bool quote, bool escapeNonPrintable, bool includeCodePoints = false, int numberRadix = NumberRadixDecimal);
        protected abstract string FormatLiteral(sbyte value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(byte value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(short value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(ushort value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(int value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(uint value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(long value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(ulong value, int numberRadix = NumberRadixDecimal, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(double value, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(float value, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(decimal value, CultureInfo cultureInfo = null);
        protected abstract string FormatLiteral(DateTime value, CultureInfo cultureInfo = null);

        /// <summary>
        /// Returns null if the type is not considered primitive in the target language.
        /// </summary>
        public string FormatPrimitive(object obj, CommonPrimitiveFormatterOptions options)
        {
            if (ReferenceEquals(obj, VoidValue))
            {
                return string.Empty;
            }

            if (obj == null)
            {
                return NullLiteral;
            }

            var type = obj.GetType();

            if (type.GetTypeInfo().IsEnum)
            {
                return obj.ToString();
            }

            switch (GetPrimitiveSpecialType(type))
            {
                case SpecialType.System_Int32:
                    return FormatLiteral((int)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_String:
                    return FormatLiteral((string)obj, options.QuoteStringsAndCharacters, options.EscapeNonPrintableCharacters, options.NumberRadix);

                case SpecialType.System_Boolean:
                    return FormatLiteral((bool)obj);

                case SpecialType.System_Char:
                    return FormatLiteral((char)obj, options.QuoteStringsAndCharacters, options.EscapeNonPrintableCharacters, options.IncludeCharacterCodePoints, options.NumberRadix);

                case SpecialType.System_Int64:
                    return FormatLiteral((long)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_Double:
                    return FormatLiteral((double)obj, options.CultureInfo);

                case SpecialType.System_Byte:
                    return FormatLiteral((byte)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_Decimal:
                    return FormatLiteral((decimal)obj, options.CultureInfo);

                case SpecialType.System_UInt32:
                    return FormatLiteral((uint)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_UInt64:
                    return FormatLiteral((ulong)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_Single:
                    return FormatLiteral((float)obj, options.CultureInfo);

                case SpecialType.System_Int16:
                    return FormatLiteral((short)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_UInt16:
                    return FormatLiteral((ushort)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_DateTime:
                    return FormatLiteral((DateTime)obj, options.CultureInfo);

                case SpecialType.System_SByte:
                    return FormatLiteral((sbyte)obj, options.NumberRadix, options.CultureInfo);

                case SpecialType.System_Object:
                case SpecialType.System_Void:
                case SpecialType.None:
                    return null;

                default:
                    throw ExceptionUtilities.UnexpectedValue(GetPrimitiveSpecialType(type));
            }
        }
    }
}
