// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ConversionOperatorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<ConversionOperatorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            ConversionOperatorDeclarationSyntax operatorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(operatorDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (operatorDeclaration.Body == null ||
                operatorDeclaration.Body.OpenBraceToken.IsMissing ||
                operatorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = operatorDeclaration;
            var nextSibling = current.GetNextSibling();

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                operatorDeclaration,
                operatorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: !nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.ConversionOperatorDeclaration),
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
