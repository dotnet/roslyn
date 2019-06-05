// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class TokenStream
    {
        private interface ITriviaDataGetter
        {
            TriviaData GetTriviaData(in TokenData token1, in TokenData token2);
        }
    }
}
