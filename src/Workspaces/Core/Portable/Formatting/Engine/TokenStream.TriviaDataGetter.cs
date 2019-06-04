// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        /// <summary>
        /// Retrieves <see cref="TriviaData"/> via <see cref="TokenStream.GetTriviaData(TokenData, TokenData)"/>
        /// </summary>
        private struct TriviaDataGetter : ITriviaDataGetter
        {
            private readonly TokenStream _tokenStream;
            public TriviaDataGetter(TokenStream tokenStream)
            {
                _tokenStream = tokenStream;
            }

            public TriviaData GetTriviaData(TokenData token1, TokenData token2)
                => _tokenStream.GetTriviaData(token1, token2);
        }
    }
}
