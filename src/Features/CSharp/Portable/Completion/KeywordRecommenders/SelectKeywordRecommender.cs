// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class SelectKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public SelectKeywordRecommender()
        : base(SyntaxKind.SelectKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var token = context.TargetToken;

        // for orderby, ascending is the default so select should be available in the orderby direction context
        if (token.IsOrderByDirectionContext())
        {
            return true;
        }

        // var q = from x in y
        //         |
        if (!token.IntersectsWith(position) &&
            token.IsLastTokenOfQueryClause())
        {
            return true;
        }

        return false;
    }
}
