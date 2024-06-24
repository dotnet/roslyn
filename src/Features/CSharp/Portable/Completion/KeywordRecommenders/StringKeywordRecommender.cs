// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class StringKeywordRecommender() : AbstractSpecialTypePreselectingKeywordRecommender(SyntaxKind.StringKeyword)
{
    protected override SpecialType SpecialType => SpecialType.System_String;

    protected override bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsTypeOfExpressionContext ||
            context.SyntaxTree.IsDefaultExpressionContext(position, context.LeftToken);
    }
}
