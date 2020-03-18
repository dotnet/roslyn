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
    internal class TypeDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<TypeDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            TypeDeclarationSyntax typeDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(typeDeclaration, spans);

            if (!typeDeclaration.OpenBraceToken.IsMissing &&
                !typeDeclaration.CloseBraceToken.IsMissing)
            {
                var lastToken = typeDeclaration.TypeParameterList == null
                    ? typeDeclaration.Identifier
                    : typeDeclaration.TypeParameterList.GetLastToken(includeZeroWidth: true);

                spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                    typeDeclaration,
                    lastToken,
                    autoCollapse: false,
                    type: BlockTypes.Type,
                    isCollapsible: true));
            }

            // add any leading comments before the end of the type block
            if (!typeDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = typeDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpStructureHelpers.CollectCommentBlockSpans(leadingTrivia, spans);
            }
        }
    }
}
