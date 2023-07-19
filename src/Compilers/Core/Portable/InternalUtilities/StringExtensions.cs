// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class StringExtensions
    {
        private static ImmutableArray<string> s_lazyNumerals;

        internal static string GetNumeral(int number)
        {
            var numerals = s_lazyNumerals;
            if (numerals.IsDefault)
            {
                numerals = ImmutableArray.Create("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
                ImmutableInterlocked.InterlockedInitialize(ref s_lazyNumerals, numerals);
            }

            Debug.Assert(number >= 0);
            return (number < numerals.Length) ? numerals[number] : number.ToString();
        }

        public static string Join(this IEnumerable<string?> source, string separator)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (separator == null)
            {
                throw new ArgumentNullException(nameof(separator));
            }

            return string.Join(separator, source);
        }

        public static bool LooksLikeInterfaceName(this string name)
        {
            return name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]);
        }

        public static bool LooksLikeTypeParameterName(this string name)
        {
            return name.Length >= 3 && name[0] == 'T' && char.IsUpper(name[1]) && char.IsLower(name[2]);
        }

        private static readonly Func<char, char> s_toLower = char.ToLower;
        private static readonly Func<char, char> s_toUpper = char.ToUpper;

        [return: NotNullIfNotNull(parameterName: nameof(shortName))]
        public static string? ToPascalCase(
            this string? shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toUpper);
        }

        [return: NotNullIfNotNull(parameterName: nameof(shortName))]
        public static string? ToCamelCase(
            this string? shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toLower);
        }

        [return: NotNullIfNotNull(parameterName: nameof(shortName))]
        private static string? ConvertCase(
            this string? shortName,
            bool trimLeadingTypePrefix,
            Func<char, char> convert)
        {
            // Special case the common .NET pattern of "IGoo" as a type name.  In this case we
            // want to generate "goo" as the parameter name.  
            if (!RoslynString.IsNullOrEmpty(shortName))
            {
                if (trimLeadingTypePrefix && (shortName.LooksLikeInterfaceName() || shortName.LooksLikeTypeParameterName()))
                {
                    return convert(shortName[1]) + shortName.Substring(2);
                }

                if (convert(shortName[0]) != shortName[0])
                {
                    return convert(shortName[0]) + shortName.Substring(1);
                }
            }

            return shortName;
        }

        internal static bool IsValidClrTypeName([NotNullWhen(returnValue: true)] this string? name)
        {
            return !RoslynString.IsNullOrEmpty(name) && name.IndexOf('\0') == -1;
        }

        /// <summary>
        /// Checks if the given name is a sequence of valid CLR names separated by a dot.
        /// </summary>
        internal static bool IsValidClrNamespaceName([NotNullWhen(returnValue: true)] this string? name)
        {
            if (RoslynString.IsNullOrEmpty(name))
            {
                return false;
            }

            char lastChar = '.';
            foreach (char c in name)
            {
                if (c == '\0' || (c == '.' && lastChar == '.'))
                {
                    return false;
                }

                lastChar = c;
            }

            return lastChar != '.';
        }

        private const string AttributeSuffix = "Attribute";

        internal static string GetWithSingleAttributeSuffix(
            this string name,
            bool isCaseSensitive)
        {
            string? cleaned = name;
            while ((cleaned = GetWithoutAttributeSuffix(cleaned, isCaseSensitive)) != null)
            {
                name = cleaned;
            }

            return name + AttributeSuffix;
        }

        internal static bool TryGetWithoutAttributeSuffix(
            this string name,
            [NotNullWhen(returnValue: true)] out string? result)
        {
            return TryGetWithoutAttributeSuffix(name, isCaseSensitive: true, result: out result);
        }

        internal static string? GetWithoutAttributeSuffix(
            this string name,
            bool isCaseSensitive)
        {
            return TryGetWithoutAttributeSuffix(name, isCaseSensitive, out var result) ? result : null;
        }

        internal static bool TryGetWithoutAttributeSuffix(
            this string name,
            bool isCaseSensitive,
            [NotNullWhen(returnValue: true)] out string? result)
        {
            if (name.HasAttributeSuffix(isCaseSensitive))
            {
                result = name.Substring(0, name.Length - AttributeSuffix.Length);
                return true;
            }

            result = null;
            return false;
        }

        internal static bool HasAttributeSuffix(this string name, bool isCaseSensitive)
        {
            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, comparison);
        }

        internal static bool IsValidUnicodeString(this string str)
        {
            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];

                // (high surrogate, low surrogate) makes a valid pair, anything else is invalid:
                if (char.IsHighSurrogate(c))
                {
                    if (i < str.Length && char.IsLowSurrogate(str[i]))
                    {
                        i++;
                    }
                    else
                    {
                        // high surrogate not followed by low surrogate
                        return false;
                    }
                }
                else if (char.IsLowSurrogate(c))
                {
                    // previous character wasn't a high surrogate
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Remove one set of leading and trailing double quote characters, if both are present.
        /// </summary>
        internal static string Unquote(this string arg)
        {
            return Unquote(arg, out _);
        }

        internal static string Unquote(this string arg, out bool quoted)
        {
            if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
            {
                quoted = true;
                return arg.Substring(1, arg.Length - 2);
            }
            else
            {
                quoted = false;
                return arg;
            }
        }

        // String isn't IEnumerable<char> in the current Portable profile. 
        internal static char First(this string arg)
        {
            return arg[0];
        }

        // String isn't IEnumerable<char> in the current Portable profile. 
        internal static char Last(this string arg)
        {
            return arg[arg.Length - 1];
        }

        // String isn't IEnumerable<char> in the current Portable profile. 
        internal static bool All(this string arg, Predicate<char> predicate)
        {
            foreach (char c in arg)
            {
                if (!predicate(c))
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetCaseInsensitivePrefixLength(this string string1, string string2)
        {
            int x = 0;
            while (x < string1.Length && x < string2.Length &&
                   char.ToUpper(string1[x]) == char.ToUpper(string2[x]))
            {
                x++;
            }

            return x;
        }

        public static int GetCaseSensitivePrefixLength(this string string1, string string2)
        {
            int x = 0;
            while (x < string1.Length && x < string2.Length &&
                   string1[x] == string2[x])
            {
                x++;
            }

            return x;
        }
    }
}
