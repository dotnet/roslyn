// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// <remarks></remarks>
        private sealed class OneToOneUnicodeComparer : StringComparer
        {
            // PERF: Grab the TextInfo for the invariant culture since this will be accessed very frequently
            private static readonly TextInfo invariantCultureTextInfo = CultureInfo.InvariantCulture.TextInfo;

            /// <summary>
            /// ToLower implements the one-to-one Unicode lowercase mapping
            /// as descriped in ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt.
            /// The VB spec states that these mappings are used for case-insensitive
            /// comparison
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            public static char ToLower(char c)
            {
                // PERF: This is a very hot code path in VB, optimize for Ascii
                if (IsAscii(c))
                {
                    // copied from BCL: Textinfo.ToLowerAsciiInvariant
                    // if ('A' <= c && c <= 'Z')
                    // we will do it with only one branch though since we want this to be fast
                    if (unchecked((uint)(c - 'A')) <= ('Z' - 'A'))
                    {
                        c = (char)(c | 0x20);
                    }
                }
                else if (c == '\u0130')
                {
                    // Special case Turkish I, see bug 531346
                    c = 'i';
                }
                else
                {
                    c = invariantCultureTextInfo.ToLower(c);
                }

                return c;
            }

            private static bool IsAscii(char c)
            {
                return c < 0x80;
            }

            public override int Compare(string str1, string str2)
            {
                if (str1 == null)
                {
                    return str2 == null ? 0 : -1;
                }
                else if (str2 == null)
                {
                    return 1;
                }

                for (int i = 0; i < Math.Min(str1.Length, str2.Length); i++)
                {
                    int ordDiff = ToLower(str1[i]) - ToLower(str2[i]);
                    if (ordDiff != 0)
                    {
                        return ordDiff;
                    }
                }

                // return the smaller string, or 0 if (they are equal in length
                return str1.Length - str2.Length;
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
                    if (ToLower(str1[i]) != ToLower(str2[i]))
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
                    if (ToLower(value[i]) != ToLower(possibleEnd[j]))
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
                    hashCode = unchecked((hashCode ^ ToLower(str[i]) * Hash.FnvPrime));
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        private static readonly OneToOneUnicodeComparer m_Comparer = new OneToOneUnicodeComparer();

        /// <summary>
        /// Returns a StringComparer that compares strings according the VB identifier comparison rules.
        /// </summary>
        public static StringComparer Comparer
        {
            get { return m_Comparer; }
        }

        /// <summary>
        /// Determines if (two VB identifiers are equal according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>true if (the identifiers should be considered the same.</returns>
        public static bool Equals(string left, string right)
        {
            return m_Comparer.Equals(left, right);
        }

        /// <summary>
        /// Determines if the string 'value' end with string 'possibleEnd'.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="possibleEnd"></param>
        /// <returns></returns>
        public static bool EndsWith(string value, string possibleEnd)
        {
            return OneToOneUnicodeComparer.EndsWith(value, possibleEnd);
        }

        /// <summary>
        /// Compares two VB identifiers according to the VB identifier comparison rules.
        /// </summary>
        /// <param name="left">First identifier to compare</param>
        /// <param name="right">Second identifier to compare</param>
        /// <returns>-1 if (ident1 &lt; ident2, 1 if (ident1 &gt; ident2, 0 if (they are equal.</returns>
        public static int Compare(string left, string right)
        {
            return m_Comparer.Compare(left, right);
        }

        /// <summary>
        /// Gets a case-insensitive hash code for VB identifiers.
        /// </summary>
        /// <param name="value">identifier to get the hash code for</param>
        /// <returns>The hash code for the given identifier</returns>
        public static int GetHashCode(string value)
        {
            Debug.Assert(value != null);

            return m_Comparer.GetHashCode(value);
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
        /// In-place convert string in StruingBuilder to lower case in culture invariant way
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