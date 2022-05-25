// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// LICENSING NOTE: The license for this file is from the originating 
// source and not the general https://github.com/dotnet/roslyn license.
// See https://github.com/dotnet/runtime/blob/5b5bd46c03c86f8545f2c4c8628ac25d875210fe/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCharClass.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using static FeaturesResources;

    /// <summary>
    /// Minimal copy of https://github.com/dotnet/corefx/blob/main/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCharClass.cs
    /// Used to accurately determine if something is a WordChar according to the .NET regex engine.
    /// </summary>
    internal static class RegexCharClass
    {
        public static readonly Dictionary<string, (string shortDescription, string longDescription)> EscapeCategories =
            new()
            {
                // Others
                { "Cc", (Regex_other_control, "") },
                { "Cf", (Regex_other_format, "") },
                { "Cn", (Regex_other_not_assigned, "") },
                { "Co", (Regex_other_private_use, "") },
                { "Cs", (Regex_other_surrogate, "") },
                { "C", (Regex_all_control_characters_short, Regex_all_control_characters_long) },
                // Letters
                { "Ll", (Regex_letter_lowercase, "") },
                { "Lm", (Regex_letter_modifier, "") },
                { "Lo", (Regex_letter_other, "") },
                { "Lt", (Regex_letter_titlecase, "") },
                { "Lu", (Regex_letter_uppercase, "") },
                { "L", (Regex_all_letter_characters_short, Regex_all_letter_characters_long) },
                // Marks
                { "Mc", (Regex_mark_spacing_combining, "") },
                { "Me", (Regex_mark_enclosing, "") },
                { "Mn", (Regex_mark_nonspacing, "") },
                { "M", (Regex_all_diacritic_marks_short, Regex_all_diacritic_marks_long) },
                // Numbers
                { "Nd", (Regex_number_decimal_digit, "") },
                { "Nl", (Regex_number_letter, "") },
                { "No", (Regex_number_other, "") },
                { "N", (Regex_all_numbers_short, Regex_all_numbers_long) },
                // Punctuation
                { "Pc", (Regex_punctuation_connector, "") },
                { "Pd", (Regex_punctuation_dash, "") },
                { "Pe", (Regex_punctuation_close, "") },
                { "Po", (Regex_punctuation_other, "") },
                { "Ps", (Regex_punctuation_open, "") },
                { "Pf", (Regex_punctuation_final_quote, "") },
                { "Pi", (Regex_punctuation_initial_quote, "") },
                { "P", (Regex_all_punctuation_characters_short, Regex_all_punctuation_characters_long) },
                // Symbols
                { "Sc", (Regex_symbol_currency, "") },
                { "Sk", (Regex_symbol_modifier, "") },
                { "Sm", (Regex_symbol_math, "") },
                { "So", (Regex_symbol_other, "") },
                { "S", (Regex_all_symbols_short, Regex_all_symbols_long) },
                // Separators
                { "Zl", (Regex_separator_line, "") },
                { "Zp", (Regex_separator_paragraph, "") },
                { "Zs", (Regex_separator_space, "") },
                { "Z", (Regex_all_separator_characters_short, Regex_all_separator_characters_long) },

                { "IsAlphabeticPresentationForms", ("", "") },
                { "IsArabic", ("", "") },
                { "IsArabicPresentationForms-A", ("", "") },
                { "IsArabicPresentationForms-B", ("", "") },
                { "IsArmenian", ("", "") },
                { "IsArrows", ("", "") },
                { "IsBasicLatin", ("", "") },
                { "IsBengali", ("", "") },
                { "IsBlockElements", ("", "") },
                { "IsBopomofo", ("", "") },
                { "IsBopomofoExtended", ("", "") },
                { "IsBoxDrawing", ("", "") },
                { "IsBraillePatterns", ("", "") },
                { "IsBuhid", ("", "") },
                { "IsCJKCompatibility", ("", "") },
                { "IsCJKCompatibilityForms", ("", "") },
                { "IsCJKCompatibilityIdeographs", ("", "") },
                { "IsCJKRadicalsSupplement", ("", "") },
                { "IsCJKSymbolsandPunctuation", ("", "") },
                { "IsCJKUnifiedIdeographs", ("", "") },
                { "IsCJKUnifiedIdeographsExtensionA", ("", "") },
                { "IsCherokee", ("", "") },
                { "IsCombiningDiacriticalMarks", ("", "") },
                { "IsCombiningDiacriticalMarksforSymbols", ("", "") },
                { "IsCombiningHalfMarks", ("", "") },
                { "IsCombiningMarksforSymbols", ("", "") },
                { "IsControlPictures", ("", "") },
                { "IsCurrencySymbols", ("", "") },
                { "IsCyrillic", ("", "") },
                { "IsCyrillicSupplement", ("", "") },
                { "IsDevanagari", ("", "") },
                { "IsDingbats", ("", "") },
                { "IsEnclosedAlphanumerics", ("", "") },
                { "IsEnclosedCJKLettersandMonths", ("", "") },
                { "IsEthiopic", ("", "") },
                { "IsGeneralPunctuation", ("", "") },
                { "IsGeometricShapes", ("", "") },
                { "IsGeorgian", ("", "") },
                { "IsGreek", ("", "") },
                { "IsGreekExtended", ("", "") },
                { "IsGreekandCoptic", ("", "") },
                { "IsGujarati", ("", "") },
                { "IsGurmukhi", ("", "") },
                { "IsHalfwidthandFullwidthForms", ("", "") },
                { "IsHangulCompatibilityJamo", ("", "") },
                { "IsHangulJamo", ("", "") },
                { "IsHangulSyllables", ("", "") },
                { "IsHanunoo", ("", "") },
                { "IsHebrew", ("", "") },
                { "IsHighPrivateUseSurrogates", ("", "") },
                { "IsHighSurrogates", ("", "") },
                { "IsHiragana", ("", "") },
                { "IsIPAExtensions", ("", "") },
                { "IsIdeographicDescriptionCharacters", ("", "") },
                { "IsKanbun", ("", "") },
                { "IsKangxiRadicals", ("", "") },
                { "IsKannada", ("", "") },
                { "IsKatakana", ("", "") },
                { "IsKatakanaPhoneticExtensions", ("", "") },
                { "IsKhmer", ("", "") },
                { "IsKhmerSymbols", ("", "") },
                { "IsLao", ("", "") },
                { "IsLatin-1Supplement", ("", "") },
                { "IsLatinExtended-A", ("", "") },
                { "IsLatinExtended-B", ("", "") },
                { "IsLatinExtendedAdditional", ("", "") },
                { "IsLetterlikeSymbols", ("", "") },
                { "IsLimbu", ("", "") },
                { "IsLowSurrogates", ("", "") },
                { "IsMalayalam", ("", "") },
                { "IsMathematicalOperators", ("", "") },
                { "IsMiscellaneousMathematicalSymbols-A", ("", "") },
                { "IsMiscellaneousMathematicalSymbols-B", ("", "") },
                { "IsMiscellaneousSymbols", ("", "") },
                { "IsMiscellaneousSymbolsandArrows", ("", "") },
                { "IsMiscellaneousTechnical", ("", "") },
                { "IsMongolian", ("", "") },
                { "IsMyanmar", ("", "") },
                { "IsNumberForms", ("", "") },
                { "IsOgham", ("", "") },
                { "IsOpticalCharacterRecognition", ("", "") },
                { "IsOriya", ("", "") },
                { "IsPhoneticExtensions", ("", "") },
                { "IsPrivateUse", ("", "") },
                { "IsPrivateUseArea", ("", "") },
                { "IsRunic", ("", "") },
                { "IsSinhala", ("", "") },
                { "IsSmallFormVariants", ("", "") },
                { "IsSpacingModifierLetters", ("", "") },
                { "IsSpecials", ("", "") },
                { "IsSuperscriptsandSubscripts", ("", "") },
                { "IsSupplementalArrows-A", ("", "") },
                { "IsSupplementalArrows-B", ("", "") },
                { "IsSupplementalMathematicalOperators", ("", "") },
                { "IsSyriac", ("", "") },
                { "IsTagalog", ("", "") },
                { "IsTagbanwa", ("", "") },
                { "IsTaiLe", ("", "") },
                { "IsTamil", ("", "") },
                { "IsTelugu", ("", "") },
                { "IsThaana", ("", "") },
                { "IsThai", ("", "") },
                { "IsTibetan", ("", "") },
                { "IsUnifiedCanadianAboriginalSyllabics", ("", "") },
                { "IsVariationSelectors", ("", "") },
                { "IsYiRadicals", ("", "") },
                { "IsYiSyllables", ("", "") },
                { "IsYijingHexagramSymbols", ("", "") },
                { "_xmlC", ("", "") },
                { "_xmlD", ("", "") },
                { "_xmlI", ("", "") },
                { "_xmlW", ("", "") },
            };

        public static bool IsEscapeCategory(string value)
            => EscapeCategories.ContainsKey(value);

        public static bool IsBoundaryWordChar(VirtualChar r)
        {
            // unicode characters that do not fit in 16bits are not supported by 
            // .net regex system.
            if (r.Value > char.MaxValue)
                return false;

            var ch = (char)r.Value;

            // According to UTS#18 Unicode Regular Expressions (http://www.unicode.org/reports/tr18/)
            // RL 1.4 Simple Word Boundaries  The class of <word_character> includes all Alphabetic
            // values from the Unicode character database, from UnicodeData.txt [UData], plus the U+200C
            // ZERO WIDTH NON-JOINER and U+200D ZERO WIDTH JOINER.

            // Fast lookup in our lookup table for ASCII characters.  This is purely an optimization, and has the
            // behavior as if we fell through to the switch below (which was actually used to produce the lookup table).
            ReadOnlySpan<byte> asciiLookup = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03,
                0xFE, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x07
            };
            var chDiv8 = ch >> 3;
            if ((uint)chDiv8 < (uint)asciiLookup.Length)
            {
                return (asciiLookup[chDiv8] & (1 << (ch & 0x7))) != 0;
            }

            // For non-ASCII, fall back to checking the Unicode category.
            switch (CharUnicodeInfo.GetUnicodeCategory(ch))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    return true;

                default:
                    const char ZeroWidthNonJoiner = '\u200C', ZeroWidthJoiner = '\u200D';
                    return ch == ZeroWidthJoiner | ch == ZeroWidthNonJoiner;
            }
        }
    }
}
