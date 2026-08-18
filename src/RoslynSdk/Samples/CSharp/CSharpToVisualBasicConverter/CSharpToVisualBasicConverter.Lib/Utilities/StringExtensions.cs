// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
