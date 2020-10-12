﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {
        /// <summary>
        /// First we break up the pattern given by dots.  Each portion of the pattern between the
        /// dots is a 'Segment'.  The 'Segment' contains information about the entire section of 
        /// text between the dots, as well as information about any individual 'Words' that we 
        /// can break the segment into.
        /// </summary>
        private struct PatternSegment : IDisposable
        {
            // Information about the entire piece of text between the dots.  For example, if the 
            // text between the dots is 'Get-Keyword', then TotalTextChunk.Text will be 'Get-Keyword' and 
            // TotalTextChunk.CharacterSpans will correspond to 'G', 'et', 'K' and 'eyword'.
            public readonly TextChunk TotalTextChunk;

            // Information about the subwords compromising the total word.  For example, if the 
            // text between the dots is 'Get-Keyword', then the subwords will be 'Get' and 'Keyword'
            // Those individual words will have CharacterSpans of ('G' and 'et') and ('K' and 'eyword')
            // respectively.
            public readonly TextChunk[] SubWordTextChunks;

            public PatternSegment(string text, bool allowFuzzyMatching)
            {
                this.TotalTextChunk = new TextChunk(text, allowFuzzyMatching);
                this.SubWordTextChunks = BreakPatternIntoSubWords(text, allowFuzzyMatching);
            }

            public void Dispose()
            {
                this.TotalTextChunk.Dispose();
                foreach (var chunk in this.SubWordTextChunks)
                {
                    chunk.Dispose();
                }
            }

            public bool IsInvalid => this.SubWordTextChunks.Length == 0;

            private static int CountTextChunks(string pattern)
            {
                var count = 0;
                var wordLength = 0;

                for (var i = 0; i < pattern.Length; i++)
                {
                    if (IsWordChar(pattern[i]))
                    {
                        wordLength++;
                    }
                    else
                    {
                        if (wordLength > 0)
                        {
                            count++;
                            wordLength = 0;
                        }
                    }
                }

                if (wordLength > 0)
                {
                    count++;
                }

                return count;
            }

            private static TextChunk[] BreakPatternIntoSubWords(string pattern, bool allowFuzzyMatching)
            {
                var partCount = CountTextChunks(pattern);

                if (partCount == 0)
                {
                    return Array.Empty<TextChunk>();
                }

                var result = new TextChunk[partCount];
                var resultIndex = 0;
                var wordStart = 0;
                var wordLength = 0;

                for (var i = 0; i < pattern.Length; i++)
                {
                    var ch = pattern[i];
                    if (IsWordChar(ch))
                    {
                        if (wordLength++ == 0)
                        {
                            wordStart = i;
                        }
                    }
                    else
                    {
                        if (wordLength > 0)
                        {
                            result[resultIndex++] = new TextChunk(pattern.Substring(wordStart, wordLength), allowFuzzyMatching);
                            wordLength = 0;
                        }
                    }
                }

                if (wordLength > 0)
                {
                    result[resultIndex++] = new TextChunk(pattern.Substring(wordStart, wordLength), allowFuzzyMatching);
                }

                return result;
            }
        }
    }
}
