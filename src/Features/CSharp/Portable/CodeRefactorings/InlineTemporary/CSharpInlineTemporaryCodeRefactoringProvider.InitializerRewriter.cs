// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class CSharpInlineTemporaryCodeRefactoringProvider
    {
        /// <summary>
        /// This class handles rewriting initializer expressions that refer to the variable
        /// being initialized into a simpler form.  For example, in "int x = x = 1", we want to
        /// get just "1" back as the initializer.
        /// </summary>
        private class InitializerRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ILocalSymbol _localSymbol;

            private InitializerRewriter(ILocalSymbol localSymbol, SemanticModel semanticModel)
            {
                _localSymbol = localSymbol;
                _semanticModel = semanticModel;
            }

            private bool IsReference(SimpleNameSyntax name)
            {
                return name.Identifier.ValueText == _localSymbol.Name &&
                       Equals(_localSymbol, _semanticModel.GetSymbolInfo(name).Symbol);
            }

            public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node is AssignmentExpressionSyntax assignment &&
                    assignment.Left is IdentifierNameSyntax name &&
                    IsReference(name))
                {
                    return base.Visit(assignment.Right);
                }

                return base.VisitAssignmentExpression(node);
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (IsReference(node))
                {
                    if (node.Parent is AssignmentExpressionSyntax assignmentExpression)
                    {
                        if (assignmentExpression.IsCompoundAssignExpression() &&
                            assignmentExpression.Left == node)
                        {
                            return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));
                        }
                    }
                }

                return base.VisitIdentifierName(node);
            }

            //public override SyntaxNode? VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            //{
            //    var newNode = base.VisitParenthesizedExpression(node);

            //    if (node != newNode && newNode?.Kind() == SyntaxKind.ParenthesizedExpression)
            //        return newNode.WithAdditionalAnnotations(Simplifier.Annotation);

            //    return newNode;
            //}

            public static ExpressionSyntax Visit(ExpressionSyntax initializer, ILocalSymbol local, SemanticModel semanticModel)
            {
                var simplifier = new InitializerRewriter(local, semanticModel);
                return (ExpressionSyntax)simplifier.Visit(initializer);
            }
        }
    }
}
