// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {
        /// <summary>
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
            public readonly ArrayBuilder<TextSpan> PatternHumps;

            public readonly WordSimilarityChecker SimilarityChecker;

            public readonly bool IsLowercase;

            public TextChunk(string text, bool allowFuzzingMatching)
            {
                this.Text = text;
                this.PatternHumps = StringBreaker.GetCharacterParts(text);
                this.SimilarityChecker = allowFuzzingMatching
                    ? WordSimilarityChecker.Allocate(text, substringsAreSimilar: false)
                    : null;

                IsLowercase = !ContainsUpperCaseLetter(text);
            }

            public void Dispose()
            {
                this.PatternHumps.Free();
                this.SimilarityChecker?.Free();
            }
        }
    }
}
