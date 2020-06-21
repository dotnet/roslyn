// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class InlineTemporaryCodeRefactoringProvider
    {
        private class ReferenceRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ILocalSymbol _localSymbol;
            private readonly VariableDeclaratorSyntax _variableDeclarator;
            private readonly ExpressionSyntax _expressionToInline;
            private readonly CancellationToken _cancellationToken;

            private ReferenceRewriter(
                SemanticModel semanticModel,
                VariableDeclaratorSyntax variableDeclarator,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
                _variableDeclarator = variableDeclarator;
                _expressionToInline = expressionToInline;
                _cancellationToken = cancellationToken;
            }

            private bool IsReference(SimpleNameSyntax name)
            {
                if (name.Identifier.ValueText != _variableDeclarator.Identifier.ValueText)
                {
                    return false;
                }

                var symbol = _semanticModel.GetSymbolInfo(name).Symbol;
                return symbol != null
                    && symbol.Equals(_localSymbol);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (IsReference(node))
                {
                    if (HasConflict(node, _variableDeclarator))
                    {
                        return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));
                    }

                    return _expressionToInline
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

                if (nameEquals != null || identifier == null || !IsReference(identifier) || HasConflict(identifier, _variableDeclarator))
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
                expression = (ExpressionSyntax)Visit(expression);
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
