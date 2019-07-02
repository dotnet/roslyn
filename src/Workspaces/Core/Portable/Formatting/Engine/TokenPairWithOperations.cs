// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// it holds onto space and wrapping operation need to run between two tokens.
    /// </summary>
    internal struct TokenPairWithOperations
    {
        public TokenStream TokenStream { get; }
        public AdjustSpacesOperation SpaceOperation { get; }
        public AdjustNewLinesOperation LineOperation { get; }

        public int PairIndex { get; }

        public TokenPairWithOperations(
            TokenStream tokenStream,
            int tokenPairIndex,
            AdjustSpacesOperation spaceOperations,
            AdjustNewLinesOperation lineOperations)
            : this()
        {
            Contract.ThrowIfNull(tokenStream);

            Contract.ThrowIfFalse(0 <= tokenPairIndex && tokenPairIndex < tokenStream.TokenCount - 1);

            this.TokenStream = tokenStream;
            this.PairIndex = tokenPairIndex;

            SpaceOperation = spaceOperations;
            LineOperation = lineOperations;
        }

        public SyntaxToken Token1
        {
            get
            {
                return this.TokenStream.GetToken(this.PairIndex);
            }
        }

        public SyntaxToken Token2
        {
            get
            {
                return this.TokenStream.GetToken(this.PairIndex + 1);
            }
        }
    }
}
