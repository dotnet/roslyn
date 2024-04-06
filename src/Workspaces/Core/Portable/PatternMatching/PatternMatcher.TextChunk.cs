// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal partial class PatternMatcher
{
    /// <summary>
    /// Information about a chunk of text from the pattern.  The chunk is a piece of text, with 
    /// cached information about the character spans within in.  Character spans separate out
    /// capitalized runs and lowercase runs.  i.e. if you have AAbb, then there will be two 
    /// character spans, one for AA and one for BB.
    /// </summary>
    [NonCopyable]
    private struct TextChunk : IDisposable
    {
        public readonly string Text;

        /// <summary>
        /// Character spans separate out
        /// capitalized runs and lowercase runs.  i.e. if you have AAbb, then there will be two 
        /// character spans, one for AA and one for BB.
        /// </summary>
        public TemporaryArray<TextSpan> PatternHumps;

        /// <summary>
        /// Not readonly as this value caches data within it, and so it needs to be able to mutate.
        /// </summary>
        public WordSimilarityChecker SimilarityChecker;

        public readonly bool IsLowercase;

        public TextChunk(string text, bool allowFuzzingMatching)
        {
            this.Text = text;
            PatternHumps = TemporaryArray<TextSpan>.Empty;
            StringBreaker.AddCharacterParts(text, ref PatternHumps);

            this.SimilarityChecker = allowFuzzingMatching
                ? new WordSimilarityChecker(text, substringsAreSimilar: false)
                : default;

            IsLowercase = !ContainsUpperCaseLetter(text);
        }

        public void Dispose()
        {
            this.PatternHumps.Dispose();
            this.SimilarityChecker.Dispose();
        }
    }
}
