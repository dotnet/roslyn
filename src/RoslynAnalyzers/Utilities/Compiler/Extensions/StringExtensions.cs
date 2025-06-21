// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class StringExtensions
    {
        //        public static bool HasSuffix(this string str, string suffix)
        //        {
        //            if (str == null)
        //            {
        //                throw new ArgumentNullException(nameof(str));
        //            }

        //            if (suffix == null)
        //            {
        //                throw new ArgumentNullException(nameof(suffix));
        //            }

        //            return str.EndsWith(suffix, StringComparison.Ordinal);
        //        }

        //        public static string WithoutSuffix(this string str, string suffix)
        //        {
        //            if (str == null)
        //            {
        //                throw new ArgumentNullException(nameof(str));
        //            }

        //            if (suffix == null)
        //            {
        //                throw new ArgumentNullException(nameof(suffix));
        //            }

        //            if (!str.HasSuffix(suffix))
        //            {
        //                throw new ArgumentException(
        //                        $"The string {str} does not end with the suffix {suffix}.",
        //                        nameof(str));
        //            }

        //            return str[..^suffix.Length];
        //        }

        public static bool IsASCII(this string value)
        {
            // ASCII encoding replaces non-ascii with question marks, so we use UTF8 to see if multi-byte sequences are there
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }
    }
}
