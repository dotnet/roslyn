// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Rename;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
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
        protected readonly ISyntaxFactsService _syntaxFactsService;
        protected readonly HashSet<SyntaxToken> _annotatedIdentifierTokens = new();
        protected readonly HashSet<InvocationExpressionSyntax> _invocationExpressionsNeedingConflictChecks = new();

        protected readonly AnnotationTable<RenameAnnotation> _renameAnnotations;

        protected bool AnnotateForComplexification => _skipRenameForComplexification > 0 && !_isProcessingComplexifiedSpans;

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
            _syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

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

        protected void AddModifiedSpan(TextSpan oldSpan, TextSpan newSpan)
        {
            newSpan = new TextSpan(oldSpan.Start, newSpan.Length);

            if (!_isProcessingComplexifiedSpans)
            {
                _renameSpansTracker.AddModifiedSpan(_documentId, oldSpan, newSpan);
            }
            else
            {
                RoslynDebug.Assert(_modifiedSubSpans != null);
                _modifiedSubSpans.Add((oldSpan, newSpan));
            }
        }

        protected async Task<SyntaxToken> RenameAndAnnotateAsync(
            SyntaxToken token,
            SyntaxToken newToken,
            bool isRenameLocation,
            bool isOldText,
            bool isVerbatim,
            bool replacementTextValid,
            bool IsRenamableAccessor,
            string replacementText,
            string originalText,
            RenameAnnotation renameRenamableSymbolDeclaration,
            Location? renamableDeclarationLocation)
        {
            try
            {
                if (_isProcessingComplexifiedSpans)
                {
                    // Rename Token
                    if (isRenameLocation)
                    {
                        var annotation = _renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().FirstOrDefault();
                        if (annotation != null)
                        {
                            newToken = RenameToken(token, newToken, annotation.Prefix, annotation.Suffix, isVerbatim, replacementText, originalText, replacementTextValid);
                            AddModifiedSpan(annotation.OriginalSpan, newToken.Span);
                        }
                        else
                        {
                            newToken = RenameToken(token, newToken, prefix: null, suffix: null, isVerbatim, replacementText, originalText, replacementTextValid);
                        }
                    }

                    return newToken;
                }

                var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Workspace.Services, _cancellationToken);

                string? suffix = null;
                var prefix = isRenameLocation && IsRenamableAccessor
                    ? newToken.ValueText.Substring(0, newToken.ValueText.IndexOf('_') + 1)
                    : null;

                if (symbols.Length == 1)
                {
                    var symbol = symbols[0];

                    if (symbol.IsConstructor())
                    {
                        symbol = symbol.ContainingSymbol;
                    }

                    var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(symbol, _solution, _cancellationToken).ConfigureAwait(false);
                    symbol = sourceDefinition ?? symbol;

                    if (symbol is INamedTypeSymbol namedTypeSymbol)
                    {
                        if (namedTypeSymbol.IsImplicitlyDeclared &&
                            namedTypeSymbol.IsDelegateType() &&
                            namedTypeSymbol.AssociatedSymbol != null)
                        {
                            suffix = "EventHandler";
                        }
                    }

                    // This is a conflicting namespace declaration token. Even if the rename results in conflict with this namespace
                    // conflict is not shown for the namespace so we are tracking this token
                    if (!isRenameLocation && symbol is INamespaceSymbol && token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword))
                    {
                        return newToken;
                    }
                }

                // Rename Token
                if (isRenameLocation && !this.AnnotateForComplexification)
                {
                    var oldSpan = token.Span;
                    newToken = RenameToken(token, newToken, prefix, suffix, isVerbatim, replacementText, originalText, replacementTextValid);

                    AddModifiedSpan(oldSpan, newToken.Span);
                }

                var renameDeclarationLocations = await
                    ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(false);

                var isNamespaceDeclarationReference = false;
                if (isRenameLocation && token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword))
                {
                    isNamespaceDeclarationReference = true;
                }

                var isMemberGroupReference = _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken);

                var renameAnnotation =
                        new RenameActionAnnotation(
                            token.Span,
                            isRenameLocation,
                            prefix,
                            suffix,
                            renameDeclarationLocations: renameDeclarationLocations,
                            isOriginalTextLocation: isOldText,
                            isNamespaceDeclarationReference: isNamespaceDeclarationReference,
                            isInvocationExpression: false,
                            isMemberGroupReference: isMemberGroupReference);

                newToken = _renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span });

                _annotatedIdentifierTokens.Add(token);
                if (renameRenamableSymbolDeclaration != null && renamableDeclarationLocation == token.GetLocation())
                {
                    newToken = _renameAnnotations.WithAdditionalAnnotations(newToken, renameRenamableSymbolDeclaration);
                }

                return newToken;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected static SyntaxToken RenameToken(
            SyntaxToken oldToken,
            SyntaxToken newToken,
            string? prefix,
            string? suffix,
            bool isVerbatim,
            string replacementText,
            string originalText,
            bool replacementTextValid)
        {
            var parent = oldToken.Parent!;
            var currentNewIdentifier = isVerbatim ? replacementText.Substring(1) : replacementText;
            var oldIdentifier = newToken.ValueText;
            var isAttributeName = SyntaxFacts.IsAttributeName(parent);

            if (isAttributeName)
            {
                if (oldIdentifier != originalText)
                {
                    if (currentNewIdentifier.TryGetWithoutAttributeSuffix(out var withoutSuffix))
                    {
                        currentNewIdentifier = withoutSuffix;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    currentNewIdentifier = prefix + currentNewIdentifier;
                }

                if (!string.IsNullOrEmpty(suffix))
                {
                    currentNewIdentifier += suffix;
                }
            }

            // determine the canonical identifier name (unescaped, no unicode escaping, ...)
            var valueText = currentNewIdentifier;
            var kind = SyntaxFacts.GetKeywordKind(currentNewIdentifier);
            if (kind != SyntaxKind.None)
            {
                valueText = SyntaxFacts.GetText(kind);
            }
            else
            {
                var parsedIdentifier = SyntaxFactory.ParseName(currentNewIdentifier);
                if (parsedIdentifier.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax? identifierName))
                {
                    valueText = identifierName.Identifier.ValueText;
                }
            }

            // TODO: we can't use escaped unicode characters in xml doc comments, so we need to pass the valuetext as text as well.
            // <param name="\u... is invalid.

            // if it's an attribute name we don't mess with the escaping because it might change overload resolution
            newToken = isVerbatim || (isAttributeName && oldToken.IsVerbatimIdentifier())
                ? newToken.CopyAnnotationsTo(SyntaxFactory.VerbatimIdentifier(newToken.LeadingTrivia, currentNewIdentifier, valueText, newToken.TrailingTrivia))
                : newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(newToken.LeadingTrivia, SyntaxKind.IdentifierToken, currentNewIdentifier, valueText, newToken.TrailingTrivia));

            if (replacementTextValid)
            {
                if (newToken.IsVerbatimIdentifier())
                {
                    // a reference location should always be tried to be unescaped, whether it was escaped before rename 
                    // or the replacement itself is escaped.
                    newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation);
                }
                else
                {
                    newToken = CSharpSimplificationHelpers.TryEscapeIdentifierToken(newToken, parent);
                }
            }

            return newToken;
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var result = base.VisitInvocationExpression(node);
            RoslynDebug.AssertNotNull(result);

            if (_invocationExpressionsNeedingConflictChecks.Contains(node))
            {
                var renameAnnotation = GetAnnotationForInvocationExpression(node);
                if (renameAnnotation != null)
                {
                    result = _renameAnnotations.WithAdditionalAnnotations(result, renameAnnotation);
                }
            }

            return result;
        }

        private RenameActionAnnotation? GetAnnotationForInvocationExpression(InvocationExpressionSyntax invocationExpression)
        {
            var identifierToken = default(SyntaxToken);
            var expressionOfInvocation = invocationExpression.Expression;

            while (expressionOfInvocation != null)
            {
                switch (expressionOfInvocation.Kind())
                {
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        identifierToken = ((SimpleNameSyntax)expressionOfInvocation).Identifier;
                        break;

                    case SyntaxKind.SimpleMemberAccessExpression:
                        identifierToken = ((MemberAccessExpressionSyntax)expressionOfInvocation).Name.Identifier;
                        break;

                    case SyntaxKind.QualifiedName:
                        identifierToken = ((QualifiedNameSyntax)expressionOfInvocation).Right.Identifier;
                        break;

                    case SyntaxKind.AliasQualifiedName:
                        identifierToken = ((AliasQualifiedNameSyntax)expressionOfInvocation).Name.Identifier;
                        break;

                    case SyntaxKind.ParenthesizedExpression:
                        expressionOfInvocation = ((ParenthesizedExpressionSyntax)expressionOfInvocation).Expression;
                        continue;
                }

                break;
            }

            if (identifierToken != default && !_annotatedIdentifierTokens.Contains(identifierToken))
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken);
                IEnumerable<ISymbol> symbols;
                if (symbolInfo.Symbol == null)
                {
                    return null;
                }
                else
                {
                    symbols = SpecializedCollections.SingletonEnumerable(symbolInfo.Symbol);
                }

                var renameDeclarationLocations = ConflictResolver.CreateDeclarationLocationAnnotationsAsync(
                    _solution,
                    symbols,
                    _cancellationToken).WaitAndGetResult_CanCallOnBackground(_cancellationToken);

                var renameAnnotation = new RenameActionAnnotation(
                                            identifierToken.Span,
                                            isRenameLocation: false,
                                            prefix: null,
                                            suffix: null,
                                            renameDeclarationLocations: renameDeclarationLocations,
                                            isOriginalTextLocation: false,
                                            isNamespaceDeclarationReference: false,
                                            isInvocationExpression: true,
                                            isMemberGroupReference: false);

                return renameAnnotation;
            }

            return null;
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

        protected SyntaxTrivia RenameInCommentTrivia(
            SyntaxTrivia trivia,
            ImmutableSortedDictionary<TextSpan, (string replacementText, string matchText)> subSpanToReplacementString)
        {
            var originalString = trivia.ToString();
            var replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(
                originalString,
                subSpanToReplacementString);

            if (replacedString != originalString)
            {
                var oldSpan = trivia.Span;
                var newTrivia = SyntaxFactory.Comment(replacedString);
                AddModifiedSpan(oldSpan, newTrivia.Span);
                return trivia.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newTrivia, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
            }

            return trivia;
        }
    }
}
