// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface IKeywordRecommender<TContext>
    {
        IEnumerable<RecommendedKeyword> RecommendKeywords(int position, TContext context, CancellationToken cancellationToken);
    }
}
