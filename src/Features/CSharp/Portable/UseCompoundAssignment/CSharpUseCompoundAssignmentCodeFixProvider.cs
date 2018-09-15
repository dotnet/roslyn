// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseCompoundAssignmentCodeFixProvider 
        : AbstractUseCompoundAssignmentCodeFixProvider<SyntaxKind, ExpressionSyntax>
    {
        public CSharpUseCompoundAssignmentCodeFixProvider()
            : base(Maps.BinaryToAssignmentMap,
                   Maps.AssignmentToTokenMap)
        {
        }

        protected override SyntaxKind GetSyntaxKind(int rawKind)
            => (SyntaxKind)rawKind;

        protected override SyntaxToken Token(SyntaxKind kind)
            => SyntaxFactory.Token(kind);

        protected override SyntaxNode AssignmentExpression(
            SyntaxKind assignmentOpKind, ExpressionSyntax left, SyntaxToken syntaxToken, ExpressionSyntax right)
        {
            return SyntaxFactory.AssignmentExpression(assignmentOpKind, left, syntaxToken, right);
        }
    }
}
