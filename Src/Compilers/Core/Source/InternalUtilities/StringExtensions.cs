// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Utilities
{
    internal static class StringExtensions
    {
        public static string Join(this IEnumerable<string> source, string separator)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (separator == null)
            {
                throw new ArgumentNullException("separator");
            }

            return string.Join(separator, source);
        }

        /// <summary>
        /// Used to indicate places where we are hard-coding strings that will later need to be
        /// localized.  This way, we can use a "Find All References" to find and fix these.
        /// </summary>
        public static string NeedsLocalization(this string value)
        {
            return value;
        }

        public static bool LooksLikeInterfaceName(this string name)
        {
            return name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]);
        }

        public static bool LooksLikeTypeParameterName(this string name)
        {
            return name.Length >= 3 && name[0] == 'T' && char.IsUpper(name[1]) && char.IsLower(name[2]);
        }

        private static readonly Func<char, char> toLower = char.ToLower;
        private static readonly Func<char, char> toUpper = char.ToUpper;

        public static string ToPascalCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, toUpper);
        }

        public static string ToCamelCase(
            this string shortName,
            bool trimLeadingTypePrefix = true)
        {
            return ConvertCase(shortName, trimLeadingTypePrefix, toLower);
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
    }
}