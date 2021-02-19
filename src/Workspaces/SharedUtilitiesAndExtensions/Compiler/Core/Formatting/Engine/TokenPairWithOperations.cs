// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// it holds onto space and wrapping operation need to run between two tokens.
    /// </summary>
    internal readonly struct TokenPairWithOperations
    {
        public readonly TokenStream TokenStream;
        public readonly AdjustSpacesOperation SpaceOperation;
        public readonly AdjustNewLinesOperation LineOperation;

        public readonly int PairIndex;

        public TokenPairWithOperations(
            TokenStream tokenStream,
            int tokenPairIndex,
            AdjustSpacesOperation spaceOperations,
            AdjustNewLinesOperation lineOperations)
        {
            Contract.ThrowIfNull(tokenStream);

            Contract.ThrowIfFalse(0 <= tokenPairIndex && tokenPairIndex < tokenStream.TokenCount - 1);

            TokenStream = tokenStream;
            PairIndex = tokenPairIndex;
            SpaceOperation = spaceOperations;
            LineOperation = lineOperations;
        }

        public SyntaxToken Token1
            => this.TokenStream.GetToken(this.PairIndex);

        public SyntaxToken Token2
            => this.TokenStream.GetToken(this.PairIndex + 1);
    }
}
