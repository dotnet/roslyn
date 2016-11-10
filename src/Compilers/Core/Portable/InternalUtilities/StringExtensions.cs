﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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

        public static string Join(this IEnumerable<string> source, string separator)
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

        public static string ToPascalCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toUpper);
        }

        public static string ToCamelCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, s_toLower);
        }

        private static string ConvertCase(
            this string shortName,
            bool trimLeadingTypePrefix,
            Func<char, char> convert)
        {
            // Special case the common .net pattern of "IFoo" as a type name.  In this case we
            // want to generate "foo" as the parameter name.  
            if (!string.IsNullOrEmpty(shortName))
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

        internal static bool IsValidClrTypeName(this string name)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOf('\0') == -1;
        }

        /// <summary>
        /// Checks if the given name is a sequence of valid CLR names separated by a dot.
        /// </summary>
        internal static bool IsValidClrNamespaceName(this string name)
        {
            if (string.IsNullOrEmpty(name))
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

        internal static string GetWithSingleAttributeSuffix(
            this string name,
            bool isCaseSensitive)
        {
            string cleaned = name;
            while ((cleaned = GetWithoutAttributeSuffix(cleaned, isCaseSensitive)) != null)
            {
                name = cleaned;
            }

            return name + "Attribute";
        }

        internal static bool TryGetWithoutAttributeSuffix(
            this string name,
            out string result)
        {
            return TryGetWithoutAttributeSuffix(name, isCaseSensitive: true, result: out result);
        }

        internal static string GetWithoutAttributeSuffix(
            this string name,
            bool isCaseSensitive)
        {
            string result;
            return TryGetWithoutAttributeSuffix(name, isCaseSensitive, out result) ? result : null;
        }

        internal static bool TryGetWithoutAttributeSuffix(
            this string name,
            bool isCaseSensitive,
            out string result)
        {
            const string AttributeSuffix = "Attribute";
            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, comparison))
            {
                result = name.Substring(0, name.Length - AttributeSuffix.Length);
                return true;
            }

            result = null;
            return false;
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
            bool quoted;
            return Unquote(arg, out quoted);
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

        internal static int IndexOfBalancedParenthesis(this string str, int openingOffset, char closing)
        {
            char opening = str[openingOffset];

            int depth = 1;
            for (int i = openingOffset + 1; i < str.Length; i++)
            {
                var c = str[i];
                if (c == opening)
                {
                    depth++;
                }
                else if (c == closing)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
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
    }
}