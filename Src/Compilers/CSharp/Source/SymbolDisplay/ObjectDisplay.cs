// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;
using ExceptionUtilities = Roslyn.Utilities.ExceptionUtilities;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Displays a value in the C# style.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="T:Microsoft.CodeAnalysis.CSharp.SymbolDisplay"/> because we want to link this functionality into
    /// the Formatter project and we don't want it to be public there.
    /// </remarks>
    /// <seealso cref="T:Microsoft.CodeAnalysis.VisualBasic.Symbols.ObjectDisplay"/>
    internal static class ObjectDisplay
    {
        /// <summary>
        /// Returns a string representation of an object of primitive type.
        /// </summary>
        /// <param name="obj">A value to display as a string.</param>
        /// <param name="quoteStrings">Whether or not to quote string literals.</param>
        /// <param name="useHexadecimalNumbers">Whether or not to display integral literals in hexadecimal.</param>
        /// <returns>A string representation of an object of primitive type (or null if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <c>null</c>.
        /// </remarks>
        public static string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers)
        {
            if (obj == null)
            {
                return NullLiteral;
            }

            Type type = obj.GetType();
            if (type.GetTypeInfo().IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(int))
            {
                return FormatLiteral((int)obj, useHexadecimalNumbers);
            }

            if (type == typeof(string))
            {
                return FormatLiteral((string)obj, quoteStrings);
            }

            if (type == typeof(bool))
            {
                return FormatLiteral((bool)obj);
            }

            if (type == typeof(char))
            {
                return FormatLiteral((char)obj, quoteStrings);
            }

            if (type == typeof(byte))
            {
                return FormatLiteral((byte)obj, useHexadecimalNumbers);
            }

            if (type == typeof(short))
            {
                return FormatLiteral((short)obj, useHexadecimalNumbers);
            }

            if (type == typeof(long))
            {
                return FormatLiteral((long)obj, useHexadecimalNumbers);
            }

            if (type == typeof(double))
            {
                return FormatLiteral((double)obj);
            }

            if (type == typeof(ulong))
            {
                return FormatLiteral((ulong)obj, useHexadecimalNumbers);
            }

            if (type == typeof(uint))
            {
                return FormatLiteral((uint)obj, useHexadecimalNumbers);
            }

            if (type == typeof(ushort))
            {
                return FormatLiteral((ushort)obj, useHexadecimalNumbers);
            }

            if (type == typeof(sbyte))
            {
                return FormatLiteral((sbyte)obj, useHexadecimalNumbers);
            }

            if (type == typeof(float))
            {
                return FormatLiteral((float)obj);
            }

            if (type == typeof(decimal))
            {
                return FormatLiteral((decimal)obj);
            }

            return null;
        }

        internal static string NullLiteral
        {
            get
            {
                return "null";
            }
        }

        internal static string FormatLiteral(bool value)
        {
            return value ? "true" : "false";
        }

        internal static string FormatString(string str, char quote, bool escapeNonPrintable)
        {
            PooledStringBuilder pooledBuilder = null;
            StringBuilder sb = null;
            int lastEscape = -1;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char replaceWith = '\0';
                bool unicodeEscape = false;
                switch (c)
                {
                    case '\\':
                        replaceWith = c;
                        break;

                    case '\0':
                        replaceWith = '0';
                        break;

                    case '\r':
                        replaceWith = 'r';
                        break;

                    case '\n':
                        replaceWith = 'n';
                        break;

                    case '\t':
                        replaceWith = 't';
                        break;

                    case '\b':
                        replaceWith = 'b';
                        break;

                    case '\v':
                        replaceWith = 'v';
                        break;

                    default:
                        if (quote == c)
                        {
                            replaceWith = c;
                            break;
                        }

                        if (escapeNonPrintable)
                        {
                            switch (CharUnicodeInfo.GetUnicodeCategory(c))
                            {
                                case UnicodeCategory.OtherNotAssigned:
                                case UnicodeCategory.ParagraphSeparator:
                                case UnicodeCategory.Control:
                                    unicodeEscape = true;
                                    break;
                            }
                        }

                        break;
                }

                if (unicodeEscape || replaceWith != '\0')
                {
                    if (pooledBuilder == null)
                    {
                        pooledBuilder = PooledStringBuilder.GetInstance();
                        sb = pooledBuilder.Builder;
                        if (quote != 0)
                        {
                            sb.Append(quote);
                        }
                    }

                    sb.Append(str, lastEscape + 1, i - (lastEscape + 1));
                    sb.Append('\\');
                    if (unicodeEscape)
                    {
                        sb.Append('u');
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(replaceWith);
                    }

                    lastEscape = i;
                }
            }

            if (sb != null)
            {
                sb.Append(str, lastEscape + 1, str.Length - (lastEscape + 1));
                if (quote != 0)
                {
                    sb.Append(quote);
                }

                return pooledBuilder.ToStringAndFree();
            }

            switch (quote)
            {
                case '"': return String.Concat("\"", str, "\"");
                case '\'': return String.Concat("'", str, "'");
                case '\0': return str;
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Returns a C# string literal with the given value.
        /// </summary>
        /// <param name="value">The value that the resulting string literal should have.</param>
        /// <param name="quote">True to put (double) quotes around the string literal.</param>
        /// <returns>A string literal with the given value.</returns>
        /// <remarks>
        /// Escapes non-printable characters.
        /// </remarks>
        public static string FormatLiteral(string value, bool quote)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return FormatString(value, quote ? '"' : '\0', escapeNonPrintable: true);
        }

        /// <summary>
        /// Returns a C# character literal with the given value.
        /// </summary>
        /// <param name="c">The value that the resulting character literal should have.</param>
        /// <param name="quote">True to put (single) quotes around the character literal.</param>
        /// <returns>A character literal with the given value.</returns>
        /// <remarks>
        /// Escapes non-printable characters.
        /// </remarks>
        public static string FormatLiteral(char c, bool quote)
        {
            return FormatLiteral(c, quote, includeCodePoints: false, useHexadecimalNumbers: false);
        }

        /// <summary>
        /// Returns a C# character literal with the given value.
        /// </summary>
        /// <param name="c">The value that the resulting character literal should have.</param>
        /// <param name="quote">True to put (single) quotes around the character literal.</param>
        /// <param name="includeCodePoints">True to include the code point before the character literal.</param>
        /// <param name="useHexadecimalNumbers">True to use hexadecimal for the code point.  Ignored if <paramref name="includeCodePoints"/> is false.</param>
        /// <returns>A character literal with the given value.</returns>
        internal static string FormatLiteral(char c, bool quote, bool includeCodePoints, bool useHexadecimalNumbers)
        {
            var result = FormatString(c.ToString(), quote ? '\'' : '\0', escapeNonPrintable: !includeCodePoints);
            if (includeCodePoints)
            {
                var codepoint = useHexadecimalNumbers ? "0x" + ((int)c).ToString("x4") : ((int)c).ToString();
                return codepoint + " " + result;
            }

            return result;
        }

        internal static string FormatLiteral(sbyte value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                // Special Case: for sbyte and short, specifically, negatives are shown
                // with extra precision.
                return "0x" + (value >= 0 ? value.ToString("x2") : ((int)value).ToString("x8"));
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(byte value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x2");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(short value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                // Special Case: for sbyte and short, specifically, negatives are shown
                // with extra precision.
                return "0x" + (value >= 0 ? value.ToString("x4") : ((int)value).ToString("x8"));
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(ushort value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x4");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(int value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x8");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(uint value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x8");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(long value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x16");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(ulong value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x16");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        internal static string FormatLiteral(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        internal static string FormatLiteral(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}