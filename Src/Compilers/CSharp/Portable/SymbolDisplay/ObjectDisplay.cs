// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
        /// <param name="options">Options used to customize formatting of an object value.</param>
        /// <returns>A string representation of an object of primitive type (or null if the type is not supported).</returns>
        /// <remarks>
        /// Handles <see cref="bool"/>, <see cref="string"/>, <see cref="char"/>, <see cref="sbyte"/>
        /// <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="long"/>, <see cref="ulong"/>, <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>,
        /// and <c>null</c>.
        /// </remarks>
        public static string FormatPrimitive(object obj, ObjectDisplayOptions options)
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
                return FormatLiteral((int)obj, options);
            }

            if (type == typeof(string))
            {
                return FormatLiteral((string)obj, options);
            }

            if (type == typeof(bool))
            {
                return FormatLiteral((bool)obj);
            }

            if (type == typeof(char))
            {
                return FormatLiteral((char)obj, options);
            }

            if (type == typeof(byte))
            {
                return FormatLiteral((byte)obj, options);
            }

            if (type == typeof(short))
            {
                return FormatLiteral((short)obj, options);
            }

            if (type == typeof(long))
            {
                return FormatLiteral((long)obj, options);
            }

            if (type == typeof(double))
            {
                return FormatLiteral((double)obj, options);
            }

            if (type == typeof(ulong))
            {
                return FormatLiteral((ulong)obj, options);
            }

            if (type == typeof(uint))
            {
                return FormatLiteral((uint)obj, options);
            }

            if (type == typeof(ushort))
            {
                return FormatLiteral((ushort)obj, options);
            }

            if (type == typeof(sbyte))
            {
                return FormatLiteral((sbyte)obj, options);
            }

            if (type == typeof(float))
            {
                return FormatLiteral((float)obj, options);
            }

            if (type == typeof(decimal))
            {
                return FormatLiteral((decimal)obj, options);
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
        /// <param name="options">Options used to customize formatting of an object value.</param>
        /// <returns>A string literal with the given value.</returns>
        /// <remarks>
        /// Escapes non-printable characters.
        /// </remarks>
        public static string FormatLiteral(string value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return FormatString(value, options.IncludesOption(ObjectDisplayOptions.UseQuotes) ? '"' : '\0', escapeNonPrintable: true);
        }

        /// <summary>
        /// Returns a C# character literal with the given value.
        /// </summary>
        /// <param name="c">The value that the resulting character literal should have.</param>
        /// <param name="options">Options used to customize formatting of an object value.</param>
        /// <returns>A character literal with the given value.</returns>
        internal static string FormatLiteral(char c, ObjectDisplayOptions options)
        {
            var includeCodePoints = options.IncludesOption(ObjectDisplayOptions.IncludeCodePoints);
            var result = FormatString(c.ToString(),
                                      quote: options.IncludesOption(ObjectDisplayOptions.UseQuotes) ? '\'' : '\0',
                                      escapeNonPrintable: !includeCodePoints);
            if (includeCodePoints)
            {
                var codepoint = options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) ? "0x" + ((int)c).ToString("x4") : ((int)c).ToString();
                return codepoint + " " + result;
            }

            return result;
        }

        internal static string FormatLiteral(sbyte value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
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

        internal static string FormatLiteral(byte value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                return "0x" + value.ToString("x2");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(short value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
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

        internal static string FormatLiteral(ushort value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                return "0x" + value.ToString("x4");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(int value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                return "0x" + value.ToString("x8");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static string FormatLiteral(uint value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var sb = pooledBuilder.Builder;

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                sb.Append("0x");
                sb.Append(value.ToString("x8"));
            }
            else
            {
                sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            if (options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix))
            {
                sb.Append('U');
            }

            return pooledBuilder.ToStringAndFree();
        }

        internal static string FormatLiteral(long value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var sb = pooledBuilder.Builder;

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                sb.Append("0x");
                sb.Append(value.ToString("x16"));
            }
            else
            {
                sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            if (options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix))
            {
                sb.Append('L');
            }

            return pooledBuilder.ToStringAndFree();
        }

        internal static string FormatLiteral(ulong value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var sb = pooledBuilder.Builder;

            if (options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers))
            {
                sb.Append("0x");
                sb.Append(value.ToString("x16"));
            }
            else
            {
                sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            if (options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix))
            {
                sb.Append("UL");
            }

            return pooledBuilder.ToStringAndFree();
        }

        internal static string FormatLiteral(double value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var result = value.ToString("R", CultureInfo.InvariantCulture);

            return options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) ? result + "D" : result;
        }

        internal static string FormatLiteral(float value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var result = value.ToString("R", CultureInfo.InvariantCulture);

            return options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) ? result + "F" : result;
        }

        internal static string FormatLiteral(decimal value, ObjectDisplayOptions options)
        {
            ValidateOptions(options);

            var result = value.ToString(CultureInfo.InvariantCulture);

            return options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) ? result + "M" : result;
        }

        [Conditional("DEBUG")]
        private static void ValidateOptions(ObjectDisplayOptions options)
        {
            // These options are mutually exclusive in C# unless we're formatting a char...should not be passed otherwise...
            Debug.Assert(!(options.IncludesOption(ObjectDisplayOptions.UseQuotes) && options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers)));
        }
    }
}