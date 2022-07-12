// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal abstract class CSharpAbstractRenameRewriter : CSharpSyntaxRewriter
    {
        protected readonly DocumentId _documentId;
        protected readonly Solution _solution;
        protected readonly ISet<TextSpan> _conflictLocations;
        protected readonly SemanticModel _semanticModel;
        protected readonly CancellationToken _cancellationToken;
        protected readonly RenamedSpansTracker _renameSpansTracker;
        protected readonly ISimplificationService _simplificationService;
        protected readonly ISemanticFactsService _semanticFactsService;
        protected readonly HashSet<SyntaxToken> _annotatedIdentifierTokens = new();
        protected readonly HashSet<InvocationExpressionSyntax> _invocationExpressionsNeedingConflictChecks = new();

        protected readonly AnnotationTable<RenameAnnotation> _renameAnnotations;

        protected bool AnnotateForComplexification
        {
            get
            {
                return _skipRenameForComplexification > 0 && !_isProcessingComplexifiedSpans;
            }
        }

        protected List<(TextSpan oldSpan, TextSpan newSpan)>? _modifiedSubSpans;
        protected bool _isProcessingComplexifiedSpans;
        protected SemanticModel? _speculativeModel;

        private int _skipRenameForComplexification;

        protected CSharpAbstractRenameRewriter(
            Document document,
            Solution solution,
            ISet<TextSpan> conflictLocations,
            SemanticModel semanticModel,
            RenamedSpansTracker renameSpansTracker,
            AnnotationTable<RenameAnnotation> renameAnnotations,
            CancellationToken cancellationToken) : base(visitIntoStructuredTrivia: true)
        {
            _documentId = document.Id;
            _solution = solution;
            _conflictLocations = conflictLocations;
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
            _renameSpansTracker = renameSpansTracker;

            _simplificationService = document.GetRequiredLanguageService<ISimplificationService>();
            _semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();

            _annotatedIdentifierTokens = new();
            _invocationExpressionsNeedingConflictChecks = new();
            _renameAnnotations = renameAnnotations;
            _modifiedSubSpans = new();
            _isProcessingComplexifiedSpans = false;
            _skipRenameForComplexification = 0;
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node == null)
            {
                return node;
            }

            var isInConflictLambdaBody = false;
            var lambdas = node.GetAncestorsOrThis(n => n is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax);
            if (lambdas.Any())
            {
                foreach (var lambda in lambdas)
                {
                    if (_conflictLocations.Any(cf => cf.Contains(lambda.Span)))
                    {
                        isInConflictLambdaBody = true;
                        break;
                    }
                }
            }

            var shouldComplexifyNode = ShouldComplexifyNode(node, isInConflictLambdaBody);

            SyntaxNode result;

            // in case the current node was identified as being a complexification target of
            // a previous node, we'll handle it accordingly.
            if (shouldComplexifyNode)
            {
                _skipRenameForComplexification++;
                result = base.Visit(node)!;
                _skipRenameForComplexification--;
                result = Complexify(node, result);
            }
            else
            {
                result = base.Visit(node)!;
            }

            return result;
        }

        private SyntaxNode Complexify(SyntaxNode originalNode, SyntaxNode newNode)
        {
            _isProcessingComplexifiedSpans = true;
            _modifiedSubSpans = new List<(TextSpan oldSpan, TextSpan newSpan)>();

            var annotation = new SyntaxAnnotation();
            newNode = newNode.WithAdditionalAnnotations(annotation);
            var speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode);
            newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

            _speculativeModel = CSharpRenameConflictLanguageService.GetSemanticModelForNode(newNode, _semanticModel);
            RoslynDebug.Assert(_speculativeModel != null, "expanding a syntax node which cannot be speculated?");

            var oldSpan = originalNode.Span;
            var expandParameter = originalNode.GetAncestorsOrThis(n => n is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax).Count() == 0;

            newNode = _simplificationService.Expand(
                newNode,
                _speculativeModel,
                annotationForReplacedAliasIdentifier: null,
                expandInsideNode: null,
                expandParameter: expandParameter,
                cancellationToken: _cancellationToken);
            speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode);
            newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

            _speculativeModel = CSharpRenameConflictLanguageService.GetSemanticModelForNode(newNode, _semanticModel);

            newNode = base.Visit(newNode)!;
            var newSpan = newNode.Span;

            newNode = newNode.WithoutAnnotations(annotation);
            newNode = _renameAnnotations.WithAdditionalAnnotations(newNode, new RenameNodeSimplificationAnnotation() { OriginalTextSpan = oldSpan });

            _renameSpansTracker.AddComplexifiedSpan(_documentId, oldSpan, new TextSpan(oldSpan.Start, newSpan.Length), _modifiedSubSpans);
            _modifiedSubSpans = null;

            _isProcessingComplexifiedSpans = false;
            _speculativeModel = null;
            return newNode;
        }

        protected bool ShouldComplexifyNode(SyntaxNode node, bool isInConflictLambdaBody)
        {
            return !isInConflictLambdaBody &&
                   _skipRenameForComplexification == 0 &&
                   !_isProcessingComplexifiedSpans &&
                   _conflictLocations.Contains(node.Span) &&
                   (node is AttributeSyntax ||
                    node is AttributeArgumentSyntax ||
                    node is ConstructorInitializerSyntax ||
                    node is ExpressionSyntax ||
                    node is FieldDeclarationSyntax ||
                    node is StatementSyntax ||
                    node is CrefSyntax ||
                    node is XmlNameAttributeSyntax ||
                    node is TypeConstraintSyntax ||
                    node is BaseTypeSyntax);
        }

        protected static bool IsPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => IsGetPropertyAccessorNameConflict(token, replacementText)
            || IsSetPropertyAccessorNameConflict(token, replacementText)
            || IsInitPropertyAccessorNameConflict(token, replacementText);

        protected static bool IsGetPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.GetKeyword)
            && IsNameConflictWithProperty("get", token.Parent as AccessorDeclarationSyntax, replacementText);

        protected static bool IsSetPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.SetKeyword)
            && IsNameConflictWithProperty("set", token.Parent as AccessorDeclarationSyntax, replacementText);

        protected static bool IsInitPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.InitKeyword)
            // using "set" here is intentional. The compiler generates set_PropName for both set and init accessors.
            && IsNameConflictWithProperty("set", token.Parent as AccessorDeclarationSyntax, replacementText);

        protected static bool IsNameConflictWithProperty(string prefix, AccessorDeclarationSyntax? accessor, string replacementText)
            => accessor?.Parent?.Parent is PropertyDeclarationSyntax property   // 3 null checks in one: accessor -> accessor list -> property declaration
            && replacementText.Equals(prefix + "_" + property.Identifier.Text, StringComparison.Ordinal);

        protected static bool IsPossiblyDestructorConflict(SyntaxToken token, string replacementText)
        {
            return replacementText == "Finalize" &&
                token.IsKind(SyntaxKind.IdentifierToken) &&
                token.Parent.IsKind(SyntaxKind.DestructorDeclaration);
        }

    }
}
