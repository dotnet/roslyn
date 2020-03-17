// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// LICENSING NOTE: The license for this file is from the originating 
// source and not the general https://github.com/dotnet/roslyn license.
// See https://github.com/dotnet/corefx/blob/68b76c30eafb3647c11e3f766a2645b130ca1448/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCharClass.cs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using static WorkspacesResources;

    /// <summary>
    /// Minimal copy of https://github.com/dotnet/corefx/blob/master/src/System.Text.RegularExpressions/src/System/Text/RegularExpressions/RegexCharClass.cs
    /// Used to accurately determine if something is a WordChar according to the .NET regex engine.
    /// </summary>
    internal static class RegexCharClass
    {
        private const int FLAGS = 0;
        private const int SETLENGTH = 1;
        private const int CATEGORYLENGTH = 2;
        private const int SETSTART = 3;

        private const short SpaceConst = 100;
        private const short NotSpaceConst = -100;

        private const char ZeroWidthJoiner = '\u200D';
        private const char ZeroWidthNonJoiner = '\u200C';

        private const string WordClass = "\u0000\u0000\u000A\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";

        public static readonly Dictionary<string, (string shortDescription, string longDescription)> EscapeCategories =
            new Dictionary<string, (string, string)>
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
        {
            return EscapeCategories.ContainsKey(value);
        }

        public static bool IsWordChar(char ch)
        {
            // According to UTS#18 Unicode Regular Expressions (http://www.unicode.org/reports/tr18/)
            // RL 1.4 Simple Word Boundaries  The class of <word_character> includes all Alphabetic
            // values from the Unicode character database, from UnicodeData.txt [UData], plus the U+200C
            // ZERO WIDTH NON-JOINER and U+200D ZERO WIDTH JOINER.
            return CharInClass(ch, WordClass) || ch == ZeroWidthJoiner || ch == ZeroWidthNonJoiner;
        }

        internal static bool CharInClass(char ch, string set)
        {
            return CharInClassRecursive(ch, set, 0);
        }

        internal static bool CharInClassRecursive(char ch, string set, int start)
        {
            int mySetLength = set[start + SETLENGTH];
            int myCategoryLength = set[start + CATEGORYLENGTH];
            var myEndPosition = start + SETSTART + mySetLength + myCategoryLength;

            var subtracted = false;

            if (set.Length > myEndPosition)
            {
                subtracted = CharInClassRecursive(ch, set, myEndPosition);
            }

            var b = CharInClassInternal(ch, set, start, mySetLength, myCategoryLength);

            // Note that we apply the negation *before* performing the subtraction.  This is because
            // the negation only applies to the first char class, not the entire subtraction.
            if (set[start + FLAGS] == 1)
                b = !b;

            return b && !subtracted;
        }

        /// <summary>
        /// Determines a character's membership in a character class (via the
        /// string representation of the class).
        /// </summary>
        private static bool CharInClassInternal(char ch, string set, int start, int mySetLength, int myCategoryLength)
        {
            int min;
            int max;
            int mid;
            min = start + SETSTART;
            max = min + mySetLength;

            while (min != max)
            {
                mid = (min + max) / 2;
                if (ch < set[mid])
                    max = mid;
                else
                    min = mid + 1;
            }

            // The starting position of the set within the character class determines
            // whether what an odd or even ending position means.  If the start is odd,
            // an *even* ending position means the character was in the set.  With recursive
            // subtractions in the mix, the starting position = start+SETSTART.  Since we know that
            // SETSTART is odd, we can simplify it out of the equation.  But if it changes we need to
            // reverse this check.
            Debug.Assert((SETSTART & 0x1) == 1, "If SETSTART is not odd, the calculation below this will be reversed");
            if ((min & 0x1) == (start & 0x1))
                return true;
            else
            {
                if (myCategoryLength == 0)
                    return false;

                return CharInCategory(ch, set, start, mySetLength, myCategoryLength);
            }
        }

        private static bool CharInCategory(char ch, string set, int start, int mySetLength, int myCategoryLength)
        {
            var chcategory = CharUnicodeInfo.GetUnicodeCategory(ch);

            var i = start + SETSTART + mySetLength;
            var end = i + myCategoryLength;
            while (i < end)
            {
                int curcat = unchecked((short)set[i]);

                if (curcat == 0)
                {
                    // zero is our marker for a group of categories - treated as a unit
                    if (CharInCategoryGroup(ch, chcategory, set, ref i))
                        return true;
                }
                else if (curcat > 0)
                {
                    // greater than zero is a positive case

                    if (curcat == SpaceConst)
                    {
                        if (char.IsWhiteSpace(ch))
                            return true;
                        else
                        {
                            i++;
                            continue;
                        }
                    }
                    --curcat;

                    if (chcategory == (UnicodeCategory)curcat)
                        return true;
                }
                else
                {
                    // less than zero is a negative case
                    if (curcat == NotSpaceConst)
                    {
                        if (!char.IsWhiteSpace(ch))
                            return true;
                        else
                        {
                            i++;
                            continue;
                        }
                    }

                    //curcat = -curcat;
                    //--curcat;
                    curcat = -1 - curcat;

                    if (chcategory != (UnicodeCategory)curcat)
                        return true;
                }
                i++;
            }
            return false;
        }

        /// <summary>
        /// This is used for categories which are composed of other categories - L, N, Z, W...
        /// These groups need special treatment when they are negated
        /// </summary>
        private static bool CharInCategoryGroup(char ch, UnicodeCategory chcategory, string category, ref int i)
        {
            i++;

            int curcat = unchecked((short)category[i]);
            if (curcat > 0)
            {
                // positive case - the character must be in ANY of the categories in the group
                var answer = false;

                while (curcat != 0)
                {
                    if (!answer)
                    {
                        --curcat;
                        if (chcategory == (UnicodeCategory)curcat)
                            answer = true;
                    }
                    i++;
                    curcat = (short)category[i];
                }
                return answer;
            }
            else
            {
                // negative case - the character must be in NONE of the categories in the group
                var answer = true;

                while (curcat != 0)
                {
                    if (answer)
                    {
                        //curcat = -curcat;
                        //--curcat;
                        curcat = -1 - curcat;
                        if (chcategory == (UnicodeCategory)curcat)
                            answer = false;
                    }
                    i++;
                    curcat = unchecked((short)category[i]);
                }
                return answer;
            }
        }
    }
}
