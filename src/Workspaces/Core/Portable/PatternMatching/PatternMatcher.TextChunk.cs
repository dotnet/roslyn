﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {        /// <summary>
             /// Information about a chunk of text from the pattern.  The chunk is a piece of text, with 
             /// cached information about the character spans within in.  Character spans separate out
             /// capitalized runs and lowercase runs.  i.e. if you have AAbb, then there will be two 
             /// character spans, one for AA and one for BB.
             /// </summary>
        private struct TextChunk : IDisposable
        {
            public readonly string Text;

            /// <summary>
            /// Character spans separate out
            /// capitalized runs and lowercase runs.  i.e. if you have AAbb, then there will be two 
            /// character spans, one for AA and one for BB.
            /// </summary>
            public readonly StringBreaks CharacterSpans;

            public readonly WordSimilarityChecker SimilarityChecker;

            public TextChunk(string text, bool allowFuzzingMatching)
            {
                this.Text = text;
                this.CharacterSpans = StringBreaker.BreakIntoCharacterParts(text);
                this.SimilarityChecker = allowFuzzingMatching
                    ? new WordSimilarityChecker(text, substringsAreSimilar: false)
                    : null;
            }

            public void Dispose()
            {
                this.SimilarityChecker?.Dispose();
            }
        }
    }
}