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
    internal class EnumDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EnumDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            EnumDeclarationSyntax enumDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(enumDeclaration, spans);

            if (!enumDeclaration.OpenBraceToken.IsMissing &&
                !enumDeclaration.CloseBraceToken.IsMissing)
            {
                spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                    enumDeclaration,
                    enumDeclaration.Identifier,
                    autoCollapse: false,
                    type: BlockTypes.Member,
                    isCollapsible: true));
            }

            // add any leading comments before the end of the type block
            if (!enumDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = enumDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpStructureHelpers.CollectCommentBlockSpans(leadingTrivia, spans);
            }
        }
    }
}
