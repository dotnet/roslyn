// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        /// <summary>
        /// Retrieves original <see cref="TriviaData"/> via <see cref="TokenStream.GetOriginalTriviaData(in TokenData, in TokenData)"/>
        /// </summary>
        private struct OriginalTriviaDataGetter : ITriviaDataGetter
        {
            private readonly TokenStream _tokenStream;

            public OriginalTriviaDataGetter(TokenStream tokenStream)
            {
                _tokenStream = tokenStream;
            }

            public TriviaData GetTriviaData(in TokenData token1, in TokenData token2)
                => _tokenStream.GetOriginalTriviaData(in token1, in token2);
        }
    }
}
