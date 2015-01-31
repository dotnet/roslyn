// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveDeclarationNearReference
{
    internal partial class MoveDeclarationNearReferenceCodeRefactoringProvider
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly BlockSyntax oldInnermostBlock;
            private readonly BlockSyntax newInnermostBlock;
            private readonly BlockSyntax oldOutermostBlock;
            private readonly LocalDeclarationStatementSyntax declarationStatement;

            public Rewriter(
                BlockSyntax oldInnermostBlock,
                BlockSyntax newInnermostBlock,
                BlockSyntax oldOutermostBlock,
                LocalDeclarationStatementSyntax declarationStatement)
            {
                this.oldInnermostBlock = oldInnermostBlock;
                this.newInnermostBlock = newInnermostBlock;
                this.oldOutermostBlock = oldOutermostBlock;
                this.declarationStatement = declarationStatement;
            }

            public override SyntaxNode VisitBlock(BlockSyntax oldBlock)
            {
                if (oldBlock == oldInnermostBlock)
                {
                    return newInnermostBlock;
                }

                if (oldBlock == oldOutermostBlock)
                {
                    var statements = SyntaxFactory.List(oldBlock.Statements.Where(s => s != declarationStatement).Select(this.Visit));
                    return oldBlock.WithStatements(statements).WithAdditionalAnnotations(Formatter.Annotation);
                }

                return base.VisitBlock(oldBlock);
            }
        }
    }
}
