// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Rename.RenameUtilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal sealed class SymbolsRenameRewriter : CSharpSyntaxRewriter
    {
        private readonly DocumentId _documentId;
        private readonly Solution _solution;
        private readonly ISet<TextSpan> _conflictLocations;
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;
        private readonly RenamedSpansTracker _renameSpansTracker;
        private readonly ISimplificationService _simplificationService;
        private readonly ISemanticFactsService _semanticFactsService;
        private readonly ISyntaxFactsService _syntaxFactsService;
        private readonly HashSet<SyntaxToken> _annotatedIdentifierTokens = new();
        private readonly HashSet<InvocationExpressionSyntax> _invocationExpressionsNeedingConflictChecks = new();

        private readonly AnnotationTable<RenameAnnotation> _renameAnnotations;

        private bool AnnotateForComplexification => _skipRenameForComplexification > 0 && !_isProcessingComplexifiedSpans;

        private List<(TextSpan oldSpan, TextSpan newSpan)>? _modifiedSubSpans;
        private bool _isProcessingComplexifiedSpans;
        private SemanticModel? _speculativeModel;

        private int _skipRenameForComplexification;

        private readonly Dictionary<TextSpan, TextSpanRenameContext> _textSpanToRenameContexts;
        private readonly Dictionary<SymbolKey, RenameSymbolContext> _renameContexts;
        private readonly Dictionary<TextSpan, HashSet<TextSpanRenameContext>> _stringAndCommentRenameContexts;

        public SymbolsRenameRewriter(RenameRewriterParameters parameters) : base(visitIntoStructuredTrivia: true)
        {
            var document = parameters.Document;
            _documentId = document.Id;
            _solution = parameters.OriginalSolution;
            _conflictLocations = parameters.ConflictLocationSpans;
            _semanticModel = parameters.SemanticModel;
            _cancellationToken = parameters.CancellationToken;
            _renameSpansTracker = parameters.RenameSpansTracker;

            _simplificationService = document.GetRequiredLanguageService<ISimplificationService>();
            _semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
            _syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            _annotatedIdentifierTokens = new();
            _invocationExpressionsNeedingConflictChecks = new();
            _renameAnnotations = parameters.RenameAnnotations;
            _modifiedSubSpans = new();
            _isProcessingComplexifiedSpans = false;
            _skipRenameForComplexification = 0;

            _renameContexts = GroupRenameContextBySymbolKey(parameters.RenameSymbolContexts, SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false));
            _textSpanToRenameContexts = GroupTextRenameContextsByTextSpan(parameters.TokenTextSpanRenameContexts);
            _stringAndCommentRenameContexts = GroupStringAndCommentsTextSpanRenameContexts(parameters.StringAndCommentsTextSpanRenameContexts);
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

        private bool ShouldComplexifyNode(SyntaxNode node, bool isInConflictLambdaBody)
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

        private void AddModifiedSpan(TextSpan oldSpan, TextSpan newSpan)
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

        private async Task<SyntaxToken> RenameAndAnnotateAsync(
            SyntaxToken token,
            SyntaxToken newToken,
            bool isRenameLocation,
            bool isOldText,
            bool isVerbatim,
            bool replacementTextValid,
            bool isRenamableAccessor,
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
                var prefix = isRenameLocation && isRenamableAccessor
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

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            var newTrivia = base.VisitTrivia(trivia);
            // Syntax token in structure trivia would be renamed when the token is visited.
            if (!trivia.HasStructure
                && _stringAndCommentRenameContexts.TryGetValue(trivia.Span, out var textSpanRenameContexts))
            {
                var subSpanToReplacementText = CreateSubSpanToReplacementTextDictionary(textSpanRenameContexts);
                return RenameInCommentTrivia(trivia, subSpanToReplacementText);
            }

            return newTrivia;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            var newToken = base.VisitToken(token);
            newToken = UpdateAliasAnnotation(newToken);
            newToken = RenameTokenInStringOrComment(token, newToken);
            if (!_isProcessingComplexifiedSpans && _textSpanToRenameContexts.TryGetValue(token.Span, out var textSpanRenameContext))
            {
                var symbolContext = textSpanRenameContext.SymbolContext;
                newToken = RenameAndAnnotateAsync(
                    token,
                    newToken,
                    isRenameLocation: true,
                    isOldText: false,
                    isVerbatim: _syntaxFactsService.IsVerbatimIdentifier(symbolContext.ReplacementText),
                    replacementTextValid: symbolContext.ReplacementTextValid,
                    isRenamableAccessor: textSpanRenameContext.RenameLocation.IsRenamableAccessor,
                    replacementText: symbolContext.ReplacementText,
                    originalText: symbolContext.OriginalText,
                    renameRenamableSymbolDeclaration: symbolContext.RenamableSymbolDeclarationAnnotation,
                    renamableDeclarationLocation: symbolContext.RenamableDeclarationLocation).WaitAndGetResult_CanCallOnBackground(_cancellationToken);
                _invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors<InvocationExpressionSyntax>());
                return newToken;
            }

            // If we are renaming a token of complexified node, it could be either
            // 1. It is a referenced token of any of the rename symbols and it should be annotated by RenameActionAnnotation before the complexification.
            // We could rename the token by finding its rename context.
            // 2. Complexification could introduce some new tokens, and they are alse referenced by rename symbols. We need to rename them.
            if (_isProcessingComplexifiedSpans)
            {
                return RenameTokenWhenProcessingComplexifiedSpans(token, newToken);
            }

            return AnnotateNonRenameLocation(token, newToken);
        }

        private SyntaxToken RenameTokenWhenProcessingComplexifiedSpans(SyntaxToken token, SyntaxToken newToken)
        {
            if (!_isProcessingComplexifiedSpans)
            {
                return newToken;
            }

            RoslynDebug.Assert(_speculativeModel != null);

            if (token.HasAnnotations(AliasAnnotation.Kind))
            {
                return newToken;
            }

            if (token.HasAnnotations(RenameAnnotation.Kind))
            {
                var annotation = _renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().First();
                if (annotation.IsRenameLocation && _textSpanToRenameContexts.TryGetValue(annotation.OriginalSpan, out var originalContext))
                {
                    return RenameComplexifiedToken(token, newToken, originalContext);
                }
                else
                {
                    return newToken;
                }
            }

            if (token.Parent is SimpleNameSyntax
                && !token.IsKind(SyntaxKind.GlobalKeyword)
                && token.Parent.Parent.IsKind(SyntaxKind.AliasQualifiedName, SyntaxKind.QualifiedCref, SyntaxKind.QualifiedName))
            {
                var symbol = _speculativeModel.GetSymbolInfo(token.Parent, _cancellationToken).Symbol;
                if (symbol != null
                    && _renameContexts.TryGetValue(symbol.GetSymbolKey(), out var symbolContext)
                    && symbolContext.RenamedSymbol.Kind != SymbolKind.Local
                    && symbolContext.RenamedSymbol.Kind != SymbolKind.RangeVariable
                    && token.ValueText == symbolContext.OriginalText)
                {
                    return RenameComplexifiedToken(token, newToken, symbolContext);
                }
            }

            return newToken;
        }

        private SyntaxToken AnnotateNonRenameLocation(SyntaxToken oldToken, SyntaxToken newToken)
        {
            if (!_isProcessingComplexifiedSpans)
            {
                // Update Alias annotations
                newToken = UpdateAliasAnnotation(newToken);
                var tokenText = oldToken.ValueText;
                var renameContexts = _renameContexts.Values;
                var replacementMatchedContexts = GetMatchedContexts(renameContexts, context => context.ReplacementText == tokenText);
                var originalTextMatchedContexts = GetMatchedContexts(renameContexts, context => context.OriginalText == tokenText);
                var possibleNameConflictsContexts = GetMatchedContexts(renameContexts, context => context.PossibleNameConflicts.Contains(tokenText));
                var possiblyDestructorConflictContexts = GetMatchedContexts(renameContexts, context => IsPossiblyDestructorConflict(oldToken, context.ReplacementText));
                var propertyAccessorNameConflictContexts = GetMatchedContexts(renameContexts, context => IsPropertyAccessorNameConflict(oldToken, context.ReplacementText));

                if (!replacementMatchedContexts.IsEmpty
                    || !originalTextMatchedContexts.IsEmpty
                    || !possibleNameConflictsContexts.IsEmpty
                    || !possiblyDestructorConflictContexts.IsEmpty
                    || !propertyAccessorNameConflictContexts.IsEmpty)
                {
                    newToken = AnnotateForConflictCheckAsync(newToken, !originalTextMatchedContexts.IsEmpty).WaitAndGetResult_CanCallOnBackground(_cancellationToken);
                    _invocationExpressionsNeedingConflictChecks.AddRange(oldToken.GetAncestors<InvocationExpressionSyntax>());
                }

                return newToken;
            }

            return newToken;
        }

        private async Task<SyntaxToken> AnnotateForConflictCheckAsync(SyntaxToken token, bool isOldText)
        {
            var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Workspace.Services, _cancellationToken);
            var renameDeclarationLocations = await ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(false);
            var isMemberGroupReference = _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken);
            var isNamespaceDeclarationReference = token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword);

            var renameAnnotation =
                    new RenameActionAnnotation(
                        token.Span,
                        isRenameLocation: false,
                        prefix: null,
                        suffix: null,
                        renameDeclarationLocations: renameDeclarationLocations,
                        isOriginalTextLocation: isOldText,
                        isNamespaceDeclarationReference: isNamespaceDeclarationReference,
                        isInvocationExpression: false,
                        isMemberGroupReference: isMemberGroupReference);

            var newToken = _renameAnnotations.WithAdditionalAnnotations(token, renameAnnotation, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span });
            _annotatedIdentifierTokens.Add(token);
            return newToken;
        }

        private SyntaxToken RenameComplexifiedToken(SyntaxToken token, SyntaxToken newToken, TextSpanRenameContext textSpanRenameContext)
        {
            if (_isProcessingComplexifiedSpans)
            {
                var annotation = _renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().FirstOrDefault();
                if (annotation != null)
                {
                    newToken = RenameToken(
                        token,
                        newToken,
                        annotation.Prefix,
                        annotation.Suffix,
                        _syntaxFactsService.IsVerbatimIdentifier(textSpanRenameContext.SymbolContext.ReplacementText),
                        textSpanRenameContext.SymbolContext.ReplacementText,
                        textSpanRenameContext.SymbolContext.OriginalText,
                        textSpanRenameContext.SymbolContext.ReplacementTextValid);

                    AddModifiedSpan(annotation.OriginalSpan, newToken.Span);
                }
            }

            return newToken;
        }

        private SyntaxToken RenameComplexifiedToken(SyntaxToken token, SyntaxToken newToken, RenameSymbolContext renameSymbolContext)
        {
            if (_isProcessingComplexifiedSpans)
            {
                return RenameToken(
                    token,
                    newToken,
                    prefix: null,
                    suffix: null,
                    _syntaxFactsService.IsVerbatimIdentifier(renameSymbolContext.ReplacementText),
                    renameSymbolContext.ReplacementText,
                    renameSymbolContext.OriginalText,
                    renameSymbolContext.ReplacementTextValid);
            }

            return newToken;
        }

        private SyntaxToken UpdateAliasAnnotation(SyntaxToken newToken)
        {
            foreach (var (_, renameSymbolContext) in _renameContexts)
            {
                if (renameSymbolContext.AliasSymbol != null && !this.AnnotateForComplexification && newToken.HasAnnotations(AliasAnnotation.Kind))
                {
                    newToken = RenameUtilities.UpdateAliasAnnotation(newToken, renameSymbolContext.AliasSymbol, renameSymbolContext.ReplacementText);
                }
            }

            return newToken;
        }

        private SyntaxToken RenameInStringLiteral(
            SyntaxToken oldToken,
            SyntaxToken newToken,
            ImmutableSortedDictionary<TextSpan, (string replacementText, string matchText)> subSpanToReplacementString,
            Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> createNewStringLiteral)
        {
            var originalString = newToken.ToString();
            var replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(
                originalString,
                subSpanToReplacementString);
            if (replacedString != originalString)
            {
                var oldSpan = oldToken.Span;
                newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia);
                AddModifiedSpan(oldSpan, newToken.Span);
                return newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
            }

            return newToken;
        }

        private SyntaxToken RenameTokenInStringOrComment(SyntaxToken token, SyntaxToken newToken)
        {
            if (_isProcessingComplexifiedSpans
                || !_stringAndCommentRenameContexts.TryGetValue(token.Span, out var textSpanSymbolContexts)
                || textSpanSymbolContexts.Count == 0)
            {
                return newToken;
            }

            var subSpanToReplacementText = CreateSubSpanToReplacementTextDictionary(textSpanSymbolContexts);
            // Rename in string
            if (newToken.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.InterpolatedStringTextToken))
            {
                Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> newStringTokenFactory = newToken.Kind() switch
                {
                    SyntaxKind.StringLiteralToken => SyntaxFactory.Literal,
                    SyntaxKind.InterpolatedStringTextToken => (leadingTrivia, text, value, trailingTrivia) => SyntaxFactory.Token(newToken.LeadingTrivia, SyntaxKind.InterpolatedStringTextToken, text, value, newToken.TrailingTrivia),
                    _ => throw ExceptionUtilities.Unreachable,
                };

                return RenameInStringLiteral(
                    token,
                    newToken,
                    subSpanToReplacementText,
                    newStringTokenFactory);
            }

            // Rename Token in structure comment
            if (newToken.IsKind(SyntaxKind.XmlTextLiteralToken))
            {
                newToken = RenameInStringLiteral(token, newToken, subSpanToReplacementText, SyntaxFactory.XmlTextLiteral);
            }
            else if (newToken.IsKind(SyntaxKind.IdentifierToken) && newToken.Parent.IsKind(SyntaxKind.XmlName))
            {
                var matchingContext = textSpanSymbolContexts.FirstOrDefault(c => c.SymbolContext.OriginalText == newToken.ValueText);
                if (matchingContext != null)
                {
                    var newIdentifierToken = SyntaxFactory.Identifier(newToken.LeadingTrivia, matchingContext.SymbolContext.ReplacementText, newToken.TrailingTrivia);
                    newToken = newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span }));
                    AddModifiedSpan(token.Span, newToken.Span);
                }
            }

            return newToken;
        }

        private static SyntaxToken RenameToken(
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

        private static bool IsPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => IsGetPropertyAccessorNameConflict(token, replacementText)
            || IsSetPropertyAccessorNameConflict(token, replacementText)
            || IsInitPropertyAccessorNameConflict(token, replacementText);

        private static bool IsGetPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.GetKeyword)
            && IsNameConflictWithProperty("get", token.Parent as AccessorDeclarationSyntax, replacementText);

        private static bool IsSetPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.SetKeyword)
            && IsNameConflictWithProperty("set", token.Parent as AccessorDeclarationSyntax, replacementText);

        private static bool IsInitPropertyAccessorNameConflict(SyntaxToken token, string replacementText)
            => token.IsKind(SyntaxKind.InitKeyword)
            // using "set" here is intentional. The compiler generates set_PropName for both set and init accessors.
            && IsNameConflictWithProperty("set", token.Parent as AccessorDeclarationSyntax, replacementText);

        private static bool IsNameConflictWithProperty(string prefix, AccessorDeclarationSyntax? accessor, string replacementText)
            => accessor?.Parent?.Parent is PropertyDeclarationSyntax property   // 3 null checks in one: accessor -> accessor list -> property declaration
            && replacementText.Equals(prefix + "_" + property.Identifier.Text, StringComparison.Ordinal);

        private static bool IsPossiblyDestructorConflict(SyntaxToken token, string replacementText)
        {
            return replacementText == "Finalize" &&
                token.IsKind(SyntaxKind.IdentifierToken) &&
                token.Parent.IsKind(SyntaxKind.DestructorDeclaration);
        }

        private SyntaxTrivia RenameInCommentTrivia(
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
