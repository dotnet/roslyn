// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    [ExportLanguageService(typeof(IRenameRewriterLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpRenameConflictLanguageService : AbstractRenameRewriterLanguageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRenameConflictLanguageService()
        {
        }

        #region "Annotation"

        public override SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters)
        {
            var renameAnnotationRewriter = new RenameRewriter(parameters);
            return renameAnnotationRewriter.Visit(parameters.SyntaxRoot)!;
        }

        private sealed class RenameRewriter : CSharpSyntaxRewriter
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

            /// <summary>
            /// Mapping from the span of renaming token to the renaming context info.
            /// </summary>
            private readonly ImmutableDictionary<TextSpan, LocationRenameContext> _textSpanToLocationContextMap;

            /// <summary>
            /// Mapping from the symbolKey to all the possible symbols might be renamed in the document.
            /// </summary>
            private readonly ImmutableDictionary<SymbolKey, RenamedSymbolContext> _renamedSymbolContexts;

            /// <summary>
            /// Mapping from the containgSpan of a common trivia/string identifier to a set of Locations needs to rename inside it.
            /// It is created by using a regex in to find the matched text when renaming inside a string/identifier.
            /// </summary>
            private readonly ImmutableDictionary<TextSpan, ImmutableSortedDictionary<TextSpan, string>> _stringAndCommentRenameContexts;

            private readonly ImmutableHashSet<string> _replacementTexts;
            private readonly ImmutableHashSet<string> _originalTexts;
            private readonly ImmutableHashSet<string> _allPossibleConflictNames;

            private List<(TextSpan oldSpan, TextSpan newSpan)>? _modifiedSubSpans;
            private bool _isProcessingComplexifiedSpans;
            private SemanticModel? _speculativeModel;
            private int _skipRenameForComplexification;
            private bool AnnotateForComplexification => _skipRenameForComplexification > 0 && !_isProcessingComplexifiedSpans;

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

            public RenameRewriter(RenameRewriterParameters parameters) : base(visitIntoStructuredTrivia: true)
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

                _textSpanToLocationContextMap = parameters.DocumentRenameInfo.TextSpanToLocationContexts;
                _renamedSymbolContexts = parameters.DocumentRenameInfo.RenamedSymbolContexts;
                _stringAndCommentRenameContexts = parameters.DocumentRenameInfo.TextSpanToStringAndCommentRenameContexts;
                _replacementTexts = parameters.DocumentRenameInfo.AllReplacementTexts;
                _originalTexts = parameters.DocumentRenameInfo.AllOriginalText;
                _allPossibleConflictNames = parameters.DocumentRenameInfo.AllPossibleConflictNames;
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

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var newToken = base.VisitToken(token);
                // Handle Alias annotations
                newToken = UpdateAliasAnnotation(newToken);
                // Rename matches in strings and comments
                newToken = RenameWithinToken(token, newToken);

                // We don't want to annotate XmlName with RenameActionAnnotation
                if (newToken.Parent.IsKind(SyntaxKind.XmlName))
                {
                    return newToken;
                }

                if (!_isProcessingComplexifiedSpans && _textSpanToLocationContextMap.TryGetValue(token.Span, out var locationRenameContext))
                {
                    newToken = RenameAndAnnotateAsync(
                        token,
                        newToken,
                        isVerbatim: _syntaxFactsService.IsVerbatimIdentifier(locationRenameContext.ReplacementText),
                        replacementTextValid: locationRenameContext.ReplacementTextValid,
                        isRenamableAccessor: locationRenameContext.RenameLocation.IsRenamableAccessor,
                        replacementText: locationRenameContext.ReplacementText,
                        originalText: locationRenameContext.OriginalText).WaitAndGetResult_CanCallOnBackground(_cancellationToken);
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

            private SyntaxNode Complexify(SyntaxNode originalNode, SyntaxNode newNode)
            {
                _isProcessingComplexifiedSpans = true;
                _modifiedSubSpans = new List<(TextSpan oldSpan, TextSpan newSpan)>();

                var annotation = new SyntaxAnnotation();
                newNode = newNode.WithAdditionalAnnotations(annotation);
                var speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode);
                newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

                _speculativeModel = GetSemanticModelForNode(newNode, _semanticModel);
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

                _speculativeModel = GetSemanticModelForNode(newNode, _semanticModel);

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

            private async Task<SyntaxToken> RenameAndAnnotateAsync(
                SyntaxToken token,
                SyntaxToken newToken,
                bool isVerbatim,
                bool replacementTextValid,
                bool isRenamableAccessor,
                string replacementText,
                string originalText)
            {
                try
                {
                    if (_isProcessingComplexifiedSpans)
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

                        return newToken;
                    }

                    var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Services, _cancellationToken);

                    string? suffix = null;
                    var prefix = isRenamableAccessor
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
                    }

                    // Rename Token
                    if (!this.AnnotateForComplexification)
                    {
                        var oldSpan = token.Span;
                        newToken = RenameToken(token, newToken, prefix, suffix, isVerbatim, replacementText, originalText, replacementTextValid);

                        AddModifiedSpan(oldSpan, newToken.Span);
                    }

                    var renameDeclarationLocations = await
                        ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(false);

                    var isNamespaceDeclarationReference = token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword);
                    var isMemberGroupReference = _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken);

                    var renameAnnotation =
                            new RenameActionAnnotation(
                                token.Span,
                                isRenameLocation: true,
                                prefix,
                                suffix,
                                renameDeclarationLocations: renameDeclarationLocations,
                                isOriginalTextLocation: token.ValueText == originalText,
                                isNamespaceDeclarationReference: isNamespaceDeclarationReference,
                                isInvocationExpression: false,
                                isMemberGroupReference: isMemberGroupReference);

                    newToken = _renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span });

                    _annotatedIdentifierTokens.Add(token);
                    return newToken;
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
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

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                var newTrivia = base.VisitTrivia(trivia);
                // Syntax token in structure trivia would be renamed when the token is visited.
                if (!trivia.HasStructure && _stringAndCommentRenameContexts.TryGetValue(trivia.Span, out var subSpanToReplacementText))
                {
                    return RenameInCommentTrivia(trivia, newTrivia, subSpanToReplacementText);
                }

                return newTrivia;
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
                    if (annotation.IsRenameLocation && _textSpanToLocationContextMap.TryGetValue(annotation.OriginalSpan, out var originalContext))
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
                    && token.Parent.Parent is (kind: SyntaxKind.AliasQualifiedName or SyntaxKind.QualifiedCref or SyntaxKind.QualifiedName))
                {
                    var symbol = _speculativeModel.GetSymbolInfo(token.Parent, _cancellationToken).Symbol;
                    if (symbol != null
                        && _renamedSymbolContexts.TryGetValue(symbol.GetSymbolKey(), out var symbolContext)
                        && symbolContext.RenamedSymbol.Kind != SymbolKind.Local
                        && symbolContext.RenamedSymbol.Kind != SymbolKind.RangeVariable
                        && token.ValueText == symbolContext.OriginalText)
                    {
                        return RenameComplexifiedToken(token, newToken, symbolContext);
                    }
                }

                return newToken;
            }

            private SyntaxToken AnnotateNonRenameLocation(SyntaxToken token, SyntaxToken newToken)
            {
                if (!_isProcessingComplexifiedSpans)
                {
                    // Annotate the token if it would cause conflict in all other scenarios
                    var tokenText = token.ValueText;

                    // This is a pretty hot code path, avoid using linq.
                    var isOldText = _originalTexts.Contains(tokenText);
                    var tokenNeedsConflictCheck = isOldText
                        || _replacementTexts.Contains(tokenText)
                        || _allPossibleConflictNames.Contains(tokenText)
                        || IsPossibleDestructorOrPropertyAccessorName(token);

                    if (tokenNeedsConflictCheck)
                    {
                        newToken = AnnotateForConflictCheckAsync(token, newToken, isOldText).WaitAndGetResult_CanCallOnBackground(_cancellationToken);
                    }

                    return newToken;
                }

                return newToken;
            }

            private bool IsPossibleDestructorOrPropertyAccessorName(SyntaxToken token)
            {
                foreach (var replacementText in _replacementTexts)
                {
                    return IsPossiblyDestructorConflict(token, replacementText)
                           || IsPropertyAccessorNameConflict(token, replacementText);
                }

                return false;
            }

            private async Task<SyntaxToken> AnnotateForConflictCheckAsync(SyntaxToken token, SyntaxToken newToken, bool isOldText)
            {
                var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Services, _cancellationToken);
                var isNamespaceDeclarationReference = token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword);
                if (symbols.Length == 1)
                {
                    // This is a conflicting namespace declaration token. Even if the rename results in conflict with this namespace
                    // conflict is not shown for the namespace so we are tracking this token
                    if (symbols[0] is INamespaceSymbol && isNamespaceDeclarationReference)
                    {
                        return newToken;
                    }
                }

                var renameDeclarationLocations = await ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(false);
                var isMemberGroupReference = _semanticFactsService.IsInsideNameOfExpression(_semanticModel, token.Parent, _cancellationToken);

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

                newToken = _renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span });
                _annotatedIdentifierTokens.Add(token);
                _invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors<InvocationExpressionSyntax>());
                return newToken;
            }

            private SyntaxToken RenameComplexifiedToken(SyntaxToken token, SyntaxToken newToken, LocationRenameContext locationRenameContext)
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
                            _syntaxFactsService.IsVerbatimIdentifier(locationRenameContext.ReplacementText),
                            locationRenameContext.ReplacementText,
                            locationRenameContext.OriginalText,
                            locationRenameContext.ReplacementTextValid);

                        AddModifiedSpan(annotation.OriginalSpan, newToken.Span);
                    }
                }

                return newToken;
            }

            private SyntaxToken RenameComplexifiedToken(SyntaxToken token, SyntaxToken newToken, RenamedSymbolContext renamedSymbolContext)
            {
                if (_isProcessingComplexifiedSpans)
                {
                    return RenameToken(
                        token,
                        newToken,
                        prefix: null,
                        suffix: null,
                        _syntaxFactsService.IsVerbatimIdentifier(renamedSymbolContext.ReplacementText),
                        renamedSymbolContext.ReplacementText,
                        renamedSymbolContext.OriginalText,
                        renamedSymbolContext.ReplacementTextValid);
                }

                return newToken;
            }

            private SyntaxToken UpdateAliasAnnotation(SyntaxToken newToken)
            {
                foreach (var (_, renameSymbolContext) in _renamedSymbolContexts)
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
                ImmutableSortedDictionary<TextSpan, string> subSpanToReplacementText,
                Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> createNewStringLiteral)
            {
                var originalString = newToken.ToString();
                var replacedString = RenameUtilities.ReplaceMatchingSubStrings(
                    originalString,
                    subSpanToReplacementText);
                if (replacedString != originalString)
                {
                    var oldSpan = oldToken.Span;
                    var replacedToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia);
                    AddModifiedSpan(oldSpan, replacedToken.Span);
                    return newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(replacedToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
                }

                return newToken;
            }

            private SyntaxToken RenameWithinToken(SyntaxToken token, SyntaxToken newToken)
            {
                if (_isProcessingComplexifiedSpans
                    || !_stringAndCommentRenameContexts.TryGetValue(token.Span, out var subSpanToReplacementText)
                    || subSpanToReplacementText.Count == 0)
                {
                    return newToken;
                }

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
                    return RenameInStringLiteral(token, newToken, subSpanToReplacementText, SyntaxFactory.XmlTextLiteral);
                }
                else if (newToken.IsKind(SyntaxKind.IdentifierToken) && newToken.Parent.IsKind(SyntaxKind.XmlName))
                {
                    // Rename the xml tag in structure comment
                    var originalText = newToken.ToString();
                    var replacementText = RenameUtilities.ReplaceMatchingSubStrings(
                        originalText,
                        subSpanToReplacementText);
                    if (replacementText != originalText)
                    {
                        var newIdentifierToken = SyntaxFactory.Identifier(
                            newToken.LeadingTrivia,
                            replacementText,
                            newToken.TrailingTrivia);
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
                    if (parsedIdentifier is IdentifierNameSyntax identifierName)
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

            private SyntaxTrivia RenameInCommentTrivia(
                SyntaxTrivia oldTrivia,
                SyntaxTrivia newTrivia,
                ImmutableSortedDictionary<TextSpan, string> subSpanToReplacementText)
            {
                var originalString = newTrivia.ToString();
                var replacedString = RenameUtilities.ReplaceMatchingSubStrings(
                    originalString,
                    subSpanToReplacementText);

                if (replacedString != originalString)
                {
                    var oldSpan = oldTrivia.Span;
                    var replacedTrivia = SyntaxFactory.Comment(replacedString);
                    AddModifiedSpan(oldSpan, replacedTrivia.Span);
                    return newTrivia.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(replacedTrivia, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
                }

                return newTrivia;
            }
        }
        #endregion

        #region "Declaration Conflicts"

        public override bool LocalVariableConflict(
            SyntaxToken token,
            IEnumerable<ISymbol> newReferencedSymbols)
        {
            if (token.Parent is ExpressionSyntax(SyntaxKind.IdentifierName) expression &&
                token.Parent.IsParentKind(SyntaxKind.InvocationExpression) &&
                token.GetPreviousToken().Kind() != SyntaxKind.DotToken &&
                token.GetNextToken().Kind() != SyntaxKind.DotToken)
            {
                var enclosingMemberDeclaration = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                if (enclosingMemberDeclaration != null)
                {
                    var locals = enclosingMemberDeclaration.GetLocalDeclarationMap()[token.ValueText];
                    if (locals.Length > 0)
                    {
                        // This unqualified invocation name matches the name of an existing local
                        // or parameter. Report a conflict if the matching local/parameter is not
                        // a delegate type.

                        var relevantLocals = newReferencedSymbols
                            .Where(s => s.MatchesKind(SymbolKind.Local, SymbolKind.Parameter) && s.Name == token.ValueText);

                        if (relevantLocals.Count() != 1)
                        {
                            return true;
                        }

                        var matchingLocal = relevantLocals.Single();
                        var invocationTargetsLocalOfDelegateType =
                            (matchingLocal.IsKind(SymbolKind.Local) && ((ILocalSymbol)matchingLocal).Type.IsDelegateType()) ||
                            (matchingLocal.IsKind(SymbolKind.Parameter) && ((IParameterSymbol)matchingLocal).Type.IsDelegateType());

                        return !invocationTargetsLocalOfDelegateType;
                    }
                }
            }

            return false;
        }

        public override async Task<ImmutableArray<Location>> ComputeDeclarationConflictsAsync(
            string replacementText,
            ISymbol renamedSymbol,
            ISymbol renameSymbol,
            IEnumerable<ISymbol> referencedSymbols,
            Solution baseSolution,
            Solution newSolution,
            IDictionary<Location, Location> reverseMappedLocations,
            CancellationToken cancellationToken)
        {
            try
            {
                using var _ = ArrayBuilder<Location>.GetInstance(out var conflicts);

                // If we're renaming a named type, we can conflict with members w/ our same name.  Note:
                // this doesn't apply to enums.
                if (renamedSymbol is INamedTypeSymbol { TypeKind: not TypeKind.Enum } namedType)
                    AddSymbolSourceSpans(conflicts, namedType.GetMembers(renamedSymbol.Name), reverseMappedLocations);

                // If we're contained in a named type (we may be a named type ourself!) then we have a
                // conflict.  NOTE(cyrusn): This does not apply to enums. 
                if (renamedSymbol.ContainingSymbol is INamedTypeSymbol { TypeKind: not TypeKind.Enum } containingNamedType &&
                    containingNamedType.Name == renamedSymbol.Name)
                {
                    AddSymbolSourceSpans(conflicts, SpecializedCollections.SingletonEnumerable(containingNamedType), reverseMappedLocations);
                }

                if (renamedSymbol.Kind is SymbolKind.Parameter or
                    SymbolKind.Local or
                    SymbolKind.RangeVariable)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);
                    var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LocalConflictVisitor(token);

                    visitor.Visit(memberDeclaration);
                    conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));

                    // If this is a parameter symbol for a partial method definition, be sure we visited 
                    // the implementation part's body.
                    if (renamedSymbol is IParameterSymbol renamedParameterSymbol &&
                        renamedSymbol.ContainingSymbol is IMethodSymbol methodSymbol &&
                        methodSymbol.PartialImplementationPart != null)
                    {
                        var matchingParameterSymbol = methodSymbol.PartialImplementationPart.Parameters[renamedParameterSymbol.Ordinal];

                        token = matchingParameterSymbol.Locations.Single().FindToken(cancellationToken);
                        memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                        visitor = new LocalConflictVisitor(token);
                        visitor.Visit(memberDeclaration);
                        conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));
                    }
                }
                else if (renamedSymbol.Kind == SymbolKind.Label)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);
                    var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LabelConflictVisitor(token);

                    visitor.Visit(memberDeclaration);
                    conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));
                }
                else if (renamedSymbol.Kind == SymbolKind.Method)
                {
                    conflicts.AddRange(DeclarationConflictHelpers.GetMembersWithConflictingSignatures((IMethodSymbol)renamedSymbol, trimOptionalParameters: false).Select(t => reverseMappedLocations[t]));

                    // we allow renaming overrides of VB property accessors with parameters in C#.
                    // VB has a special rule that properties are not allowed to have the same name as any of the parameters. 
                    // Because this declaration in C# affects the property declaration in VB, we need to check this VB rule here in C#.
                    var properties = new List<ISymbol>();
                    foreach (var referencedSymbol in referencedSymbols)
                    {
                        var property = await RenameUtilities.TryGetPropertyFromAccessorOrAnOverrideAsync(
                            referencedSymbol, baseSolution, cancellationToken).ConfigureAwait(false);
                        if (property != null)
                            properties.Add(property);
                    }

                    AddConflictingParametersOfProperties(properties.Distinct(), replacementText, conflicts);
                }
                else if (renamedSymbol.Kind == SymbolKind.Alias)
                {
                    // in C# there can only be one using with the same alias name in the same block (top of file of namespace). 
                    // It's ok to redefine the alias in different blocks.
                    var location = renamedSymbol.Locations.Single();
                    var tree = location.SourceTree;
                    Contract.ThrowIfNull(tree);

                    var token = await tree.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                    var currentUsing = (UsingDirectiveSyntax)token.Parent!.Parent!.Parent!;

                    var namespaceDecl = token.Parent.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
                    SyntaxList<UsingDirectiveSyntax> usings;
                    if (namespaceDecl != null)
                    {
                        usings = namespaceDecl.Usings;
                    }
                    else
                    {
                        var compilationUnit = (CompilationUnitSyntax)await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                        usings = compilationUnit.Usings;
                    }

                    foreach (var usingDirective in usings)
                    {
                        if (usingDirective.Alias != null && usingDirective != currentUsing)
                        {
                            if (usingDirective.Alias.Name.Identifier.ValueText == currentUsing.Alias!.Name.Identifier.ValueText)
                                conflicts.Add(reverseMappedLocations[usingDirective.Alias.Name.GetLocation()]);
                        }
                    }
                }
                else if (renamedSymbol.Kind == SymbolKind.TypeParameter)
                {
                    foreach (var location in renamedSymbol.Locations)
                    {
                        var token = await location.SourceTree!.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                        var currentTypeParameter = token.Parent!;

                        foreach (var typeParameter in ((TypeParameterListSyntax)currentTypeParameter.Parent!).Parameters)
                        {
                            if (typeParameter != currentTypeParameter && token.ValueText == typeParameter.Identifier.ValueText)
                                conflicts.Add(reverseMappedLocations[typeParameter.Identifier.GetLocation()]);
                        }
                    }
                }

                // if the renamed symbol is a type member, it's name should not conflict with a type parameter
                if (renamedSymbol.ContainingType != null && renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol))
                {
                    var conflictingLocations = renamedSymbol.ContainingType.TypeParameters
                        .Where(t => t.Name == renamedSymbol.Name)
                        .SelectMany(t => t.Locations);

                    foreach (var location in conflictingLocations)
                    {
                        var typeParameterToken = location.FindToken(cancellationToken);
                        conflicts.Add(reverseMappedLocations[typeParameterToken.GetLocation()]);
                    }
                }

                return conflicts.ToImmutable();
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<ISymbol?> GetVBPropertyFromAccessorOrAnOverrideAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            try
            {
                if (symbol.IsPropertyAccessor())
                {
                    var property = ((IMethodSymbol)symbol).AssociatedSymbol!;

                    return property.Language == LanguageNames.VisualBasic ? property : null;
                }

                if (symbol.IsOverride && symbol.GetOverriddenMember() != null)
                {
                    var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol.GetOverriddenMember(), solution, cancellationToken).ConfigureAwait(false);
                    if (originalSourceSymbol != null)
                    {
                        return await GetVBPropertyFromAccessorOrAnOverrideAsync(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
                    }
                }

                return null;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static void AddSymbolSourceSpans(
            ArrayBuilder<Location> conflicts, IEnumerable<ISymbol> symbols,
            IDictionary<Location, Location> reverseMappedLocations)
        {
            foreach (var symbol in symbols)
            {
                foreach (var location in symbol.Locations)
                {
                    // reverseMappedLocations may not contain the location if the location's token
                    // does not contain the text of it's name (e.g. the getter of "int X { get; }"
                    // does not contain the text "get_X" so conflicting renames to "get_X" will not
                    // have added the getter to reverseMappedLocations).
                    if (location.IsInSource && reverseMappedLocations.ContainsKey(location))
                    {
                        conflicts.Add(reverseMappedLocations[location]);
                    }
                }
            }
        }

        public override async Task<ImmutableArray<Location>> ComputeImplicitReferenceConflictsAsync(
            ISymbol renameSymbol, ISymbol renamedSymbol, IEnumerable<ReferenceLocation> implicitReferenceLocations, CancellationToken cancellationToken)
        {
            // Handle renaming of symbols used for foreach
            var implicitReferencesMightConflict = renameSymbol.Kind == SymbolKind.Property &&
                                                string.Compare(renameSymbol.Name, "Current", StringComparison.OrdinalIgnoreCase) == 0;

            implicitReferencesMightConflict =
                implicitReferencesMightConflict ||
                    (renameSymbol.Kind == SymbolKind.Method &&
                        (string.Compare(renameSymbol.Name, WellKnownMemberNames.MoveNextMethodName, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.GetEnumeratorMethodName, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.GetAwaiter, StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(renameSymbol.Name, WellKnownMemberNames.DeconstructMethodName, StringComparison.OrdinalIgnoreCase) == 0));

            // TODO: handle Dispose for using statement and Add methods for collection initializers.

            if (implicitReferencesMightConflict)
            {
                if (renamedSymbol.Name != renameSymbol.Name)
                {
                    foreach (var implicitReferenceLocation in implicitReferenceLocations)
                    {
                        var token = await implicitReferenceLocation.Location.SourceTree!.GetTouchingTokenAsync(
                            implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, findInsideTrivia: false).ConfigureAwait(false);

                        switch (token.Kind())
                        {
                            case SyntaxKind.ForEachKeyword:
                                return ImmutableArray.Create(((CommonForEachStatementSyntax)token.Parent!).Expression.GetLocation());
                            case SyntaxKind.AwaitKeyword:
                                return ImmutableArray.Create(token.GetLocation());
                        }

                        if (token.Parent.IsInDeconstructionLeft(out var deconstructionLeft))
                        {
                            return ImmutableArray.Create(deconstructionLeft.GetLocation());
                        }
                    }
                }
            }

            return ImmutableArray<Location>.Empty;
        }

        public override ImmutableArray<Location> ComputePossibleImplicitUsageConflicts(
            ISymbol renamedSymbol,
            SemanticModel semanticModel,
            Location originalDeclarationLocation,
            int newDeclarationLocationStartingPosition,
            CancellationToken cancellationToken)
        {
            // TODO: support other implicitly used methods like dispose

            if ((renamedSymbol.Name == "MoveNext" || renamedSymbol.Name == "GetEnumerator" || renamedSymbol.Name == "Current") && renamedSymbol.GetAllTypeArguments().Length == 0)
            {
                // TODO: partial methods currently only show the location where the rename happens as a conflict.
                //       Consider showing both locations as a conflict.
                var baseType = renamedSymbol.ContainingType?.GetBaseTypes().FirstOrDefault();
                if (baseType != null)
                {
                    var implicitSymbols = semanticModel.LookupSymbols(
                        newDeclarationLocationStartingPosition,
                        baseType,
                        renamedSymbol.Name)
                            .Where(sym => !sym.Equals(renamedSymbol));

                    foreach (var symbol in implicitSymbols)
                    {
                        if (symbol.GetAllTypeArguments().Length != 0)
                        {
                            continue;
                        }

                        if (symbol.Kind == SymbolKind.Method)
                        {
                            var method = (IMethodSymbol)symbol;

                            if (symbol.Name == "MoveNext")
                            {
                                if (!method.ReturnsVoid && !method.Parameters.Any() && method.ReturnType.SpecialType == SpecialType.System_Boolean)
                                {
                                    return ImmutableArray.Create(originalDeclarationLocation);
                                }
                            }
                            else if (symbol.Name == "GetEnumerator")
                            {
                                // we are a bit pessimistic here. 
                                // To be sure we would need to check if the returned type is having a MoveNext and Current as required by foreach
                                if (!method.ReturnsVoid &&
                                    !method.Parameters.Any())
                                {
                                    return ImmutableArray.Create(originalDeclarationLocation);
                                }
                            }
                        }
                        else if (symbol.Kind == SymbolKind.Property && symbol.Name == "Current")
                        {
                            var property = (IPropertySymbol)symbol;

                            if (!property.Parameters.Any() && !property.IsWriteOnly)
                            {
                                return ImmutableArray.Create(originalDeclarationLocation);
                            }
                        }
                    }
                }
            }

            return ImmutableArray<Location>.Empty;
        }

        #endregion

        public override void TryAddPossibleNameConflicts(ISymbol symbol, string replacementText, ICollection<string> possibleNameConflicts)
        {
            if (replacementText.EndsWith("Attribute", StringComparison.Ordinal) && replacementText.Length > 9)
            {
                var conflict = replacementText.Substring(0, replacementText.Length - 9);
                if (!possibleNameConflicts.Contains(conflict))
                {
                    possibleNameConflicts.Add(conflict);
                }
            }

            if (symbol.Kind == SymbolKind.Property)
            {
                foreach (var conflict in new string[] { "_" + replacementText, "get_" + replacementText, "set_" + replacementText })
                {
                    if (!possibleNameConflicts.Contains(conflict))
                    {
                        possibleNameConflicts.Add(conflict);
                    }
                }
            }

            // in C# we also need to add the valueText because it can be different from the text in source
            // e.g. it can contain escaped unicode characters. Otherwise conflicts would be detected for
            // v\u0061r and var or similar.
            var valueText = replacementText;
            var kind = SyntaxFacts.GetKeywordKind(replacementText);
            if (kind != SyntaxKind.None)
            {
                valueText = SyntaxFacts.GetText(kind);
            }
            else
            {
                var name = SyntaxFactory.ParseName(replacementText);
                if (name.Kind() == SyntaxKind.IdentifierName)
                {
                    valueText = ((IdentifierNameSyntax)name).Identifier.ValueText;
                }
            }

            // this also covers the case of an escaped replacementText
            if (valueText != replacementText)
            {
                possibleNameConflicts.Add(valueText);
            }
        }

        /// <summary>
        /// Gets the top most enclosing statement or CrefSyntax as target to call MakeExplicit on.
        /// It's either the enclosing statement, or if this statement is inside of a lambda expression, the enclosing
        /// statement of this lambda.
        /// </summary>
        /// <param name="token">The token to get the complexification target for.</param>
        /// <returns></returns>
        public override SyntaxNode? GetExpansionTargetForLocation(SyntaxToken token)
            => GetExpansionTarget(token);

        private static SyntaxNode? GetExpansionTarget(SyntaxToken token)
        {
            // get the directly enclosing statement
            var enclosingStatement = token.GetAncestors(n => n is StatementSyntax).FirstOrDefault();

            // System.Func<int, int> myFunc = arg => X;
            var possibleLambdaExpression = enclosingStatement == null
                ? token.GetAncestors(n => n is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax).FirstOrDefault()
                : null;
            if (possibleLambdaExpression != null)
            {
                var lambdaExpression = ((LambdaExpressionSyntax)possibleLambdaExpression);
                if (lambdaExpression.Body is ExpressionSyntax)
                {
                    return lambdaExpression.Body;
                }
            }

            // int M() => X;
            var possibleArrowExpressionClause = enclosingStatement == null
                ? token.GetAncestors<ArrowExpressionClauseSyntax>().FirstOrDefault()
                : null;
            if (possibleArrowExpressionClause != null)
            {
                return possibleArrowExpressionClause.Expression;
            }

            var enclosingNameMemberCrefOrnull = token.GetAncestors(n => n is NameMemberCrefSyntax).LastOrDefault();
            if (enclosingNameMemberCrefOrnull != null)
            {
                if (token.Parent is TypeSyntax && token.Parent.Parent is TypeSyntax)
                {
                    enclosingNameMemberCrefOrnull = null;
                }
            }

            var enclosingXmlNameAttr = token.GetAncestors(n => n is XmlNameAttributeSyntax).FirstOrDefault();
            if (enclosingXmlNameAttr != null)
            {
                return null;
            }

            var enclosingInitializer = token.GetAncestors<EqualsValueClauseSyntax>().FirstOrDefault();
            if (enclosingStatement == null && enclosingInitializer != null && enclosingInitializer.Parent is VariableDeclaratorSyntax)
            {
                return enclosingInitializer.Value;
            }

            var attributeSyntax = token.GetAncestor<AttributeSyntax>();
            if (attributeSyntax != null)
            {
                return attributeSyntax;
            }

            // there seems to be no statement above this one. Let's see if we can at least get an SimpleNameSyntax
            return enclosingStatement ?? enclosingNameMemberCrefOrnull ?? token.GetAncestors(n => n is SimpleNameSyntax).FirstOrDefault();
        }

        #region "Helper Methods"

        public override bool IsIdentifierValid(string replacementText, ISyntaxFactsService syntaxFactsService)
        {
            // Identifiers we never consider valid to rename to.
            switch (replacementText)
            {
                case "var":
                case "dynamic":
                case "unmanaged":
                case "notnull":
                    return false;
            }

            var escapedIdentifier = replacementText.StartsWith("@", StringComparison.Ordinal)
                ? replacementText : "@" + replacementText;

            // Make sure we got an identifier. 
            if (!syntaxFactsService.IsValidIdentifier(escapedIdentifier))
            {
                // We still don't have an identifier, so let's fail
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the semantic model for the given node.
        /// If the node belongs to the syntax tree of the original semantic model, then returns originalSemanticModel.
        /// Otherwise, returns a speculative model.
        /// The assumption for the later case is that span start position of the given node in it's syntax tree is same as
        /// the span start of the original node in the original syntax tree.
        /// </summary>
        public static SemanticModel? GetSemanticModelForNode(SyntaxNode node, SemanticModel originalSemanticModel)
        {
            if (node.SyntaxTree == originalSemanticModel.SyntaxTree)
            {
                // This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                return originalSemanticModel;
            }

            var nodeToSpeculate = node.GetAncestorsOrThis(n => SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault();
            if (nodeToSpeculate == null)
            {
                if (node is NameMemberCrefSyntax nameMember)
                {
                    nodeToSpeculate = nameMember.Name;
                }
                else if (node is QualifiedCrefSyntax qualifiedCref)
                {
                    nodeToSpeculate = qualifiedCref.Container;
                }
                else if (node is TypeConstraintSyntax typeConstraint)
                {
                    nodeToSpeculate = typeConstraint.Type;
                }
                else if (node is BaseTypeSyntax baseType)
                {
                    nodeToSpeculate = baseType.Type;
                }
                else
                {
                    return null;
                }
            }

            var isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);
            var position = nodeToSpeculate.SpanStart;
            return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, originalSemanticModel, position, isInNamespaceOrTypeContext);
        }

        public override bool IsRenamableTokenInComment(SyntaxToken token)
            => token.IsKind(SyntaxKind.XmlTextLiteralToken) || token.IsKind(SyntaxKind.IdentifierToken) && token.Parent.IsKind(SyntaxKind.XmlName);

        #endregion
    }
}
