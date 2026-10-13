// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace CSharpToVisualBasicConverter.Utilities
{
    internal static class StringExtensions
    {
        public static string Repeat(this string s, int count)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (count == 0 || s.Length == 0)
            {
                return string.Empty;
            }
            else if (count == 1)
            {
                return s;
            }
            else
            {
                StringBuilder builder = new StringBuilder(s.Length * count);
                for (int i = 0; i < count; i++)
                {
                    builder.Append(s);
                }

                return builder.ToString();
            }
        }
    }
}
