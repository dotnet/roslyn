// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class CSharpInlineTemporaryCodeRefactoringProvider
    {
        private class ReferenceRewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ISet<IdentifierNameSyntax> _conflictReferences;
            private readonly ISet<IdentifierNameSyntax> _nonConflictReferences;
            private readonly ExpressionSyntax _expressionToInline;
            private readonly CancellationToken _cancellationToken;

            private ReferenceRewriter(
                SemanticModel semanticModel,
                ISet<IdentifierNameSyntax> conflictReferences,
                ISet<IdentifierNameSyntax> nonConflictReferences,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _conflictReferences = conflictReferences;
                _nonConflictReferences = nonConflictReferences;
                _expressionToInline = expressionToInline;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (_conflictReferences.Contains(node))
                    return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));

                if (_nonConflictReferences.Contains(node))
                    return _expressionToInline;

                return base.VisitIdentifierName(node);
            }

            public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
            {
                if (node.NameEquals == null &&
                    node.Expression is IdentifierNameSyntax identifier &&
                    _nonConflictReferences.Contains(identifier))
                {

                    // Special case inlining into anonymous types to ensure that we keep property names:
                    //
                    // E.g.
                    //     int x = 42;
                    //     var a = new { x; };
                    //
                    // Should become:
                    //     var a = new { x = 42; };
                    return node.Update(SyntaxFactory.NameEquals(identifier), (ExpressionSyntax)Visit(node.Expression));
                }

                return base.VisitAnonymousObjectMemberDeclarator(node);
            }

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Parent is TupleExpressionSyntax &&
                    node.NameColon == null &&
                    node.Expression is IdentifierNameSyntax identifier &&
                    _nonConflictReferences.Contains(identifier) &&
                    !SyntaxFacts.IsReservedTupleElementName(identifier.Identifier.ValueText))
                {
                    return node.Update(SyntaxFactory.NameColon(identifier), node.RefKindKeyword, (ExpressionSyntax)Visit(node.Expression));
                }

                return base.VisitArgument(node);
            }

            public static SyntaxNode Visit(
                SyntaxNode scope,
                SemanticModel semanticModel,
                ISet<IdentifierNameSyntax> conflictReferences,
                ISet<IdentifierNameSyntax> nonConflictReferences,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                var rewriter = new ReferenceRewriter(semanticModel, conflictReferences, nonConflictReferences, expressionToInline, cancellationToken);
                return rewriter.Visit(scope);
            }
        }
    }
}
