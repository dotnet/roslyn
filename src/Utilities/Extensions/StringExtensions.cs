// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Analyzer.Utilities.Extensions
{
    internal static class StringExtensions
    {
        public static bool HasSuffix(this string str, string suffix)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            return str.EndsWith(suffix, StringComparison.Ordinal);
        }

        public static string WithoutSuffix(this string str, string suffix)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            if (!str.HasSuffix(suffix))
            {
                throw new ArgumentException(
                        $"The string {str} does not end with the suffix {suffix}.",
                        nameof(str));
            }

            return str.Substring(0, str.Length - suffix.Length);
        }
    }
}
