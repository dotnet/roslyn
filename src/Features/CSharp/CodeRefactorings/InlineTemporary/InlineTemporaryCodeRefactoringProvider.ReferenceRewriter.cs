// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class InlineTemporaryCodeRefactoringProvider
    {
        private class ReferenceRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel semanticModel;
            private readonly ILocalSymbol localSymbol;
            private readonly VariableDeclaratorSyntax variableDeclarator;
            private readonly ExpressionSyntax expressionToInline;
            private readonly CancellationToken cancellationToken;

            private ReferenceRewriter(
                SemanticModel semanticModel,
                VariableDeclaratorSyntax variableDeclarator,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                this.semanticModel = semanticModel;
                this.localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
                this.variableDeclarator = variableDeclarator;
                this.expressionToInline = expressionToInline;
                this.cancellationToken = cancellationToken;
            }

            private bool IsReference(SimpleNameSyntax name)
            {
                if (name.Identifier.ValueText != variableDeclarator.Identifier.ValueText)
                {
                    return false;
                }

                var symbol = semanticModel.GetSymbolInfo(name).Symbol;
                return symbol != null
                    && symbol.Equals(this.localSymbol);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsReference(node))
                {
                    if (HasConflict(node, variableDeclarator))
                    {
                        return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));
                    }

                    return this.expressionToInline
                        .Parenthesize()
                        .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);
                }

                return base.VisitIdentifierName(node);
            }

            public override SyntaxNode VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
            {
                var nameEquals = node.NameEquals;
                var expression = node.Expression;
                var identifier = expression as IdentifierNameSyntax;

                if (nameEquals != null || identifier == null || !IsReference(identifier) || HasConflict(identifier, variableDeclarator))
                {
                    return base.VisitAnonymousObjectMemberDeclarator(node);
                }

                // Special case inlining into anonymous types to ensure that we keep property names:
                //
                // E.g.
                //     int x = 42;
                //     var a = new { x; };
                //
                // Should become:
                //     var a = new { x = 42; };
                nameEquals = SyntaxFactory.NameEquals(identifier);
                expression = (ExpressionSyntax)this.Visit(expression);
                return node.Update(nameEquals, expression).WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);
            }

            public static SyntaxNode Visit(
                SemanticModel semanticModel,
                SyntaxNode scope,
                VariableDeclaratorSyntax variableDeclarator,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                var rewriter = new ReferenceRewriter(semanticModel, variableDeclarator, expressionToInline, cancellationToken);
                return rewriter.Visit(scope);
            }
        }
    }
}
