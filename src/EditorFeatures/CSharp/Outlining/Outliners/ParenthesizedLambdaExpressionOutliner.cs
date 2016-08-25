// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class ParenthesizedLambdaExpressionOutliner : AbstractSyntaxNodeOutliner<ParenthesizedLambdaExpressionSyntax>
    {
        protected override void CollectOutliningSpans(
            ParenthesizedLambdaExpressionSyntax lambdaExpression,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            // fault tolerance
            if (lambdaExpression.Body.IsMissing)
            {
                return;
            }

            var lambdaBlock = lambdaExpression.Body as BlockSyntax;
            if (lambdaBlock == null ||
                lambdaBlock.OpenBraceToken.IsMissing ||
                lambdaBlock.CloseBraceToken.IsMissing)
            {
                return;
            }

            var lastToken = CSharpOutliningHelpers.GetLastInlineMethodBlockToken(lambdaExpression);
            if (lastToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                lambdaExpression,
                lambdaExpression.ArrowToken,
                lastToken,
                autoCollapse: false));
        }
    }
}
