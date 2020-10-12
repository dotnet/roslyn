﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class AnonymousMethodExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<AnonymousMethodExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            AnonymousMethodExpressionSyntax anonymousMethod,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
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

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                anonymousMethod,
                startToken,
                lastToken,
                compressEmptyLines: false,
                autoCollapse: false,
                type: BlockTypes.Expression,
                isCollapsible: true));
        }
    }
}
