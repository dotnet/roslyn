﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Defines a set of methods to determine how Unicode characters are treated by the C# compiler.
    /// </summary>
    public static partial class SyntaxFacts
    {
        /// <summary>
        /// Returns true if the Unicode character is a hexadecimal digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the character is a hexadecimal digit 0-9, A-F, a-f.</returns>
        internal static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }

        /// <summary>
        /// Returns true if the Unicode character is a binary (0-1) digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the character is a binary digit.</returns>
        internal static bool IsBinaryDigit(char c)
        {
            return c == '0' || c == '1';
        }

        /// <summary>
        /// Returns true if the Unicode character is a decimal digit.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns>true if the Unicode character is a decimal digit.</returns>
        internal static bool IsDecDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Returns the value of a hexadecimal Unicode character.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        internal static int HexValue(char c)
        {
            Debug.Assert(IsHexDigit(c));
            return (c >= '0' && c <= '9') ? c - '0' : (c & 0xdf) - 'A' + 10;
        }

        /// <summary>
        /// Returns the value of a decimal Unicode character.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        internal static int DecValue(char c)
        {
            Debug.Assert(IsDecDigit(c));
            return c - '0';
        }

        // UnicodeCategory value | Unicode designation
        // -----------------------+-----------------------
        // UppercaseLetter         "Lu" (letter, uppercase)
        // LowercaseLetter         "Ll" (letter, lowercase)
        // TitlecaseLetter         "Lt" (letter, titlecase)
        // ModifierLetter          "Lm" (letter, modifier)
        // OtherLetter             "Lo" (letter, other)
        // NonSpacingMark          "Mn" (mark, nonspacing)
        // SpacingCombiningMark    "Mc" (mark, spacing combining)
        // EnclosingMark           "Me" (mark, enclosing)
        // DecimalDigitNumber      "Nd" (number, decimal digit)
        // LetterNumber            "Nl" (number, letter)
        // OtherNumber             "No" (number, other)
        // SpaceSeparator          "Zs" (separator, space)
        // LineSeparator           "Zl" (separator, line)
        // ParagraphSeparator      "Zp" (separator, paragraph)
        // Control                 "Cc" (other, control)
        // Format                  "Cf" (other, format)
        // Surrogate               "Cs" (other, surrogate)
        // PrivateUse              "Co" (other, private use)
        // ConnectorPunctuation    "Pc" (punctuation, connector)
        // DashPunctuation         "Pd" (punctuation, dash)
        // OpenPunctuation         "Ps" (punctuation, open)
        // ClosePunctuation        "Pe" (punctuation, close)
        // InitialQuotePunctuation "Pi" (punctuation, initial quote)
        // FinalQuotePunctuation   "Pf" (punctuation, final quote)
        // OtherPunctuation        "Po" (punctuation, other)
        // MathSymbol              "Sm" (symbol, math)
        // CurrencySymbol          "Sc" (symbol, currency)
        // ModifierSymbol          "Sk" (symbol, modifier)
        // OtherSymbol             "So" (symbol, other)
        // OtherNotAssigned        "Cn" (other, not assigned)

        /// <summary>
        /// Returns true if the Unicode character represents a whitespace.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsWhitespace(char ch)
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
                || ch == '\uFEFF'
                || ch == '\u001A'
                || (ch > 255 && CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator);
        }

        /// <summary>
        /// Returns true if the Unicode character is a newline character.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsNewLine(char ch)
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

        /// <summary>
        /// Returns true if the Unicode character can be the starting character of a C# identifier.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsIdentifierStartCharacter(char ch)
        {
            // identifier-start-character:
            //   letter-character
            //   _ (the underscore character U+005F)

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return false;
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch));
        }

        /// <summary>
        /// Returns true if the Unicode character can be a part of a C# identifier.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        public static bool IsIdentifierPartCharacter(char ch)
        {
            // identifier-part-character:
            //   letter-character
            //   decimal-digit-character
            //   connecting-character
            //   combining-character
            //   formatting-character

            if (ch < 'a') // '\u0061'
            {
                if (ch < 'A') // '\u0041'
                {
                    return ch >= '0'  // '\u0030'
                        && ch <= '9'; // '\u0039'
                }

                return ch <= 'Z'  // '\u005A'
                    || ch == '_'; // '\u005F'
            }

            if (ch <= 'z') // '\u007A'
            {
                return true;
            }

            if (ch <= '\u007F') // max ASCII
            {
                return false;
            }

            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            return IsLetterChar(cat)
                || IsDecimalDigitChar(cat)
                || IsConnectingChar(cat)
                || IsCombiningChar(cat)
                || IsFormattingChar(cat);
        }

        /// <summary>
        /// Check that the name is a valid identifier.
        /// </summary>
        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!SyntaxFacts.IsIdentifierStartCharacter(name[0]))
            {
                return false;
            }

            int nameLength = name.Length;
            for (int i = 1; i < nameLength; i++) //NB: start at 1
            {
                if (!SyntaxFacts.IsIdentifierPartCharacter(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Spec section 2.4.2 says that identifiers are compared without regard
        /// to leading "@" characters or unicode formatting characters.  As in dev10,
        /// this is actually accomplished by dropping such characters during parsing.
        /// Unfortunately, metadata names can still contain these characters and will
        /// not be referenceable from source if they do (lookup will fail since the
        /// characters will have been dropped from the search string).
        /// See DevDiv #14432 for more.
        /// </summary>
        internal static bool ContainsDroppedIdentifierCharacters(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (name[0] == '@')
            {
                return true;
            }

            int nameLength = name.Length;
            for (int i = 0; i < nameLength; i++)
            {
                if (SyntaxFacts.IsFormattingChar(name[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsLetterChar(UnicodeCategory cat)
        {
            // letter-character:
            //   A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl 
            //   A Unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl

            switch (cat)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
            }

            return false;
        }

        internal static bool IsCombiningChar(UnicodeCategory cat)
        {
            // combining-character:
            //   A Unicode character of classes Mn or Mc 
            //   A Unicode-escape-sequence representing a character of classes Mn or Mc

            switch (cat)
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                    return true;
            }

            return false;
        }

        internal static bool IsDecimalDigitChar(UnicodeCategory cat)
        {
            // decimal-digit-character:
            //   A Unicode character of the class Nd 
            //   A unicode-escape-sequence representing a character of the class Nd

            return cat == UnicodeCategory.DecimalDigitNumber;
        }

        internal static bool IsConnectingChar(UnicodeCategory cat)
        {
            // connecting-character:  
            //   A Unicode character of the class Pc
            //   A unicode-escape-sequence representing a character of the class Pc

            return cat == UnicodeCategory.ConnectorPunctuation;
        }

        /// <summary>
        /// Returns true if the Unicode character is a formatting character (Unicode class Cf).
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        internal static bool IsFormattingChar(char ch)
        {
            // There are no FormattingChars in ASCII range

            return ch > 127 && IsFormattingChar(CharUnicodeInfo.GetUnicodeCategory(ch));
        }

        /// <summary>
        /// Returns true if the Unicode character is a formatting character (Unicode class Cf).
        /// </summary>
        /// <param name="cat">The Unicode character.</param>
        internal static bool IsFormattingChar(UnicodeCategory cat)
        {
            // formatting-character:  
            //   A Unicode character of the class Cf
            //   A unicode-escape-sequence representing a character of the class Cf

            return cat == UnicodeCategory.Format;
        }

        internal static bool IsNonAsciiQuotationMark(char ch)
        {
            // CONSIDER: There are others:
            // http://en.wikipedia.org/wiki/Quotation_mark_glyphs#Quotation_marks_in_Unicode
            switch (ch)
            {
                case '\u2018': //LEFT SINGLE QUOTATION MARK
                case '\u2019': //RIGHT SINGLE QUOTATION MARK
                    return true;
                case '\u201C': //LEFT DOUBLE QUOTATION MARK
                case '\u201D': //RIGHT DOUBLE QUOTATION MARK
                    return true;
                default:
                    return false;
            }
        }
    }
}
