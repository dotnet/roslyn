// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Implements VB string escaping. Uses iterators and thus it's implemented in shared code.
    /// </summary>
    internal static class VbStringDisplay
    {
        internal static string FormatString(string str, bool quote, char nonPrintableSubstitute, bool useHexadecimalNumbers)
        {
            PooledStringBuilder pooledBuilder = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooledBuilder.Builder;
            foreach (int token in TokenizeString(str, quote, nonPrintableSubstitute, useHexadecimalNumbers))
            {
                sb.Append(unchecked((char)token));
            }

            return pooledBuilder.ToStringAndFree();
        }

        internal static void AddSymbolDisplayParts(ArrayBuilder<SymbolDisplayPart> parts, string str)
        {
            PooledStringBuilder pooledBuilder = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooledBuilder.Builder;

            int lastKind = -1;
            foreach (int token in TokenizeString(str, quote: true, nonPrintableSubstitute: '\0', useHexadecimalNumbers: true))
            {
                int kind = token >> 16;

                // merge contiguous tokens of the same kind into a single part:
                if (lastKind >= 0 && lastKind != kind)
                {
                    parts.Add(new SymbolDisplayPart((SymbolDisplayPartKind)lastKind, null, sb.ToString()));
                    sb.Clear();
                }

                lastKind = kind;
                sb.Append(unchecked((char)token));
            }

            if (lastKind >= 0)
            {
                parts.Add(new SymbolDisplayPart((SymbolDisplayPartKind)lastKind, null, sb.ToString()));
            }

            pooledBuilder.Free();
        }

        internal static void AddSymbolDisplayParts(ArrayBuilder<SymbolDisplayPart> parts, char c)
        {
            string wellKnown = GetWellKnownCharacterName(c);
            if (wellKnown != null)
            {
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, wellKnown));
                return;
            }

            if (IsPrintable(c))
            {
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.StringLiteral, null, "\"" + c + "\"c"));
                return;
            }

            // non-printable, add "ChrW(codepoint)"
            int codepoint = (int)c;
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, "ChrW"));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.NumericLiteral, null, "&H" + codepoint.ToString("X")));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
        }

        private static int Character(char c)
        {
            return ((int)SymbolDisplayPartKind.StringLiteral << 16) | (int)c;
        }

        private static int Identifier(char c)
        {
            return ((int)SymbolDisplayPartKind.MethodName << 16) | (int)c;
        }

        private static int Number(char c)
        {
            return ((int)SymbolDisplayPartKind.NumericLiteral << 16) | (int)c;
        }

        private static int Punctuation(char c)
        {
            return ((int)SymbolDisplayPartKind.Punctuation << 16) | (int)c;
        }

        private static int Operator(char c)
        {
            return ((int)SymbolDisplayPartKind.Operator << 16) | (int)c;
        }

        private static int Space()
        {
            return ((int)SymbolDisplayPartKind.Space << 16) | (int)' ';
        }

        private static int Quotes()
        {
            return ((int)SymbolDisplayPartKind.StringLiteral << 16) | (int)'"';
        }

        private static IEnumerable<int> TokenizeString(string str, bool quote, char nonPrintableSubstitute, bool useHexadecimalNumbers)
        {
            if (str.Length == 0)
            {
                if (quote)
                {
                    yield return Quotes();
                    yield return Quotes();
                }

                yield break;
            }

            var startNewConcatenand = false;
            var lastConcatenandWasQuoted = false;
            int i = 0;
            while (i < str.Length)
            {
                bool isFirst = i == 0;
                char c = str[i++];
                string wellKnown;
                bool isNonPrintable;
                bool isCrLf;

                // vbCrLf
                if (c == '\r' && i < str.Length && str[i] == '\n')
                {
                    wellKnown = "vbCrLf";
                    isNonPrintable = true;
                    isCrLf = true;
                    i = i + 1;
                }
                else
                {
                    wellKnown = GetWellKnownCharacterName(c);
                    isNonPrintable = wellKnown != null || !IsPrintable(c);
                    isCrLf = false;
                }

                if (isNonPrintable)
                {
                    if (nonPrintableSubstitute != '\0')
                    {
                        yield return Character(nonPrintableSubstitute);
                        if (isCrLf)
                        {
                            yield return Character(nonPrintableSubstitute);
                        }
                    }
                    else if (quote)
                    {
                        if (lastConcatenandWasQuoted)
                        {
                            yield return Quotes();
                            lastConcatenandWasQuoted = false;
                        }

                        if (!isFirst)
                        {
                            yield return Space();
                            yield return Operator('&');
                            yield return Space();
                        }

                        if (wellKnown != null)
                        {
                            foreach (var e in wellKnown)
                            {
                                yield return Identifier(e);
                            }
                        }
                        else
                        {
                            yield return Identifier('C');
                            yield return Identifier('h');
                            yield return Identifier('r');
                            yield return Identifier('W');
                            yield return Punctuation('(');

                            if (useHexadecimalNumbers)
                            {
                                yield return Number('&');
                                yield return Number('H');
                            }

                            int codepoint = (int)c;
                            foreach (var digit in useHexadecimalNumbers ? codepoint.ToString("X") : codepoint.ToString())
                            {
                                yield return Number(digit);
                            }

                            yield return Punctuation(')');
                        }

                        startNewConcatenand = true;
                    }
                    else if (isCrLf)
                    {
                        yield return Character('\r');
                        yield return Character('\n');
                    }
                    else
                    {
                        yield return Character(c);
                    }
                }
                else
                {
                    if (isFirst && quote)
                    {
                        yield return Quotes();
                    }

                    if (startNewConcatenand)
                    {
                        yield return Space();
                        yield return Operator('&');
                        yield return Space();
                        yield return Quotes();

                        startNewConcatenand = false;
                    }

                    lastConcatenandWasQuoted = true;
                    if (c == '"' && quote)
                    {
                        yield return Quotes();
                        yield return Quotes();
                    }
                    else
                    {
                        yield return Character(c);
                    }
                }
            }

            if (quote && lastConcatenandWasQuoted)
            {
                yield return Quotes();
            }
        }

        internal static bool IsPrintable(char c)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            return category != UnicodeCategory.OtherNotAssigned && category != UnicodeCategory.ParagraphSeparator && category != UnicodeCategory.Control;
        }

        internal static string GetWellKnownCharacterName(char c)
        {
            switch (c)
            {
                case '\0':
                    return "vbNullChar";

                case '\b':
                    return "vbBack";

                case '\r':
                    return "vbCr";

                case '\f':
                    return "vbFormFeed";

                case '\n':
                    return "vbLf";

                case '\t':
                    return "vbTab";

                case '\v':
                    return "vbVerticalTab";
            }

            return null;
        }
    }
}
