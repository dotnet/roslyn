// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class InlineTemporaryCodeRefactoringProvider
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
            private readonly ExpressionSyntax _initializer;

            private InitializerRewriter(ExpressionSyntax initializer, ILocalSymbol localSymbol, SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
                _localSymbol = localSymbol;
                _initializer = initializer;
            }

            private bool IsReference(SimpleNameSyntax name)
            {
                if (name.Identifier.ValueText != _localSymbol.Name)
                {
                    return false;
                }

                var symbol = _semanticModel.GetSymbolInfo(name).Symbol;
                return symbol != null
                    && symbol.Equals(_localSymbol);
            }

            public override SyntaxNode VisitValueAssignmentExpression(ValueAssignmentExpressionSyntax node)
            {
                // Note - leave this as SyntaxNode for now, we might have already re-written it
                var newNode = base.VisitValueAssignmentExpression(node);

                if (newNode.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    // It's okay to just look at the text, since we're explicitly looking for an
                    // identifier standing alone, and we know we're in a local's initializer.
                    // The text can only bind to the initializer.
                    var assignment = (AssignmentExpressionSyntax)newNode;
                    var name = assignment.Left.Kind() == SyntaxKind.IdentifierName
                        ? (IdentifierNameSyntax)assignment.Left
                        : null;

                    if (name != null && IsReference(name))
                    {
                        return assignment.Right;
                    }
                }

                return newNode;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (IsReference(node))
                {
                    var assignmentExpression = node.Parent as AssignmentExpressionSyntax;
                    if (assignmentExpression != null)
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

            public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
            {
                var newNode = base.VisitParenthesizedExpression(node);

                if (node != newNode && newNode.Kind() == SyntaxKind.ParenthesizedExpression)
                {
                    return newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                }

                return newNode;
            }

            public static ExpressionSyntax Visit(ExpressionSyntax initializer, ILocalSymbol local, SemanticModel semanticModel)
            {
                var simplifier = new InitializerRewriter(initializer, local, semanticModel);
                return (ExpressionSyntax)simplifier.Visit(initializer);
            }
        }
    }
}
