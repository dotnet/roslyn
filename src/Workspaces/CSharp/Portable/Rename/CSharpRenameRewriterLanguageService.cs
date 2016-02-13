// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    [ExportLanguageService(typeof(IRenameRewriterLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpRenameConflictLanguageService : IRenameRewriterLanguageService
    {
        #region "Annotation"

        public SyntaxNode AnnotateAndRename(RenameRewriterParameters parameters)
        {
            var renameAnnotationRewriter = new RenameRewriter(parameters);
            return renameAnnotationRewriter.Visit(parameters.SyntaxRoot);
        }

        private class RenameRewriter : CSharpSyntaxRewriter
        {
            private readonly DocumentId _documentId;
            private readonly RenameAnnotation _renameRenamableSymbolDeclaration;
            private readonly Solution _solution;
            private readonly string _replacementText;
            private readonly string _originalText;
            private readonly ICollection<string> _possibleNameConflicts;
            private readonly Dictionary<TextSpan, RenameLocation> _renameLocations;
            private readonly ISet<TextSpan> _conflictLocations;
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;

            private readonly ISymbol _renamedSymbol;
            private readonly IAliasSymbol _aliasSymbol;
            private readonly Location _renamableDeclarationLocation;

            private readonly RenamedSpansTracker _renameSpansTracker;
            private readonly bool _isVerbatim;
            private readonly bool _replacementTextValid;
            private readonly bool _isRenamingInStrings;
            private readonly bool _isRenamingInComments;
            private readonly ISet<TextSpan> _stringAndCommentTextSpans;
            private readonly ISimplificationService _simplificationService;
            private readonly ISemanticFactsService _semanticFactsService;
            private readonly HashSet<SyntaxToken> _annotatedIdentifierTokens = new HashSet<SyntaxToken>();
            private readonly HashSet<InvocationExpressionSyntax> _invocationExpressionsNeedingConflictChecks = new HashSet<InvocationExpressionSyntax>();

            private readonly AnnotationTable<RenameAnnotation> _renameAnnotations;

            public bool AnnotateForComplexification
            {
                get
                {
                    return _skipRenameForComplexification > 0 && !_isProcessingComplexifiedSpans;
                }
            }

            private int _skipRenameForComplexification;
            private bool _isProcessingComplexifiedSpans;
            private List<ValueTuple<TextSpan, TextSpan>> _modifiedSubSpans;
            private SemanticModel _speculativeModel;
            private int _isProcessingTrivia;

            private void AddModifiedSpan(TextSpan oldSpan, TextSpan newSpan)
            {
                newSpan = new TextSpan(oldSpan.Start, newSpan.Length);

                if (!_isProcessingComplexifiedSpans)
                {
                    _renameSpansTracker.AddModifiedSpan(_documentId, oldSpan, newSpan);
                }
                else
                {
                    _modifiedSubSpans.Add(ValueTuple.Create(oldSpan, newSpan));
                }
            }

            public RenameRewriter(RenameRewriterParameters parameters)
                : base(visitIntoStructuredTrivia: true)
            {
                _documentId = parameters.Document.Id;
                _renameRenamableSymbolDeclaration = parameters.RenamedSymbolDeclarationAnnotation;
                _solution = parameters.OriginalSolution;
                _replacementText = parameters.ReplacementText;
                _originalText = parameters.OriginalText;
                _possibleNameConflicts = parameters.PossibleNameConflicts;
                _renameLocations = parameters.RenameLocations;
                _conflictLocations = parameters.ConflictLocationSpans;
                _cancellationToken = parameters.CancellationToken;
                _semanticModel = parameters.SemanticModel;
                _renamedSymbol = parameters.RenameSymbol;
                _replacementTextValid = parameters.ReplacementTextValid;
                _renameSpansTracker = parameters.RenameSpansTracker;
                _isRenamingInStrings = parameters.OptionSet.GetOption(RenameOptions.RenameInStrings);
                _isRenamingInComments = parameters.OptionSet.GetOption(RenameOptions.RenameInComments);
                _stringAndCommentTextSpans = parameters.StringAndCommentTextSpans;
                _renameAnnotations = parameters.RenameAnnotations;

                _aliasSymbol = _renamedSymbol as IAliasSymbol;
                _renamableDeclarationLocation = _renamedSymbol.Locations.FirstOrDefault(loc => loc.IsInSource && loc.SourceTree == _semanticModel.SyntaxTree);
                _isVerbatim = _replacementText.StartsWith("@", StringComparison.Ordinal);

                _simplificationService = parameters.Document.Project.LanguageServices.GetService<ISimplificationService>();
                _semanticFactsService = parameters.Document.Project.LanguageServices.GetService<ISemanticFactsService>();
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return node;
                }

                var isInConflictLambdaBody = false;
                var lambdas = node.GetAncestorsOrThis(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax);
                if (lambdas.Count() != 0)
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
                    _skipRenameForComplexification += shouldComplexifyNode ? 1 : 0;
                    result = base.Visit(node);
                    _skipRenameForComplexification -= shouldComplexifyNode ? 1 : 0;
                    result = Complexify(node, result);
                }
                else
                {
                    result = base.Visit(node);
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
                var shouldCheckTrivia = _stringAndCommentTextSpans.Contains(token.Span);
                _isProcessingTrivia += shouldCheckTrivia ? 1 : 0;
                var newToken = base.VisitToken(token);
                _isProcessingTrivia -= shouldCheckTrivia ? 1 : 0;

                // Handle Alias annotations
                newToken = UpdateAliasAnnotation(newToken);

                // Rename matches in strings and comments
                newToken = RenameWithinToken(token, newToken);

                // We don't want to annotate XmlName with RenameActionAnnotation
                if (newToken.Parent.IsKind(SyntaxKind.XmlName))
                {
                    return newToken;
                }

                bool isRenameLocation = IsRenameLocation(token);

                // if this is a reference location, or the identifier token's name could possibly
                // be a conflict, we need to process this token
                var isOldText = token.ValueText == _originalText;
                var tokenNeedsConflictCheck =
                    isRenameLocation ||
                    token.ValueText == _replacementText ||
                    isOldText ||
                    _possibleNameConflicts.Contains(token.ValueText);

                if (tokenNeedsConflictCheck)
                {
                    newToken = RenameAndAnnotateAsync(token, newToken, isRenameLocation, isOldText).WaitAndGetResult_CanCallOnBackground(_cancellationToken);

                    if (!_isProcessingComplexifiedSpans)
                    {
                        _invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors<InvocationExpressionSyntax>());
                    }
                }

                return newToken;
            }

            private SyntaxNode Complexify(SyntaxNode originalNode, SyntaxNode newNode)
            {
                _isProcessingComplexifiedSpans = true;
                _modifiedSubSpans = new List<ValueTuple<TextSpan, TextSpan>>();

                var annotation = new SyntaxAnnotation();
                newNode = newNode.WithAdditionalAnnotations(annotation);
                var speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode);
                newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

                _speculativeModel = GetSemanticModelForNode(newNode, _semanticModel);
                Debug.Assert(_speculativeModel != null, "expanding a syntax node which cannot be speculated?");

                var oldSpan = originalNode.Span;
                var expandParameter = originalNode.GetAncestorsOrThis(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax).Count() == 0;

                newNode = _simplificationService.Expand(newNode,
                                                                    _speculativeModel,
                                                                    annotationForReplacedAliasIdentifier: null,
                                                                    expandInsideNode: null,
                                                                    expandParameter: expandParameter,
                                                                    cancellationToken: _cancellationToken);
                speculativeTree = originalNode.SyntaxTree.GetRoot(_cancellationToken).ReplaceNode(originalNode, newNode);
                newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

                _speculativeModel = GetSemanticModelForNode(newNode, _semanticModel);

                newNode = base.Visit(newNode);
                var newSpan = newNode.Span;

                newNode = newNode.WithoutAnnotations(annotation);
                newNode = _renameAnnotations.WithAdditionalAnnotations(newNode, new RenameNodeSimplificationAnnotation() { OriginalTextSpan = oldSpan });

                _renameSpansTracker.AddComplexifiedSpan(_documentId, oldSpan, new TextSpan(oldSpan.Start, newSpan.Length), _modifiedSubSpans);
                _modifiedSubSpans = null;

                _isProcessingComplexifiedSpans = false;
                _speculativeModel = null;
                return newNode;
            }

            private bool IsExpandWithinMultiLineLambda(SyntaxNode node)
            {
                if (node == null)
                {
                    return false;
                }

                if (_conflictLocations.Contains(node.Span))
                {
                    return true;
                }

                if (node.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
                {
                    var parent = (ParenthesizedLambdaExpressionSyntax)node;
                    if (ReferenceEquals(parent.ParameterList, node))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (node.IsParentKind(SyntaxKind.SimpleLambdaExpression))
                {
                    var parent = (SimpleLambdaExpressionSyntax)node;
                    if (ReferenceEquals(parent.Parameter, node))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            private async Task<SyntaxToken> RenameAndAnnotateAsync(SyntaxToken token, SyntaxToken newToken, bool isRenameLocation, bool isOldText)
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
                                newToken = RenameToken(token, newToken, annotation.Prefix, annotation.Suffix);
                                AddModifiedSpan(annotation.OriginalSpan, newToken.Span);
                            }
                            else
                            {
                                newToken = RenameToken(token, newToken, prefix: null, suffix: null);
                            }
                        }

                        return newToken;
                    }

                    var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, _semanticModel, _solution.Workspace, _cancellationToken);

                    string suffix = null;
                    string prefix = isRenameLocation && _renameLocations[token.Span].IsRenamableAccessor
                        ? newToken.ValueText.Substring(0, newToken.ValueText.IndexOf('_') + 1)
                        : null;

                    if (symbols.Count() == 1)
                    {
                        var symbol = symbols.Single();

                        if (symbol.IsConstructor())
                        {
                            symbol = symbol.ContainingSymbol;
                        }

                        var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(symbol, _solution, _cancellationToken).ConfigureAwait(false);
                        symbol = sourceDefinition ?? symbol;

                        if (symbol is INamedTypeSymbol)
                        {
                            var namedTypeSymbol = (INamedTypeSymbol)symbol;
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
                        newToken = RenameToken(token, newToken, prefix, suffix);

                        AddModifiedSpan(oldSpan, newToken.Span);
                    }

                    var renameDeclarationLocations = await
                        ConflictResolver.CreateDeclarationLocationAnnotationsAsync(_solution, symbols, _cancellationToken).ConfigureAwait(false);

                    var isNamespaceDeclarationReference = false;
                    if (isRenameLocation && token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword))
                    {
                        isNamespaceDeclarationReference = true;
                    }

                    var isMemberGroupReference = _semanticFactsService.IsNameOfContext(_semanticModel, token.Span.Start, _cancellationToken);

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
                    if (_renameRenamableSymbolDeclaration != null && _renamableDeclarationLocation == token.GetLocation())
                    {
                        newToken = _renameAnnotations.WithAdditionalAnnotations(newToken, _renameRenamableSymbolDeclaration);
                    }

                    return newToken;
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private RenameActionAnnotation GetAnnotationForInvocationExpression(InvocationExpressionSyntax invocationExpression)
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

                if (identifierToken != default(SyntaxToken) && !_annotatedIdentifierTokens.Contains(identifierToken))
                {
                    var symbolInfo = _semanticModel.GetSymbolInfo(invocationExpression, _cancellationToken);
                    IEnumerable<ISymbol> symbols = null;
                    if (symbolInfo.Symbol == null)
                    {
                        return null;
                    }
                    else
                    {
                        symbols = SpecializedCollections.SingletonEnumerable(symbolInfo.Symbol);
                    }

                    RenameDeclarationLocationReference[] renameDeclarationLocations =
                                                                                ConflictResolver.CreateDeclarationLocationAnnotationsAsync(
                                                                                    _solution,
                                                                                    symbols,
                                                                                    _cancellationToken)
                                                                                        .WaitAndGetResult_CanCallOnBackground(_cancellationToken);

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

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var result = base.VisitInvocationExpression(node);
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

            private bool IsRenameLocation(SyntaxToken token)
            {
                if (!_isProcessingComplexifiedSpans)
                {
                    return _renameLocations.ContainsKey(token.Span);
                }
                else
                {
                    if (token.HasAnnotations(AliasAnnotation.Kind))
                    {
                        return false;
                    }

                    if (token.HasAnnotations(RenameAnnotation.Kind))
                    {
                        return _renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().First().IsRenameLocation;
                    }

                    if (token.Parent is SimpleNameSyntax && !token.IsKind(SyntaxKind.GlobalKeyword) && token.Parent.Parent.IsKind(SyntaxKind.AliasQualifiedName, SyntaxKind.QualifiedCref, SyntaxKind.QualifiedName))
                    {
                        var symbol = _speculativeModel.GetSymbolInfo(token.Parent, _cancellationToken).Symbol;

                        if (symbol != null && _renamedSymbol.Kind != SymbolKind.Local && _renamedSymbol.Kind != SymbolKind.RangeVariable &&
                            (symbol == _renamedSymbol || SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false).Equals(symbol.GetSymbolKey(), _renamedSymbol.GetSymbolKey())))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private SyntaxToken UpdateAliasAnnotation(SyntaxToken newToken)
            {
                if (_aliasSymbol != null && !this.AnnotateForComplexification && newToken.HasAnnotations(AliasAnnotation.Kind))
                {
                    newToken = RenameUtilities.UpdateAliasAnnotation(newToken, _aliasSymbol, _replacementText);
                }

                return newToken;
            }

            private SyntaxToken RenameToken(SyntaxToken oldToken, SyntaxToken newToken, string prefix, string suffix)
            {
                var parent = oldToken.Parent;
                string currentNewIdentifier = _isVerbatim ? _replacementText.Substring(1) : _replacementText;
                var oldIdentifier = newToken.ValueText;
                var isAttributeName = SyntaxFacts.IsAttributeName(parent);

                if (isAttributeName)
                {
                    Debug.Assert(_renamedSymbol.IsAttribute() || _aliasSymbol.Target.IsAttribute());
                    if (oldIdentifier != _renamedSymbol.Name)
                    {
                        string withoutSuffix;
                        if (currentNewIdentifier.TryGetWithoutAttributeSuffix(out withoutSuffix))
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
                        currentNewIdentifier = currentNewIdentifier + suffix;
                    }
                }

                // determine the canonical identifier name (unescaped, no unicode escaping, ...)
                string valueText = currentNewIdentifier;
                var kind = SyntaxFacts.GetKeywordKind(currentNewIdentifier);
                if (kind != SyntaxKind.None)
                {
                    valueText = SyntaxFacts.GetText(kind);
                }
                else
                {
                    var parsedIdentifier = SyntaxFactory.ParseName(currentNewIdentifier);
                    if (parsedIdentifier.IsKind(SyntaxKind.IdentifierName))
                    {
                        valueText = ((IdentifierNameSyntax)parsedIdentifier).Identifier.ValueText;
                    }
                }

                // TODO: we can't use escaped unicode characters in xml doc comments, so we need to pass the valuetext as text as well.
                // <param name="\u... is invalid.

                // if it's an attribute name we don't mess with the escaping because it might change overload resolution
                newToken = _isVerbatim || (isAttributeName && oldToken.IsVerbatimIdentifier())
                    ? newToken = newToken.CopyAnnotationsTo(SyntaxFactory.VerbatimIdentifier(newToken.LeadingTrivia, currentNewIdentifier, valueText, newToken.TrailingTrivia))
                    : newToken = newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(newToken.LeadingTrivia, SyntaxKind.IdentifierToken, currentNewIdentifier, valueText, newToken.TrailingTrivia));

                if (_replacementTextValid)
                {
                    if (newToken.IsVerbatimIdentifier())
                    {
                        // a reference location should always be tried to be unescaped, whether it was escaped before rename 
                        // or the replacement itself is escaped.
                        newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation);
                    }
                    else
                    {
                        var semanticModel = GetSemanticModelForNode(parent, _speculativeModel ?? _semanticModel);
                        newToken = Simplification.CSharpSimplificationService.TryEscapeIdentifierToken(newToken, parent, semanticModel);
                    }
                }

                return newToken;
            }

            private SyntaxToken RenameInStringLiteral(SyntaxToken oldToken, SyntaxToken newToken, Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> createNewStringLiteral)
            {
                var originalString = newToken.ToString();
                string replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, _originalText, _replacementText);
                if (replacedString != originalString)
                {
                    var oldSpan = oldToken.Span;
                    newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia);
                    AddModifiedSpan(oldSpan, newToken.Span);
                    return newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
                }

                return newToken;
            }

            private SyntaxToken RenameInTrivia(SyntaxToken token, IEnumerable<SyntaxTrivia> leadingOrTrailingTriviaList)
            {
                return token.ReplaceTrivia(leadingOrTrailingTriviaList, (oldTrivia, newTrivia) =>
                {
                    if (newTrivia.IsSingleLineComment() || newTrivia.IsMultiLineComment())
                    {
                        return RenameInCommentTrivia(newTrivia);
                    }

                    return newTrivia;
                });
            }

            private SyntaxTrivia RenameInCommentTrivia(SyntaxTrivia trivia)
            {
                var originalString = trivia.ToString();
                string replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, _originalText, _replacementText);
                if (replacedString != originalString)
                {
                    var oldSpan = trivia.Span;
                    var newTrivia = SyntaxFactory.Comment(replacedString);
                    AddModifiedSpan(oldSpan, newTrivia.Span);
                    return trivia.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newTrivia, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
                }

                return trivia;
            }

            private SyntaxToken RenameWithinToken(SyntaxToken oldToken, SyntaxToken newToken)
            {
                if (_isProcessingComplexifiedSpans ||
                    (_isProcessingTrivia == 0 &&
                    !_stringAndCommentTextSpans.Contains(oldToken.Span)))
                {
                    return newToken;
                }

                if (_isRenamingInStrings)
                {
                    if (newToken.IsKind(SyntaxKind.StringLiteralToken))
                    {
                        newToken = RenameInStringLiteral(oldToken, newToken, SyntaxFactory.Literal);
                    }
                    else if (newToken.IsKind(SyntaxKind.InterpolatedStringTextToken))
                    {
                        newToken = RenameInStringLiteral(oldToken, newToken, (leadingTrivia, text, value, trailingTrivia) =>
                            SyntaxFactory.Token(newToken.LeadingTrivia, SyntaxKind.InterpolatedStringTextToken, text, value, newToken.TrailingTrivia));
                    }
                }

                if (_isRenamingInComments)
                {
                    if (newToken.IsKind(SyntaxKind.XmlTextLiteralToken))
                    {
                        newToken = RenameInStringLiteral(oldToken, newToken, SyntaxFactory.XmlTextLiteral);
                    }
                    else if (newToken.IsKind(SyntaxKind.IdentifierToken) && newToken.Parent.IsKind(SyntaxKind.XmlName) && newToken.ValueText == _originalText)
                    {
                        var newIdentifierToken = SyntaxFactory.Identifier(newToken.LeadingTrivia, _replacementText, newToken.TrailingTrivia);
                        newToken = newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldToken.Span }));
                        AddModifiedSpan(oldToken.Span, newToken.Span);
                    }

                    if (newToken.HasLeadingTrivia)
                    {
                        var updatedToken = RenameInTrivia(oldToken, oldToken.LeadingTrivia);
                        if (updatedToken != oldToken)
                        {
                            newToken = newToken.WithLeadingTrivia(updatedToken.LeadingTrivia);
                        }
                    }

                    if (newToken.HasTrailingTrivia)
                    {
                        var updatedToken = RenameInTrivia(oldToken, oldToken.TrailingTrivia);
                        if (updatedToken != oldToken)
                        {
                            newToken = newToken.WithTrailingTrivia(updatedToken.TrailingTrivia);
                        }
                    }
                }

                return newToken;
            }
        }

        #endregion

        #region "Declaration Conflicts"

        public bool LocalVariableConflict(
            SyntaxToken token,
            IEnumerable<ISymbol> newReferencedSymbols)
        {
            if (token.Parent.IsKind(SyntaxKind.IdentifierName) &&
                token.Parent.IsParentKind(SyntaxKind.InvocationExpression) &&
                token.GetPreviousToken().Kind() != SyntaxKind.DotToken &&
                token.GetNextToken().Kind() != SyntaxKind.DotToken)
            {
                var expression = (ExpressionSyntax)token.Parent;
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

        public async Task<IEnumerable<Location>> ComputeDeclarationConflictsAsync(
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
                var conflicts = new List<Location>();

                // If we're renaming a named type, we can conflict with members w/ our same name.  Note:
                // this doesn't apply to enums.
                if (renamedSymbol.Kind == SymbolKind.NamedType &&
                    ((INamedTypeSymbol)renamedSymbol).TypeKind != TypeKind.Enum)
                {
                    var namedType = (INamedTypeSymbol)renamedSymbol;
                    AddSymbolSourceSpans(conflicts, namedType.GetMembers(renamedSymbol.Name), reverseMappedLocations);
                }

                // If we're contained in a named type (we may be a named type ourself!) then we have a
                // conflict.  NOTE(cyrusn): This does not apply to enums. 
                if (renamedSymbol.ContainingSymbol is INamedTypeSymbol &&
                    renamedSymbol.ContainingType.Name == renamedSymbol.Name &&
                    renamedSymbol.ContainingType.TypeKind != TypeKind.Enum)
                {
                    AddSymbolSourceSpans(conflicts, SpecializedCollections.SingletonEnumerable(renamedSymbol.ContainingType), reverseMappedLocations);
                }

                if (renamedSymbol.Kind == SymbolKind.Parameter ||
                    renamedSymbol.Kind == SymbolKind.Local ||
                    renamedSymbol.Kind == SymbolKind.RangeVariable)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);

                    var methodDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LocalConflictVisitor(token);
                    visitor.Visit(methodDeclaration);
                    conflicts.AddRange(visitor.ConflictingTokens.Select(t => reverseMappedLocations[t.GetLocation()]));
                }
                else if (renamedSymbol.Kind == SymbolKind.Label)
                {
                    var token = renamedSymbol.Locations.Single().FindToken(cancellationToken);

                    var methodDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
                    var visitor = new LabelConflictVisitor(token);
                    visitor.Visit(methodDeclaration);
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
                        var property = await RenameLocations.ReferenceProcessing.GetPropertyFromAccessorOrAnOverride(
                            referencedSymbol, baseSolution, cancellationToken).ConfigureAwait(false);
                        if (property != null)
                        {
                            properties.Add(property);
                        }
                    }

                    ConflictResolver.AddConflictingParametersOfProperties(properties.Distinct(), replacementText, conflicts);
                }
                else if (renamedSymbol.Kind == SymbolKind.Alias)
                {
                    // in C# there can only be one using with the same alias name in the same block (top of file of namespace). 
                    // It's ok to redefine the alias in different blocks.
                    var location = renamedSymbol.Locations.Single();
                    var token = await location.SourceTree.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                    var currentUsing = (UsingDirectiveSyntax)token.Parent.Parent.Parent;

                    var namespaceDecl = token.Parent.GetAncestorsOrThis(n => n.Kind() == SyntaxKind.NamespaceDeclaration).FirstOrDefault();
                    SyntaxList<UsingDirectiveSyntax> usings;
                    if (namespaceDecl != null)
                    {
                        usings = ((NamespaceDeclarationSyntax)namespaceDecl).Usings;
                    }
                    else
                    {
                        var compilationUnit = (CompilationUnitSyntax)token.Parent.GetAncestorsOrThis(n => n.Kind() == SyntaxKind.CompilationUnit).Single();
                        usings = compilationUnit.Usings;
                    }

                    foreach (var usingDirective in usings)
                    {
                        if (usingDirective.Alias != null && usingDirective != currentUsing)
                        {
                            if (usingDirective.Alias.Name.Identifier.ValueText == currentUsing.Alias.Name.Identifier.ValueText)
                            {
                                conflicts.Add(reverseMappedLocations[usingDirective.Alias.Name.GetLocation()]);
                            }
                        }
                    }
                }
                else if (renamedSymbol.Kind == SymbolKind.TypeParameter)
                {
                    var location = renamedSymbol.Locations.Single();
                    var token = await location.SourceTree.GetTouchingTokenAsync(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
                    var currentTypeParameter = token.Parent;

                    foreach (var typeParameter in ((TypeParameterListSyntax)currentTypeParameter.Parent).Parameters)
                    {
                        if (typeParameter != currentTypeParameter && token.ValueText == typeParameter.Identifier.ValueText)
                        {
                            conflicts.Add(reverseMappedLocations[typeParameter.Identifier.GetLocation()]);
                        }
                    }
                }

                // if the renamed symbol is a type member, it's name should not conflict with a type parameter
                if (renamedSymbol.ContainingType != null && renamedSymbol.ContainingType.GetMembers(renamedSymbol.Name).Contains(renamedSymbol))
                {
                    foreach (var typeParameter in renamedSymbol.ContainingType.TypeParameters)
                    {
                        if (typeParameter.Name == renamedSymbol.Name)
                        {
                            var typeParameterToken = typeParameter.Locations.Single().FindToken(cancellationToken);
                            conflicts.Add(reverseMappedLocations[typeParameterToken.GetLocation()]);
                        }
                    }
                }

                return conflicts;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<ISymbol> GetVBPropertyFromAccessorOrAnOverrideAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            try
            {
                if (symbol.IsPropertyAccessor())
                {
                    var property = ((IMethodSymbol)symbol).AssociatedSymbol;

                    return property.Language == LanguageNames.VisualBasic ? property : null;
                }

                if (symbol.IsOverride && symbol.OverriddenMember() != null)
                {
                    var originalSourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol.OverriddenMember(), solution, cancellationToken).ConfigureAwait(false);

                    if (originalSourceSymbol != null)
                    {
                        return await GetVBPropertyFromAccessorOrAnOverrideAsync(originalSourceSymbol, solution, cancellationToken).ConfigureAwait(false);
                    }
                }

                return null;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void AddSymbolSourceSpans(List<Location> conflicts, IEnumerable<ISymbol> symbols, IDictionary<Location, Location> reverseMappedLocations)
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

        public async Task<IEnumerable<Location>> ComputeImplicitReferenceConflictsAsync(ISymbol renameSymbol, ISymbol renamedSymbol, IEnumerable<ReferenceLocation> implicitReferenceLocations, CancellationToken cancellationToken)
        {
            // Handle renaming of symbols used for foreach
            bool implicitReferencesMightConflict = renameSymbol.Kind == SymbolKind.Property &&
                                                string.Compare(renameSymbol.Name, "Current", StringComparison.OrdinalIgnoreCase) == 0;
            implicitReferencesMightConflict = implicitReferencesMightConflict ||
                                                (renameSymbol.Kind == SymbolKind.Method &&
                                                    (string.Compare(renameSymbol.Name, "MoveNext", StringComparison.OrdinalIgnoreCase) == 0 ||
                                                    string.Compare(renameSymbol.Name, "GetEnumerator", StringComparison.OrdinalIgnoreCase) == 0));

            // TODO: handle Dispose for using statement and Add methods for collection initializers.

            if (implicitReferencesMightConflict)
            {
                if (renamedSymbol.Name != renameSymbol.Name)
                {
                    foreach (var implicitReferenceLocation in implicitReferenceLocations)
                    {
                        var token = await implicitReferenceLocation.Location.SourceTree.GetTouchingTokenAsync(
                            implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, findInsideTrivia: false).ConfigureAwait(false);

                        switch (token.Kind())
                        {
                            case SyntaxKind.ForEachKeyword:
                                return SpecializedCollections.SingletonEnumerable(((ForEachStatementSyntax)token.Parent).Expression.GetLocation());
                        }
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<Location>();
        }

        public IEnumerable<Location> ComputePossibleImplicitUsageConflicts(
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
                var baseType = renamedSymbol.ContainingType.GetBaseTypes().FirstOrDefault();
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
                                    return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation);
                                }
                            }
                            else if (symbol.Name == "GetEnumerator")
                            {
                                // we are a bit pessimistic here. 
                                // To be sure we would need to check if the returned type is having a MoveNext and Current as required by foreach
                                if (!method.ReturnsVoid &&
                                    !method.Parameters.Any())
                                {
                                    return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation);
                                }
                            }
                        }
                        else if (symbol.Kind == SymbolKind.Property && symbol.Name == "Current")
                        {
                            var property = (IPropertySymbol)symbol;

                            if (!property.Parameters.Any() && !property.IsWriteOnly)
                            {
                                return SpecializedCollections.SingletonEnumerable(originalDeclarationLocation);
                            }
                        }
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<Location>();
        }

        #endregion

        public void TryAddPossibleNameConflicts(ISymbol symbol, string replacementText, ICollection<string> possibleNameConflicts)
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
            string valueText = replacementText;
            SyntaxKind kind = SyntaxFacts.GetKeywordKind(replacementText);
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
        public SyntaxNode GetExpansionTargetForLocation(SyntaxToken token)
        {
            return GetExpansionTarget(token);
        }

        private static SyntaxNode GetExpansionTarget(SyntaxToken token)
        {
            // get the directly enclosing statement
            var enclosingStatement = token.GetAncestors(n => n is StatementSyntax).FirstOrDefault();

            // System.Func<int, int> myFunc = arg => X;
            SyntaxNode possibleLambdaExpression = enclosingStatement == null
                ? token.GetAncestors(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax).FirstOrDefault()
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

        public bool IsIdentifierValid(string replacementText, ISyntaxFactsService syntaxFactsService)
        {
            string escapedIdentifier;
            if (replacementText.StartsWith("@", StringComparison.Ordinal))
            {
                escapedIdentifier = replacementText;
            }
            else
            {
                escapedIdentifier = "@" + replacementText;
            }

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
        public static SemanticModel GetSemanticModelForNode(SyntaxNode node, SemanticModel originalSemanticModel)
        {
            if (node.SyntaxTree == originalSemanticModel.SyntaxTree)
            {
                // This is possible if the previous rename phase didn't rewrite any nodes in this tree.
                return originalSemanticModel;
            }

            var nodeToSpeculate = node.GetAncestorsOrThis(n => SpeculationAnalyzer.CanSpeculateOnNode(n)).LastOrDefault();
            if (nodeToSpeculate == null)
            {
                if (node.IsKind(SyntaxKind.NameMemberCref))
                {
                    nodeToSpeculate = ((NameMemberCrefSyntax)node).Name;
                }
                else if (node.IsKind(SyntaxKind.QualifiedCref))
                {
                    nodeToSpeculate = ((QualifiedCrefSyntax)node).Container;
                }
                else if (node.IsKind(SyntaxKind.TypeConstraint))
                {
                    nodeToSpeculate = ((TypeConstraintSyntax)node).Type;
                }
                else if (node is BaseTypeSyntax)
                {
                    nodeToSpeculate = ((BaseTypeSyntax)node).Type;
                }
                else
                {
                    return null;
                }
            }

            bool isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(node as ExpressionSyntax);
            var position = nodeToSpeculate.SpanStart;
            return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, originalSemanticModel, position, isInNamespaceOrTypeContext);
        }

        #endregion
    }
}
