// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class StringBreaker
    {
        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static ArrayBuilder<TextSpan> GetWordParts(string identifier)
            => GetParts(identifier, word: true);

        public static ArrayBuilder<TextSpan> GetCharacterParts(string identifier)
            => GetParts(identifier, word: false);

        public static ArrayBuilder<TextSpan> GetParts(string text, bool word)
        {
            var parts = ArrayBuilder<TextSpan>.GetInstance();
            for (var start = 0; start < text.Length;)
            {
                var span = StringBreaker.GenerateSpan(text, start, word);
                if (span.IsEmpty)
                {
                    // All done
                    break;
                }

                Debug.Assert(span.Start >= start, "Bad generator.");

                parts.Add(span);
                start = span.End;
            }

            return parts;
        }

        public static TextSpan GenerateSpan(string identifier, int wordStart, bool word)
        {
            var length = identifier.Length;
            wordStart = SkipPunctuation(identifier, length, wordStart);
            if (wordStart < length)
            {
                var firstChar = identifier[wordStart];
                if (char.IsUpper(firstChar))
                {
                    if (wordStart + 1 == length)
                    {
                        return new TextSpan(wordStart, 1);
                    }

                    if (word)
                    {
                        return ScanWordRun(identifier, length, wordStart);
                    }
                    else
                    {
                        return ScanCharacterRun(identifier, length, wordStart);
                    }
                }
                else if (IsLower(firstChar))
                {
                    return ScanLowerCaseRun(identifier, length, wordStart);
                }
                else if (firstChar == '_')
                {
                    return new TextSpan(wordStart, 1);
                }
                else if (char.IsDigit(firstChar))
                {
                    return ScanNumber(identifier, length, wordStart);
                }
            }

            return default;
        }

        private static TextSpan ScanCharacterRun(string identifier, int length, int wordStart)
        {
            // In a character run, if we have XMLDocument, then we will break that up into
            // X, M, L, and Document.
            var current = wordStart + 1;
            Debug.Assert(current < length);
            var c = identifier[current];

            if (IsLower(c))
            {
                // "Do"
                // 
                // scan the lowercase letters from here on to scna out 'Document'.
                return ScanLowerCaseRun(identifier, length, wordStart);
            }
            else
            {
                return new TextSpan(wordStart, 1);
            }
        }

        private static TextSpan ScanWordRun(string identifier, int length, int wordStart)
        {
            // In a word run, if we have XMLDocument, then we will break that up into
            // XML and Document.

            var current = wordStart + 1;
            Debug.Assert(current < length);
            var c = identifier[current];

            if (char.IsUpper(c))
            {
                // "XM"

                current++;

                // scan all the upper case letters until we hit one followed by a lower
                // case letter.
                while (current < length && char.IsUpper(identifier[current]))
                {
                    current++;
                }

                if (current < length && IsLower(identifier[current]))
                {
                    // hit the 'o' in XMLDo.  Return "XML"
                    Debug.Assert(char.IsUpper(identifier[current - 1]));
                    var end = current - 1;
                    return new TextSpan(wordStart, end - wordStart);
                }
                else
                {
                    // Hit something else (punctuation, end of string, etc.)
                    // return the entire upper-case section.
                    return new TextSpan(wordStart, current - wordStart);
                }
            }
            else if (IsLower(c))
            {
                // "Do"
                // 
                // scan the lowercase letters from here on to scan out 'Document'.
                return ScanLowerCaseRun(identifier, length, wordStart);
            }
            else
            {
                return new TextSpan(wordStart, 1);
            }
        }

        private static TextSpan ScanLowerCaseRun(string identifier, int length, int wordStart)
        {
            var current = wordStart + 1;
            while (current < length && IsLower(identifier[current]))
            {
                current++;
            }

            return new TextSpan(wordStart, current - wordStart);
        }

        private static TextSpan ScanNumber(string identifier, int length, int wordStart)
        {
            var current = wordStart + 1;
            while (current < length && char.IsDigit(identifier[current]))
            {
                current++;
            }

            return TextSpan.FromBounds(wordStart, current);
        }

        private static int SkipPunctuation(string identifier, int length, int wordStart)
        {
            while (wordStart < length)
            {
                var ch = identifier[wordStart];
                if (ch != '_' && char.IsPunctuation(ch))
                {
                    wordStart++;
                    continue;
                }

                break;
            }

            return wordStart;
        }

        private static bool IsLower(char c)
        {
            if (IsAscii(c))
            {
                return c >= 'a' && c <= 'z';
            }

            return char.IsLower(c);
        }

        private static bool IsAscii(char v)
            => v < 0x80;
    }
}
