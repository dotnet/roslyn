// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class SimpleLambdaExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<SimpleLambdaExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            SimpleLambdaExpressionSyntax lambdaExpression,
            ArrayBuilder<BlockSpan> spans,
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

            var lastToken = CSharpStructureHelpers.GetLastInlineMethodBlockToken(lambdaExpression);
            if (lastToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                lambdaExpression,
                lambdaExpression.ArrowToken,
                lastToken,
                autoCollapse: false,
                type: BlockTypes.AnonymousMethod,
                isCollapsible: true));
        }
    }
}