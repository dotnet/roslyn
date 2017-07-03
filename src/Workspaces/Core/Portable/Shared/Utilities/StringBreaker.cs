// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Values returned from <see cref="StringBreaker"/> routines.
    /// Optimized for short strings with a handful of spans.
    /// Each span is encoded in two bitfields 'gap' and 'length' and these
    /// bitfields are stored in a 32-bit bitmap.
    /// Falls back to a <see cref="List{T}"/> if the encoding won't work.
    /// </summary>
    internal partial struct StringBreaks : IDisposable
    {
        private readonly ArrayBuilder<TextSpan> _spans;
        private readonly EncodedSpans _encodedSpans;

        // These two values may be adjusted. The remaining constants are
        // derived from them. The values are chosen to minimize the number
        // of fallbacks during normal typing. With 5 total bits per span, we
        // can encode up to 6 spans, each as long as 15 chars with 0 or 1 char
        // gap. This is sufficient for the vast majority of framework symbols.
        private const int BitsForGap = 1;
        private const int BitsForLength = 4;

        private const int BitsPerEncodedSpan = BitsForGap + BitsForLength;
        private const int MaxShortSpans = 32 / BitsPerEncodedSpan;
        private const int MaxGap = (1 << BitsForGap) - 1;
        private const int MaxLength = (1 << BitsForLength) - 1;

        public static StringBreaks CreateSpans(string text, bool word)
        {
            Debug.Assert(text != null);
            return TryEncodeSpans(text, word, out var encodedSpans)
                ? new StringBreaks(encodedSpans)
                : new StringBreaks(CreateFallbackList(text, word));
        }

        private static bool TryEncodeSpans(string text, bool word, out EncodedSpans encodedSpans)
        {
            encodedSpans = default(EncodedSpans);
            for (int start = 0, b = 0; start < text.Length;)
            {
                var span = StringBreaker.GenerateSpan(text, start, word);
                if (span.IsEmpty)
                {
                    // All done
                    break;
                }

                int gap = span.Start - start;
                Debug.Assert(gap >= 0, "Bad generator.");

                if (b >= MaxShortSpans ||
                    span.Length > MaxLength ||
                    gap > MaxGap)
                {
                    // Too many spans, or span cannot be encoded.
                    return false;
                }

                encodedSpans[b++] = Encode(gap, span.Length);
                start = span.End;
            }

            return true;
        }

        internal static ArrayBuilder<TextSpan> CreateFallbackList(string text, bool word)
        {
            var list = ArrayBuilder<TextSpan>.GetInstance();
            for (int start = 0; start < text.Length;)
            {
                var span = StringBreaker.GenerateSpan(text, start, word);
                if (span.IsEmpty)
                {
                    // All done
                    break;
                }

                Debug.Assert(span.Start >= start, "Bad generator.");

                list.Add(span);
                start = span.End;
            }

            return list;
        }

        private StringBreaks(EncodedSpans encodedSpans)
        {
            _encodedSpans = encodedSpans;
            _spans = null;
        }

        private StringBreaks(ArrayBuilder<TextSpan> spans)
        {
            _encodedSpans = default(EncodedSpans);
            _spans = spans;
        }

        public void Dispose()
        {
            _spans?.Free();
        }

        public int GetCount()
        {
            if (_spans != null)
            {
                return _spans.Count;
            }

            int i;
            for (i = 0; i < MaxShortSpans; i++)
            {
                if (_encodedSpans[i] == 0)
                {
                    break;
                }
            }

            return i;
        }

        public TextSpan this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                if (_spans != null)
                {
                    return _spans[index];
                }

                for (int i = 0, start = 0; i < MaxShortSpans; i++)
                {
                    byte b = _encodedSpans[i];
                    if (b == 0)
                    {
                        break;
                    }

                    start += DecodeGap(b);
                    int length = DecodeLength(b);
                    if (i == index)
                    {
                        return new TextSpan(start, length);
                    }

                    start += length;
                }

                throw new IndexOutOfRangeException(nameof(index));
            }
        }

        private static byte Encode(int gap, int length)
        {
            Debug.Assert(gap >= 0 && gap <= MaxGap);
            Debug.Assert(length >= 0 && length <= MaxLength);
            return unchecked((byte)((gap << BitsForLength) | length));
        }

        private static int DecodeLength(byte b) => b & MaxLength;

        private static int DecodeGap(byte b) => b >> BitsForLength;
    }

    internal static class StringBreaker
    {
        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static StringBreaks BreakIntoWordParts(string identifier)
            => StringBreaks.CreateSpans(identifier, word: true);

        public static StringBreaks BreakIntoCharacterParts(string identifier)
            => StringBreaks.CreateSpans(identifier, word: false);

        public static TextSpan GenerateSpan(string identifier, int wordStart, bool word)
        {
            int length = identifier.Length;
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
