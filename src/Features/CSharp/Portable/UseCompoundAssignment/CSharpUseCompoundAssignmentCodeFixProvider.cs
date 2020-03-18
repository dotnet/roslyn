// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseCompoundAssignmentCodeFixProvider
        : AbstractUseCompoundAssignmentCodeFixProvider<SyntaxKind, AssignmentExpressionSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpUseCompoundAssignmentCodeFixProvider()
            : base(Utilities.Kinds)
        {
        }

        protected override SyntaxToken Token(SyntaxKind kind)
            => SyntaxFactory.Token(kind);

        protected override AssignmentExpressionSyntax Assignment(
            SyntaxKind assignmentOpKind, ExpressionSyntax left, SyntaxToken syntaxToken, ExpressionSyntax right)
        {
            return SyntaxFactory.AssignmentExpression(assignmentOpKind, left, syntaxToken, right);
        }
    }
}
