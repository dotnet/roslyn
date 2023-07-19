// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    internal partial class CSharpInlineTemporaryCodeRefactoringProvider
    {
        private class ReferenceRewriter : CSharpSyntaxRewriter
        {
            private readonly ISet<IdentifierNameSyntax> _conflictReferences;
            private readonly ISet<IdentifierNameSyntax> _nonConflictReferences;
            private readonly ExpressionSyntax _expressionToInline;
            private readonly CancellationToken _cancellationToken;

            private ReferenceRewriter(
                ISet<IdentifierNameSyntax> conflictReferences,
                ISet<IdentifierNameSyntax> nonConflictReferences,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                _conflictReferences = conflictReferences;
                _nonConflictReferences = nonConflictReferences;
                _expressionToInline = expressionToInline;
                _cancellationToken = cancellationToken;
            }

            private ExpressionSyntax UpdateIdentifier(IdentifierNameSyntax node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (_conflictReferences.Contains(node))
                    return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));

                if (_nonConflictReferences.Contains(node))
                    return _expressionToInline;

                return node;
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                var result = UpdateIdentifier(node);
                return result == _expressionToInline
                    ? result.WithTriviaFrom(node)
                    : result;
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
                    return node.Update(
                        SyntaxFactory.NameEquals(identifier), UpdateIdentifier(identifier)).WithTriviaFrom(node);
                }

                return base.VisitAnonymousObjectMemberDeclarator(node);
            }

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Parent is TupleExpressionSyntax tupleExpression &&
                    ShouldAddTupleMemberName(node, out var identifier) &&
                    tupleExpression.Arguments.Count(a => ShouldAddTupleMemberName(a, out _)) == 1)
                {
                    return node.Update(
                        SyntaxFactory.NameColon(identifier), node.RefKindKeyword, UpdateIdentifier(identifier)).WithTriviaFrom(node);
                }

                return base.VisitArgument(node);
            }

            private bool ShouldAddTupleMemberName(ArgumentSyntax node, [NotNullWhen(true)] out IdentifierNameSyntax? identifier)
            {
                if (node.NameColon == null &&
                    node.Expression is IdentifierNameSyntax id &&
                    _nonConflictReferences.Contains(id) &&
                    !SyntaxFacts.IsReservedTupleElementName(id.Identifier.ValueText))
                {
                    identifier = id;
                    return true;
                }

                identifier = null;
                return false;
            }

            public static SyntaxNode Visit(
                SyntaxNode scope,
                ISet<IdentifierNameSyntax> conflictReferences,
                ISet<IdentifierNameSyntax> nonConflictReferences,
                ExpressionSyntax expressionToInline,
                CancellationToken cancellationToken)
            {
                var rewriter = new ReferenceRewriter(conflictReferences, nonConflictReferences, expressionToInline, cancellationToken);
                return rewriter.Visit(scope);
            }
        }
    }
}
