// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;
using ExceptionUtilities = Roslyn.Utilities.ExceptionUtilities;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp
{
#pragma warning disable RS0010
    /// <summary>
    /// Displays a value in the C# style.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="T:Microsoft.CodeAnalysis.CSharp.SymbolDisplay"/> because we want to link this functionality into
    /// the Formatter project and we don't want it to be public there.
    /// </remarks>
    /// <seealso cref="T:Microsoft.CodeAnalysis.VisualBasic.Symbols.ObjectDisplay"/>
#pragma warning restore RS0010
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

        private static void FormatStringChar(StringBuilder builder, char c, char quote)
        {
            string replaceWith;
            if (TryReplaceChar(c, quote, out replaceWith))
            {
                builder.Append(replaceWith);
            }
            else
            {
                builder.Append(c);
            }
        }

        /// <summary>
        /// Returns true if the character should be replaced and sets
        /// <paramref name="replaceWith"/> to the replacement text if the
        /// character is replaced with text other than the Unicode escape sequence.
        /// </summary>
        private static bool TryReplaceChar(char c, char quote, out string replaceWith)
        {
            Debug.Assert(quote == '\0' || quote == '"' || quote == '\'');

            replaceWith = null;
            switch (c)
            {
                case '\\':
                    replaceWith = "\\\\";
                    break;
                case '"':
                    if (quote == c)
                    {
                        replaceWith = "\\\"";
                    }
                    break;
                case '\'':
                    if (quote == c)
                    {
                        replaceWith = "\\'";
                    }
                    break;
                case '\0':
                    replaceWith = "\\0";
                    break;
                case '\a':
                    replaceWith = "\\a";
                    break;
                case '\b':
                    replaceWith = "\\b";
                    break;
                case '\f':
                    replaceWith = "\\f";
                    break;
                case '\n':
                    replaceWith = "\\n";
                    break;
                case '\r':
                    replaceWith = "\\r";
                    break;
                case '\t':
                    replaceWith = "\\t";
                    break;
                case '\v':
                    replaceWith = "\\v";
                    break;
            }

            if (replaceWith != null)
            {
                return true;
            }

            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.Control:
                case UnicodeCategory.OtherNotAssigned:
                case UnicodeCategory.ParagraphSeparator:
                    replaceWith = "\\u" + ((int)c).ToString("x4");
                    return true;
                default:
                    return false;
            }
        }

        private static bool ReplaceAny(string s, char quote)
        {
            foreach (var c in s)
            {
                string replaceWith;
                if (TryReplaceChar(c, quote, out replaceWith))
                {
                    return true;
                }
            }
            return false;
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
                throw new ArgumentNullException(nameof(value));
            }

            var useQuotes = options.IncludesOption(ObjectDisplayOptions.UseQuotes);
            var escapeNonPrintable = options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableStringCharacters);
            var quote = useQuotes ? '"' : '\0';
            if (!useQuotes && !(escapeNonPrintable && ReplaceAny(value, quote)))
            {
                return value;
            }

            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            if (useQuotes)
            {
                builder.Append(quote);
            }
            if (escapeNonPrintable)
            {
                foreach (var c in value)
                {
                    FormatStringChar(builder, c, quote);
                }
            }
            else
            {
                builder.Append(value);
            }
            if (useQuotes)
            {
                builder.Append(quote);
            }
            return pooledBuilder.ToStringAndFree();
        }

        /// <summary>
        /// Returns a C# character literal with the given value.
        /// </summary>
        /// <param name="c">The value that the resulting character literal should have.</param>
        /// <param name="options">Options used to customize formatting of an object value.</param>
        /// <returns>A character literal with the given value.</returns>
        internal static string FormatLiteral(char c, ObjectDisplayOptions options)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            if (options.IncludesOption(ObjectDisplayOptions.IncludeCodePoints))
            {
                builder.Append(options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) ? "0x" + ((int)c).ToString("x4") : ((int)c).ToString());
                builder.Append(" ");
            }

            const char quote = '\'';

            string charString;
            if (!options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableStringCharacters) || !TryReplaceChar(c, quote, out charString))
            {
                charString = c.ToString();
            }

            if (options.IncludesOption(ObjectDisplayOptions.UseQuotes))
            {
                builder.Append(quote);
                builder.Append(charString);
                builder.Append(quote);
            }
            else
            {
                builder.Append(charString);
            }

            return pooledBuilder.ToStringAndFree();
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
