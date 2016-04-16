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
            private readonly BlockSyntax _oldInnermostBlock;
            private readonly BlockSyntax _newInnermostBlock;
            private readonly BlockSyntax _oldOutermostBlock;
            private readonly LocalDeclarationStatementSyntax _declarationStatement;

            public Rewriter(
                BlockSyntax oldInnermostBlock,
                BlockSyntax newInnermostBlock,
                BlockSyntax oldOutermostBlock,
                LocalDeclarationStatementSyntax declarationStatement)
            {
                _oldInnermostBlock = oldInnermostBlock;
                _newInnermostBlock = newInnermostBlock;
                _oldOutermostBlock = oldOutermostBlock;
                _declarationStatement = declarationStatement;
            }

            public override SyntaxNode VisitBlock(BlockSyntax oldBlock)
            {
                if (oldBlock == _oldInnermostBlock)
                {
                    return _newInnermostBlock;
                }

                if (oldBlock == _oldOutermostBlock)
                {
                    var statements = SyntaxFactory.List(oldBlock.Statements.Where(s => s != _declarationStatement).Select(this.Visit));
                    return oldBlock.WithStatements(statements).WithAdditionalAnnotations(Formatter.Annotation);
                }

                return base.VisitBlock(oldBlock);
            }
        }
    }
}
