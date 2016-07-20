using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class PatternMatcher
    {
        /// <summary>
        /// First we break up the pattern given by dots.  Each portion of the pattern between the
        /// dots is a 'Segment'.  The 'Segment' contains information about the entire section of 
        /// text between the dots, as well as information about any individual 'Words' that we 
        /// can break the segment into.
        /// </summary>
        private struct Segment : IDisposable
        {
            // Information about the entire piece of text between the dots.  For example, if the 
            // text between the dots is 'GetKeyword', then TotalTextChunk.Text will be 'GetKeyword' and 
            // TotalTextChunk.CharacterSpans will correspond to 'G', 'et', 'K' and 'eyword'.
            public readonly TextChunk TotalTextChunk;

            // Information about the subwords compromising the total word.  For example, if the 
            // text between the dots is 'GetKeyword', then the subwords will be 'Get' and 'Keyword'
            // Those individual words will have CharacterSpans of ('G' and 'et') and ('K' and 'eyword')
            // respectively.
            public readonly TextChunk[] SubWordTextChunks;

            public Segment(string text, bool verbatimIdentifierPrefixIsWordCharacter, bool allowFuzzyMatching)
            {
                this.TotalTextChunk = new TextChunk(text, allowFuzzyMatching);
                this.SubWordTextChunks = BreakPatternIntoTextChunks(
                    text, verbatimIdentifierPrefixIsWordCharacter, allowFuzzyMatching);
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

            private static int CountTextChunks(string pattern, bool verbatimIdentifierPrefixIsWordCharacter)
            {
                int count = 0;
                int wordLength = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (IsWordChar(pattern[i], verbatimIdentifierPrefixIsWordCharacter))
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

            private static TextChunk[] BreakPatternIntoTextChunks(
                string pattern, bool verbatimIdentifierPrefixIsWordCharacter, bool allowFuzzyMatching)
            {
                int partCount = CountTextChunks(pattern, verbatimIdentifierPrefixIsWordCharacter);

                if (partCount == 0)
                {
                    return SpecializedCollections.EmptyArray<TextChunk>();
                }

                var result = new TextChunk[partCount];
                int resultIndex = 0;
                int wordStart = 0;
                int wordLength = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                    var ch = pattern[i];
                    if (IsWordChar(ch, verbatimIdentifierPrefixIsWordCharacter))
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