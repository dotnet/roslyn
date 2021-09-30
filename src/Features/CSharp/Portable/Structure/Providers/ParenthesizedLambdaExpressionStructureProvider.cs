﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ParenthesizedLambdaExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<ParenthesizedLambdaExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            ParenthesizedLambdaExpressionSyntax lambdaExpression,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider,
            CancellationToken cancellationToken)
        {
            // fault tolerance
            if (lambdaExpression.Body.IsMissing)
            {
                return;
            }

            if (!(lambdaExpression.Body is BlockSyntax lambdaBlock) ||
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

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                lambdaExpression,
                lambdaExpression.ArrowToken,
                lastToken,
                compressEmptyLines: false,
                autoCollapse: false,
                type: BlockTypes.Expression,
                isCollapsible: true));
        }
    }
}
