// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using static ObjectFormatterHelpers;

    public abstract partial class CommonPrimitiveFormatter
    {
        /// <summary>
        /// String that describes "null" literal in the language.
        /// </summary>
        protected abstract string NullLiteral { get; }

        protected abstract string FormatLiteral(bool value);
        protected abstract string FormatLiteral(string value, bool quote, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(char value, bool quote, bool includeCodePoints = false, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(sbyte value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(byte value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(short value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(ushort value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(int value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(uint value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(long value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(ulong value, bool useHexadecimalNumbers = false);
        protected abstract string FormatLiteral(double value);
        protected abstract string FormatLiteral(float value);
        protected abstract string FormatLiteral(decimal value);
        protected abstract string FormatLiteral(DateTime value);

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
                    return FormatLiteral((int)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_String:
                    return FormatLiteral((string)obj, options.QuoteStringsAndCharacters, options.UseHexadecimalNumbers);

                case SpecialType.System_Boolean:
                    return FormatLiteral((bool)obj);

                case SpecialType.System_Char:
                    return FormatLiteral((char)obj, options.QuoteStringsAndCharacters, options.IncludeCharacterCodePoints, options.UseHexadecimalNumbers);

                case SpecialType.System_Int64:
                    return FormatLiteral((long)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_Double:
                    return FormatLiteral((double)obj);

                case SpecialType.System_Byte:
                    return FormatLiteral((byte)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_Decimal:
                    return FormatLiteral((decimal)obj);

                case SpecialType.System_UInt32:
                    return FormatLiteral((uint)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_UInt64:
                    return FormatLiteral((ulong)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_Single:
                    return FormatLiteral((float)obj);

                case SpecialType.System_Int16:
                    return FormatLiteral((short)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_UInt16:
                    return FormatLiteral((ushort)obj, options.UseHexadecimalNumbers);

                case SpecialType.System_DateTime:
                    return FormatLiteral((DateTime)obj);

                case SpecialType.System_SByte:
                    return FormatLiteral((sbyte)obj, options.UseHexadecimalNumbers);

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
