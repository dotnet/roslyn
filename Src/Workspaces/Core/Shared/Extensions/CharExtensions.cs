// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CharExtensions
    {
        // Copied from LexerBase.cs.  Could these be made public in 
        // Roslyn.Compilers.CSharp?
        public static bool IsCSharpWhitespace(this char ch)
        {
            // whitespace:
            //   Any character with Unicode class Zs
            //   Horizontal tab character (U+0009)
            //   Vertical tab character (U+000B)
            //   Form feed character (U+000C)

            // Space and no-break space are the only space separators (Zs) in ASCII range

            return ch == ' '
                || ch == '\t'
                || ch == '\v'
                || ch == '\f'
                || ch == '\u00A0' // NO-BREAK SPACE
                // The native compiler, in ScanToken, recognized both the byte-order
                // marker '\uFEFF' as well as ^Z '\u001A' as whitespace, although
                // this is not to spec since neither of these are in Zs. For the
                // sake of compatibility, we recognize them both here. Note: '\uFEFF'
                // also happens to be a formatting character (class Cf), which means
                // that it is a legal non-initial identifier character. So it's
                // especially funny, because it will be whitespace UNLESS we happen
                // to be scanning an identifier or keyword, in which case it winds
                // up in the identifier or keyword.
                // TODO: This is not ideal but we need to consider compat.
                || ch == '\uFEFF'
                || ch == '\u001A'
                || (ch > 255 && char.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator);
        }

        public static bool IsCSharpNewLine(this char ch)
        {
            // new-line-character:
            //   Carriage return character (U+000D)
            //   Line feed character (U+000A)
            //   Next line character (U+0085)
            //   Line separator character (U+2028)
            //   Paragraph separator character (U+2029)

            return ch == '\r'
                || ch == '\n'
                || ch == '\u0085'
                || ch == '\u2028'
                || ch == '\u2029';
        }
    }
}
