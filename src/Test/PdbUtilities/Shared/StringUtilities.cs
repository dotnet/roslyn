// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;
using System.Text;

namespace Roslyn.Test
{
    internal static class StringUtilities
    {
        internal static string EscapeNonPrintableCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in str)
            {
                bool escape;
                switch (CharUnicodeInfo.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.Control:
                    case UnicodeCategory.OtherNotAssigned:
                    case UnicodeCategory.ParagraphSeparator:
                    case UnicodeCategory.Surrogate:
                        escape = true;
                        break;

                    default:
                        escape = c >= 0xFFFC;
                        break;
                }

                if (escape)
                {
                    sb.AppendFormat("\\u{0:X4}", (int)c);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
