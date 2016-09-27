// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class TypeDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<TypeDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            TypeDeclarationSyntax typeDeclaration,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(typeDeclaration, spans);

            if (!typeDeclaration.OpenBraceToken.IsMissing &&
                !typeDeclaration.CloseBraceToken.IsMissing)
            {
                var lastToken = typeDeclaration.TypeParameterList == null
                    ? typeDeclaration.Identifier
                    : typeDeclaration.TypeParameterList.GetLastToken(includeZeroWidth: true);

                var type = typeDeclaration.Kind() == SyntaxKind.InterfaceDeclaration
                    ? BlockTypes.Interface
                    : typeDeclaration.Kind() == SyntaxKind.StructDeclaration
                        ? BlockTypes.Structure
                        : BlockTypes.Class;
                spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                    typeDeclaration,
                    lastToken,
                    autoCollapse: false,
                    type: type,
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