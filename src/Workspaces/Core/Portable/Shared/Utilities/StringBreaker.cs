// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Values returned from StringBreaker routines.
    /// Optimized for short strings with up to 4 spans.
    /// Each span is encoded in a byte using 6 bits for a length and 2 bits as the gap.
    /// Falls back to a <see cref="List{T}"/> if the encoding won't work.
    /// </summary>
    internal struct StringBreaks
    {
        private readonly List<TextSpan> _spans;
        private readonly FourBytes _encodedSpans;

        private const int MaxGap = 3;
        private const int MaxLength = 63;

        private unsafe struct FourBytes
        {
            private fixed byte _bytes[4];

            public byte this[int index]
            {
                get
                {
                    Debug.Assert(index >= 0 && index < 4);
                    fixed (byte* b = _bytes) return b[index];
                }
                set
                {
                    Debug.Assert(index >= 0 && index < 4);
                    fixed (byte* b = _bytes) b[index] = value;
                }
            }
        }

        public static StringBreaks Create(string text, Func<string, int, TextSpan> spanGenerator)
        {
            Debug.Assert(text != null);
            Debug.Assert(spanGenerator != null);

            if (text.Length == 0)
            {
                return default(StringBreaks);
            }

            int b = 0;
            FourBytes encodedBytes;
            for (int i = 0; i < text.Length;)
            {
                var span = spanGenerator(text, i);
                if (span.Length == 0)
                {
                    // All done
                    break;
                }

                Debug.Assert(span.Start >= i, "Bad generator.");

                if (b < 4)
                {
                    int gap = span.Start - i;
                    if (span.Length <= MaxLength && gap <= MaxGap)
                    {
                        encodedBytes[b++] = Encode(gap, span.Length);
                        i = span.End;
                        continue;
                    }
                }

                return CreateFallback(text, spanGenerator);
            }

            return new StringBreaks(encodedBytes);
        }

        private static StringBreaks CreateFallback(string text, Func<string, int, TextSpan> spanGenerator)
        {
            List<TextSpan> list = new List<TextSpan>();
            for (int i = 0; i < text.Length;)
            {
                var span = spanGenerator(text, i);
                if (span.Length == 0)
                {
                    // All done
                    break;
                }

                Debug.Assert(span.Start >= i, "Bad generator.");

                list.Add(span);
                i = span.End;
            }

            return new StringBreaks(list);
        }

        private StringBreaks(FourBytes encodedSpans)
        {
            this._encodedSpans = encodedSpans;
            this._spans = null;
        }

        private StringBreaks(List<TextSpan> spans)
        {
            this._encodedSpans = default(FourBytes);
            this._spans = spans;
        }

        public int Count
        {
            get
            {
                if (_spans != null)
                {
                    return _spans.Count;
                }

                int i;
                for (i = 0; i < 4; i++)
                {
                    if (_encodedSpans[i] == 0) break;
                }

                return i;
            }
        }

        public TextSpan this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new IndexOutOfRangeException("index");
                }

                if (_spans != null)
                {
                    return _spans[index];
                }

                for (int i = 0, start = 0; ; i++)
                {
                    byte b = _encodedSpans[i];
                    if (b == 0)
                    {
                        throw new IndexOutOfRangeException("index");
                    }

                    start += DecodeGap(b);
                    int length = DecodeLength(b);
                    if (i == index)
                    {
                        return new TextSpan(start, length);
                    }

                    start += length;
                }
            }
        }

        private static byte Encode(int gap, int length)
        {
            Debug.Assert(gap >= 0 && gap < MaxGap);
            Debug.Assert(length >= 0 && length < MaxLength);
            return unchecked((byte)((gap << 6) | length));
        }

        private static int DecodeLength(byte b) => b & 0x3F;

        private static int DecodeGap(byte b) => b >> 6;
    }

    internal static class StringBreaker
    {
        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static StringBreaks BreakIntoCharacterParts(string identifier) => StringBreaks.Create(identifier, CharacterPartsGenerator);

        /// <summary>
        /// Breaks an identifier string into constituent parts.
        /// </summary>
        public static StringBreaks BreakIntoWordParts(string identifier) => StringBreaks.Create(identifier, WordPartsGenerator);

        private static TextSpan CharacterPartsGenerator(string identifier, int start) => GenerateSpan(identifier, start, word: false);

        private static TextSpan WordPartsGenerator(string identifier, int start) => GenerateSpan(identifier, start, word: true);

        public static TextSpan GenerateSpan(string identifier, int wordStart, bool word)
        {
            for (int i = wordStart + 1; i < identifier.Length; i++)
            {
                var lastIsDigit = char.IsDigit(identifier[i - 1]);
                var currentIsDigit = char.IsDigit(identifier[i]);

                var transitionFromLowerToUpper = TransitionFromLowerToUpper(identifier, word, i);
                var transitionFromUpperToLower = TransitionFromUpperToLower(identifier, word, i, wordStart);

                if (char.IsPunctuation(identifier[i - 1]) ||
                    char.IsPunctuation(identifier[i]) ||
                    lastIsDigit != currentIsDigit ||
                    transitionFromLowerToUpper ||
                    transitionFromUpperToLower)
                {
                    if (!IsAllPunctuation(identifier, wordStart, i))
                    {
                        return new TextSpan(wordStart, i - wordStart);
                    }

                    wordStart = i;
                }
            }

            if (!IsAllPunctuation(identifier, wordStart, identifier.Length))
            {
                return new TextSpan(wordStart, identifier.Length - wordStart);
            }

            return default(TextSpan);
        }

        private static bool IsAllPunctuation(string identifier, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                var ch = identifier[i];

                // We don't consider _ as punctuation as there may be things with that name.
                if (!char.IsPunctuation(ch) || ch == '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TransitionFromUpperToLower(string identifier, bool word, int index, int wordStart)
        {
            if (word)
            {
                // Cases this supports:
                // 1) IDisposable -> I, Disposable
                // 2) UIElement -> UI, Element
                // 3) HTMLDocument -> HTML, Document
                //
                // etc.
                if (index != wordStart &&
                    index + 1 < identifier.Length)
                {
                    var currentIsUpper = char.IsUpper(identifier[index]);
                    var nextIsLower = char.IsLower(identifier[index + 1]);
                    if (currentIsUpper && nextIsLower)
                    {
                        // We have a transition from an upper to a lower letter here.  But we only
                        // want to break if all the letters that preceded are uppercase.  i.e. if we
                        // have "Foo" we don't want to break that into "F, oo".  But if we have
                        // "IFoo" or "UIFoo", then we want to break that into "I, Foo" and "UI,
                        // Foo".  i.e. the last uppercase letter belongs to the lowercase letters
                        // that follows.  Note: this will make the following not split properly:
                        // "HELLOthere".  However, these sorts of names do not show up in .Net
                        // programs.
                        for (int i = wordStart; i < index; i++)
                        {
                            if (!char.IsUpper(identifier[i]))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TransitionFromLowerToUpper(string identifier, bool word, int index)
        {
            var lastIsUpper = char.IsUpper(identifier[index - 1]);
            var currentIsUpper = char.IsUpper(identifier[index]);

            // See if the casing indicates we're starting a new word. Note: if we're breaking on
            // words, then just seeing an upper case character isn't enough.  Instead, it has to
            // be uppercase and the previous character can't be uppercase. 
            //
            // For example, breaking "AddMetadata" on words would make: Add Metadata
            //
            // on characters would be: A dd M etadata
            //
            // Break "AM" on words would be: AM
            //
            // on characters would be: A M
            //
            // We break the search string on characters.  But we break the symbol name on words.
            var transition = word
                ? (currentIsUpper && !lastIsUpper)
                : currentIsUpper;
            return transition;
        }
    }
}
