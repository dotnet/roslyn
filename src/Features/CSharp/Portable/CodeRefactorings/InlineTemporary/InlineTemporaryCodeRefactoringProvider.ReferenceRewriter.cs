//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Threading;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Shared.Extensions;
//using Microsoft.CodeAnalysis.Shared;
//using Microsoft.CodeAnalysis.CSharp.Extensions;

//namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary;

//internal sealed partial class CSharpInlineTemporaryCodeRefactoringProvider
//{
//    private sealed class ReferenceRewriter(
//        ISet<IdentifierNameSyntax> conflictReferences,
//        ISet<IdentifierNameSyntax> nonConflictReferences,
//        ExpressionSyntax expressionToInline,
//        ExpressionSyntax originalDeclaratorExpression,
//        CancellationToken cancellationToken) : CSharpSyntaxRewriter
//    {
//        private readonly ISet<IdentifierNameSyntax> _conflictReferences = conflictReferences;
//        private readonly ISet<IdentifierNameSyntax> _nonConflictReferences = nonConflictReferences;
//        private readonly ExpressionSyntax _expressionToInline = expressionToInline;
//        private readonly ExpressionSyntax _originalDeclaratorExpression = originalDeclaratorExpression;
//        private readonly CancellationToken _cancellationToken = cancellationToken;

//        private ExpressionSyntax UpdateIdentifier(IdentifierNameSyntax node)
//        {
//            _cancellationToken.ThrowIfCancellationRequested();

//            if (_conflictReferences.Contains(node))
//                return node.Update(node.Identifier.WithAdditionalAnnotations(CreateConflictAnnotation()));

//            if (_nonConflictReferences.Contains(node))
//                return _expressionToInline;

//            return node;
//        }

//        private bool ShouldSpreadCollectionIntoCollection(
//            ExpressionSyntax expressions,
//            [NotNullWhen(true)] out CollectionExpressionSyntax? collectionToInline)
//        {
//            if (expressions is not IdentifierNameSyntax identifier ||
//                !_nonConflictReferences.Contains(identifier) ||
//                expressions.Parent is not SpreadElementSyntax)
//            {
//                collectionToInline = null;
//                return false;
//            }

//            collectionToInline = _originalDeclaratorExpression as CollectionExpressionSyntax;
//            return collectionToInline != null;
//        }

//        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
//        {
//            // Special case inlining a collection into a spread element.  We can just move the original elements into
//            // the spreaded location.
//            if (ShouldSpreadCollectionIntoCollection(node, out _))
//                return node;

//            var result = UpdateIdentifier(node);
//            return result == _expressionToInline
//                ? result.WithTriviaFrom(node)
//                : result;
//        }

//        public override SyntaxNode? VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
//        {
//            if (node.NameEquals == null &&
//                node.Expression is IdentifierNameSyntax identifier &&
//                _nonConflictReferences.Contains(identifier))
//            {

//            }

//            return base.VisitAnonymousObjectMemberDeclarator(node);
//        }

//        public override SyntaxNode? VisitArgument(ArgumentSyntax node)
//        {
//            if (node.Parent is TupleExpressionSyntax tupleExpression &&
//                ShouldAddTupleMemberName(node, out var identifier) &&
//                tupleExpression.Arguments.Count(a => ShouldAddTupleMemberName(a, out _)) == 1)
//            {
//                return node.Update(
//                    SyntaxFactory.NameColon(identifier), node.RefKindKeyword, UpdateIdentifier(identifier)).WithTriviaFrom(node);
//            }

//            return base.VisitArgument(node);
//        }

//        private bool ShouldAddTupleMemberName(ArgumentSyntax node, [NotNullWhen(true)] out IdentifierNameSyntax? identifier)
//        {
//            if (node.NameColon == null &&
//                node.Expression is IdentifierNameSyntax id &&
//                _nonConflictReferences.Contains(id) &&
//                !SyntaxFacts.IsReservedTupleElementName(id.Identifier.ValueText))
//            {
//                identifier = id;
//                return true;
//            }

//            identifier = null;
//            return false;
//        }

//        public override SyntaxNode? VisitCollectionExpression(CollectionExpressionSyntax node)
//        {


//            node.Update(VisitToken(node.OpenBracketToken), VisitList(node.Elements), VisitToken(node.CloseBracketToken))
//        }

//        public override SyntaxNode? VisitSpreadElement(SpreadElementSyntax node)
//        {
//            if (!ShouldSpreadCollectionIntoCollection(node.Expression, out var collectionToInline))
//                return node;

//            // inlining an existing `[...]` collection into a `..` spread element.  We can just move the original
//            // elements into the final location.
//            var leadingTrivia = node.GetLeadingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia) lastWhitespace]
//                ? lastWhitespace
//                : default;
//        }
//    }
//}
