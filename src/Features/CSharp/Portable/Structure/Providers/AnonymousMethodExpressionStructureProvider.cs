// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class AnonymousMethodExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<AnonymousMethodExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            AnonymousMethodExpressionSyntax anonymousMethod,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            // fault tolerance
            if (anonymousMethod.Block.IsMissing ||
                anonymousMethod.Block.OpenBraceToken.IsMissing ||
                anonymousMethod.Block.CloseBraceToken.IsMissing)
            {
                return;
            }

            var lastToken = CSharpStructureHelpers.GetLastInlineMethodBlockToken(anonymousMethod);
            if (lastToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            var startToken = anonymousMethod.ParameterList != null
                ? anonymousMethod.ParameterList.GetLastToken(includeZeroWidth: true)
                : anonymousMethod.DelegateKeyword;

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                anonymousMethod,
                startToken,
                lastToken,
                autoCollapse: false,
                type: BlockTypes.AnonymousMethod,
                isCollapsible: true));
        }
    }
}
