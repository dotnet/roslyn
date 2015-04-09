// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Case-insensitive operations (mostly comparison) on unicode strings.
    /// </summary>
    public static class CaseInsensitiveComparison
    {
        /// <summary>
        /// This class seeks to perform one-to-one lowercase Unicode case mappings, which should be culture invariant.
        /// </summary>
        private sealed class OneToOneUnicodeComparer : StringComparer
        {
            // PERF: Grab the TextInfo for the invariant culture since this will be accessed very frequently
            private static readonly TextInfo s_invariantCultureTextInfo = CultureInfo.InvariantCulture.TextInfo;

            /// <summary>
            /// ToLower implements the one-to-one Unicode lowercase mapping
            /// as descriped in ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt.
            /// The VB spec states that these mappings are used for case-insensitive
            /// comparison
            /// </summary>
            /// <param name="c"></param>
            /// <returns>If <paramref name="c"/> is upper case, then this returns its lower case equivalent. Otherwise, <paramref name="c"/> is returned unmodified.</returns>
            public static char ToLower(char c)
            {
                // PERF: This is a very hot code path in VB, optimize for ASCII

                // Perform a range check with a single compare by using unsigned arithmetic
                if (unchecked((uint)(c - 'A')) <= ('Z' - 'A'))
                {
                    return (char)(c | 0x20);
                }

                if (c < 0xC0) // Covers ASCII (U+0000 - U+007F) and up to the next upper-case codepoint (Latin Capital Letter A with Grave)
                {
                    return c;
                }

                return ToLowerNonAscii(c);
            }

            private static char ToLowerNonAscii(char c)
            {
                if (c == '\u0130')
                {
                    // Special case Turkish I, see bug 531346
                    return 'i';
                }

                return s_invariantCultureTextInfo.ToLower(c);
            }

            private static int CompareLowerInvariant(char c1, char c2)
            {
                return (c1 == c2) ? 0 : ToLower(c1) - ToLower(c2);
            }

            public override int Compare(string str1, string str2)
            {
                if (ReferenceEquals(str1, str2))
                {
                    return 0;
                }

                if (str1 == null)
                {
                    return -1;
                }

                if (str2 == null)
                {
                    return 1;
                }

                int len = Math.Min(str1.Length, str2.Length);
                for (int i = 0; i < len; i++)
                {
                    int ordDiff = CompareLowerInvariant(str1[i], str2[i]);
                    if (ordDiff != 0)
                    {
                        return ordDiff;
                    }
                }

                // return the smaller string, or 0 if they are equal in length
                return str1.Length - str2.Length;
            }

            private static bool AreEqualLowerInvariant(char c1, char c2)
            {
                return c1 == c2 || ToLower(c1) == ToLower(c2);
            }

            public override bool Equals(string str1, string str2)
            {
                if (ReferenceEquals(str1, str2))
                {
                    return true;
                }

                if (str1 == null || str2 == null)
                {
                    return false;
                }

                if (str1.Length != str2.Length)
                {
                    return false;
                }

                for (int i = 0; i < str1.Length; i++)
                {
                    if (!AreEqualLowerInvariant(str1[i], str2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool EndsWith(string value, string possibleEnd)
            {
                if (ReferenceEquals(value, possibleEnd))
                {
                    return true;
                }

                if (value == null || possibleEnd == null)
                {
                    return false;
                }

                int i = value.Length - 1;
                int j = possibleEnd.Length - 1;

                if (i < j)
                {
                    return false;
                }

                while (j >= 0)
                {
                    if (!AreEqualLowerInvariant(value[i], possibleEnd[j]))
                    {
                        return false;
                    }

                    i--;
                    j--;
                }

                return true;
            }

            public override int GetHashCode(string str)
            {
                int hashCode = Hash.FnvOffsetBias;

                for (int i = 0; i < str.Length; i++)
                {
                    hashCode = Hash.CombineFNVHash(hashCode, ToLower(str[i]));
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        private static readonly OneToOneUnicodeComparer s_comparer = new OneToOneUnicodeComparer();

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        public static StringComparer Comparer => s_comparer;

        /// <summary>
        /// Determines if two VB identifiers are equal according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>true if the identifiers should be considered the same.</returns>
        public static bool Equals(string left, string right) => s_comparer.Equals(left, right);

        /// <summary>
        /// Determines if the string 'value' end with string 'possibleEnd'.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="possibleEnd"></param>
        /// <returns></returns>
        public static bool EndsWith(string value, string possibleEnd) => OneToOneUnicodeComparer.EndsWith(value, possibleEnd);

        /// <summary>
        /// Compares two VB identifiers according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>-1 if <paramref name="left"/> &lt; <paramref name="right"/>, 1 if <paramref name="left"/> &gt; <paramref name="right"/>, 0 if they are equal.</returns>
        public static int Compare(string left, string right) => s_comparer.Compare(left, right);

        /// <summary>
        /// Gets a case-insensitive hash code for VB identifiers.
        /// </summary>
        /// <param name="value">identifier to get the hash code for</param>
        /// <returns>The hash code for the given identifier</returns>
        public static int GetHashCode(string value)
        {
            Debug.Assert(value != null);

            return s_comparer.GetHashCode(value);
        }

        /// <summary>
        /// Convert a string to lower case in culture invariant way
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToLower(string value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return value;

            var pooledStrbuilder = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooledStrbuilder.Builder;

            builder.Append(value);
            ToLower(builder);

            return pooledStrbuilder.ToStringAndFree();
        }

        /// <summary>
        /// In-place convert string in StringBuilder to lower case in culture invariant way
        /// </summary>
        /// <param name="builder"></param>
        public static void ToLower(StringBuilder builder)
        {
            if (builder == null)
                return;

            for (int i = 0; i < builder.Length; i++)
            {
                builder[i] = OneToOneUnicodeComparer.ToLower(builder[i]);
            }
        }
    }
}
