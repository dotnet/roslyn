// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCompoundAssignment), Shared]
    internal class CSharpUseCompoundAssignmentCodeFixProvider
        : AbstractUseCompoundAssignmentCodeFixProvider<SyntaxKind, AssignmentExpressionSyntax, ExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

        protected override ExpressionSyntax Increment(ExpressionSyntax left, bool postfix)
            => postfix
                ? Postfix(SyntaxKind.PostIncrementExpression, left)
                : Prefix(SyntaxKind.PreIncrementExpression, left);

        protected override ExpressionSyntax Decrement(ExpressionSyntax left, bool postfix)
            => postfix
                ? Postfix(SyntaxKind.PostDecrementExpression, left)
                : Prefix(SyntaxKind.PreDecrementExpression, left);

        private static ExpressionSyntax Postfix(SyntaxKind kind, ExpressionSyntax operand)
            => SyntaxFactory.PostfixUnaryExpression(kind, operand);

        private static ExpressionSyntax Prefix(SyntaxKind kind, ExpressionSyntax operand)
            => SyntaxFactory.PrefixUnaryExpression(kind, operand);

        protected override SyntaxTriviaList PrepareRightExpressionLeadingTrivia(SyntaxTriviaList initialTrivia) => initialTrivia.SkipWhile(el => el.Kind() is SyntaxKind.WhitespaceTrivia or SyntaxKind.EndOfLineTrivia).ToSyntaxTriviaList();

        protected override bool PreferPostfix(ISyntaxFactsService syntaxFacts, AssignmentExpressionSyntax currentAssignment)
        {
            // in `for (...; x = x + 1)` we prefer to translate that idiomatically as `for (...; x++)`
            if (currentAssignment.Parent is ForStatementSyntax forStatement &&
                forStatement.Incrementors.Contains(currentAssignment))
            {
                return true;
            }

            return base.PreferPostfix(syntaxFacts, currentAssignment);
        }
    }
}
