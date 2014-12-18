// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly DocumentId documentId;
            private readonly RenameAnnotation renameRenamableSymbolDeclaration;
            private readonly Solution solution;
            private readonly string replacementText;
            private readonly string originalText;
            private readonly ICollection<string> possibleNameConflicts;
            private readonly Dictionary<TextSpan, RenameLocation> renameLocations;
            private readonly ISet<TextSpan> conflictLocations;
            private readonly SemanticModel semanticModel;
            private readonly CancellationToken cancellationToken;

            private readonly ISymbol renamedSymbol;
            private readonly IAliasSymbol aliasSymbol;
            private readonly Location renamableDeclarationLocation;

            private readonly RenamedSpansTracker renameSpansTracker;
            private readonly bool isVerbatim;
            private readonly bool replacementTextValid;
            private readonly bool isRenamingInStrings;
            private readonly bool isRenamingInComments;
            private readonly ISet<TextSpan> stringAndCommentTextSpans;
            private readonly ISimplificationService simplificationService;
            private readonly HashSet<SyntaxToken> annotatedIdentifierTokens = new HashSet<SyntaxToken>();
            private readonly HashSet<InvocationExpressionSyntax> invocationExpressionsNeedingConflictChecks = new HashSet<InvocationExpressionSyntax>();

            private readonly AnnotationTable<RenameAnnotation> renameAnnotations;

            public bool AnnotateForComplexification
            {
                get
                {
                    return this.skipRenameForComplexification > 0 && !this.isProcessingComplexifiedSpans;
                }
            }

            private int skipRenameForComplexification = 0;
            private bool isProcessingComplexifiedSpans;
            private List<ValueTuple<TextSpan, TextSpan>> modifiedSubSpans = null;
            private SemanticModel speculativeModel;
            private int isProcessingTrivia;

            private void AddModifiedSpan(TextSpan oldSpan, TextSpan newSpan)
            {
                newSpan = new TextSpan(oldSpan.Start, newSpan.Length);

                if (!this.isProcessingComplexifiedSpans)
                {
                    renameSpansTracker.AddModifiedSpan(documentId, oldSpan, newSpan);
                }
                else
                {
                    this.modifiedSubSpans.Add(ValueTuple.Create(oldSpan, newSpan));
                }
            }

            public RenameRewriter(RenameRewriterParameters parameters)
                : base(visitIntoStructuredTrivia: true)
            {
                this.documentId = parameters.Document.Id;
                this.renameRenamableSymbolDeclaration = parameters.RenamedSymbolDeclarationAnnotation;
                this.solution = parameters.OriginalSolution;
                this.replacementText = parameters.ReplacementText;
                this.originalText = parameters.OriginalText;
                this.possibleNameConflicts = parameters.PossibleNameConflicts;
                this.renameLocations = parameters.RenameLocations;
                this.conflictLocations = parameters.ConflictLocationSpans;
                this.cancellationToken = parameters.CancellationToken;
                this.semanticModel = (SemanticModel)parameters.SemanticModel;
                this.renamedSymbol = parameters.RenameSymbol;
                this.replacementTextValid = parameters.ReplacementTextValid;
                this.renameSpansTracker = parameters.RenameSpansTracker;
                this.isRenamingInStrings = parameters.OptionSet.GetOption(RenameOptions.RenameInStrings);
                this.isRenamingInComments = parameters.OptionSet.GetOption(RenameOptions.RenameInComments);
                this.stringAndCommentTextSpans = parameters.StringAndCommentTextSpans;
                this.renameAnnotations = parameters.RenameAnnotations;

                this.aliasSymbol = this.renamedSymbol as IAliasSymbol;
                this.renamableDeclarationLocation = this.renamedSymbol.Locations.Where(loc => loc.IsInSource && loc.SourceTree == semanticModel.SyntaxTree).FirstOrDefault();
                this.isVerbatim = this.replacementText.StartsWith("@");

                this.simplificationService = parameters.Document.Project.LanguageServices.GetService<ISimplificationService>();
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
                        if (this.conflictLocations.Any(cf => cf.Contains(lambda.Span)))
                        {
                            isInConflictLambdaBody = true;
                            break;
                        }
                    }
                }

                var shouldComplexifyNode =
                    !isInConflictLambdaBody &&
                    this.skipRenameForComplexification == 0 &&
                    !this.isProcessingComplexifiedSpans &&
                    this.conflictLocations.Contains(node.Span);

                SyntaxNode result;

                // in case the current node was identified as being a complexification target of
                // a previous node, we'll handle it accordingly.
                if (shouldComplexifyNode)
                {
                    this.skipRenameForComplexification += shouldComplexifyNode ? 1 : 0;
                    result = base.Visit(node);
                    this.skipRenameForComplexification -= shouldComplexifyNode ? 1 : 0;
                    result = Complexify(node, result);
                }
                else
                {
                    result = base.Visit(node);
                }

                return result;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var shouldCheckTrivia = this.stringAndCommentTextSpans.Contains(token.Span);
                this.isProcessingTrivia += shouldCheckTrivia ? 1 : 0;
                var newToken = base.VisitToken(token);
                this.isProcessingTrivia -= shouldCheckTrivia ? 1 : 0;

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
                var isOldText = token.ValueText == originalText;
                var tokenNeedsConflictCheck =
                    isRenameLocation ||
                    token.ValueText == replacementText ||
                    isOldText ||
                    possibleNameConflicts.Contains(token.ValueText);

                if (tokenNeedsConflictCheck)
                {
                    newToken = RenameAndAnnotateAsync(token, newToken, isRenameLocation, isOldText).WaitAndGetResult(cancellationToken);

                    if (!this.isProcessingComplexifiedSpans)
                    {
                        invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors<InvocationExpressionSyntax>());
                    }
                }

                return newToken;
            }

            private SyntaxNode Complexify(SyntaxNode originalNode, SyntaxNode newNode)
            {
                this.isProcessingComplexifiedSpans = true;
                this.modifiedSubSpans = new List<ValueTuple<TextSpan, TextSpan>>();

                var annotation = new SyntaxAnnotation();
                newNode = newNode.WithAdditionalAnnotations(annotation);
                var speculativeTree = originalNode.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(originalNode, newNode);
                newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

                this.speculativeModel = GetSemanticModelForNode(newNode, this.semanticModel);
                Debug.Assert(speculativeModel != null, "expanding a syntax node which cannot be speculated?");

                var oldSpan = originalNode.Span;
                var expandParameter = originalNode.GetAncestorsOrThis(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax).Count() == 0;

                newNode = (SyntaxNode)simplificationService.Expand(newNode,
                                                                    speculativeModel,
                                                                    annotationForReplacedAliasIdentifier: null,
                                                                    expandInsideNode: null,
                                                                    expandParameter: expandParameter,
                                                                    cancellationToken: cancellationToken);
                speculativeTree = originalNode.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(originalNode, newNode);
                newNode = speculativeTree.GetAnnotatedNodes<SyntaxNode>(annotation).First();

                this.speculativeModel = GetSemanticModelForNode(newNode, this.semanticModel);

                newNode = base.Visit(newNode);
                var newSpan = newNode.Span;

                newNode = newNode.WithoutAnnotations(annotation);
                newNode = this.renameAnnotations.WithAdditionalAnnotations(newNode, new RenameNodeSimplificationAnnotation() { OriginalTextSpan = oldSpan });

                this.renameSpansTracker.AddComplexifiedSpan(this.documentId, oldSpan, new TextSpan(oldSpan.Start, newSpan.Length), this.modifiedSubSpans);
                this.modifiedSubSpans = null;

                this.isProcessingComplexifiedSpans = false;
                this.speculativeModel = null;
                return newNode;
            }

            private bool IsExpandWithinMultiLineLambda(SyntaxNode node)
            {
                if (node == null)
                {
                    return false;
                }

                if (this.conflictLocations.Contains(node.Span))
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
                if (this.isProcessingComplexifiedSpans)
                {
                    // Rename Token
                    if (isRenameLocation)
                    {
                        var annotation = this.renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().FirstOrDefault();
                        if (annotation != null)
                        {
                            newToken = RenameToken(token, newToken, annotation.Suffix, annotation.IsAccessorLocation);
                            AddModifiedSpan(annotation.OriginalSpan, newToken.Span);
                        }
                        else
                        {
                            newToken = RenameToken(token, newToken, suffix: null, isAccessorLocation: false);
                        }
                    }

                    return newToken;
                }

                var symbols = RenameUtilities.GetSymbolsTouchingPosition(token.Span.Start, this.semanticModel, this.solution.Workspace, this.cancellationToken);

                string suffix = null;
                if (symbols.Count() == 1)
                {
                    var symbol = symbols.Single();

                    if (symbol.IsConstructor())
                    {
                        symbol = symbol.ContainingSymbol;
                    }

                    var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
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
                    newToken = RenameToken(
                        token,
                        newToken,
                        suffix: suffix,
                        isAccessorLocation: isRenameLocation && this.renameLocations[token.Span].IsRenamableAccessor);

                    AddModifiedSpan(oldSpan, newToken.Span);
                }

                RenameDeclarationLocationReference[] renameDeclarationLocations =
                    ConflictResolver.CreateDeclarationLocationAnnotationsAsync(solution, symbols, cancellationToken)
                            .WaitAndGetResult(cancellationToken);

                var isNamespaceDeclarationReference = false;
                if (isRenameLocation && token.GetPreviousToken().IsKind(SyntaxKind.NamespaceKeyword))
                {
                    isNamespaceDeclarationReference = true;
                }

                var renameAnnotation =
                        new RenameActionAnnotation(
                            token.Span,
                            isRenameLocation,
                            isRenameLocation ? this.renameLocations[token.Span].IsRenamableAccessor : false,
                            suffix,
                            renameDeclarationLocations: renameDeclarationLocations,
                            isOriginalTextLocation: isOldText,
                            isNamespaceDeclarationReference: isNamespaceDeclarationReference,
                            isInvocationExpression: false);

                newToken = this.renameAnnotations.WithAdditionalAnnotations(newToken, renameAnnotation, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span });

                annotatedIdentifierTokens.Add(token);
                if (this.renameRenamableSymbolDeclaration != null && renamableDeclarationLocation == token.GetLocation())
                {
                    newToken = this.renameAnnotations.WithAdditionalAnnotations(newToken, this.renameRenamableSymbolDeclaration);
                }

                return newToken;
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

                if (identifierToken != default(SyntaxToken) && !this.annotatedIdentifierTokens.Contains(identifierToken))
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken);
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
                                                                                    solution,
                                                                                    symbols,
                                                                                    cancellationToken)
                                                                                        .WaitAndGetResult(cancellationToken);

                    var renameAnnotation = new RenameActionAnnotation(
                                                identifierToken.Span,
                                                isRenameLocation: false,
                                                isAccessorLocation: false,
                                                suffix: null,
                                                renameDeclarationLocations: renameDeclarationLocations,
                                                isOriginalTextLocation: false,
                                                isNamespaceDeclarationReference: false,
                                                isInvocationExpression: true);

                    return renameAnnotation;
                }

                return null;
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var result = base.VisitInvocationExpression(node);
                if (invocationExpressionsNeedingConflictChecks.Contains(node))
                {
                    var renameAnnotation = GetAnnotationForInvocationExpression(node);
                    if (renameAnnotation != null)
                    {
                        result = this.renameAnnotations.WithAdditionalAnnotations(result, renameAnnotation);
                    }
                }

                return result;
            }

            private bool IsRenameLocation(SyntaxToken token)
            {
                if (!this.isProcessingComplexifiedSpans)
                {
                    return this.renameLocations.ContainsKey(token.Span);
                }
                else
                {
                    if (token.HasAnnotations(AliasAnnotation.Kind))
                    {
                        return false;
                    }

                    if (token.HasAnnotations(RenameAnnotation.Kind))
                    {
                        return this.renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().First().IsRenameLocation;
                    }

                    if (token.Parent is SimpleNameSyntax && !token.IsKind(SyntaxKind.GlobalKeyword) && token.Parent.Parent.IsKind(SyntaxKind.AliasQualifiedName, SyntaxKind.QualifiedCref, SyntaxKind.QualifiedName))
                    {
                        var symbol = this.speculativeModel.GetSymbolInfo(token.Parent, this.cancellationToken).Symbol;

                        if (symbol != null && this.renamedSymbol.Kind != SymbolKind.Local && this.renamedSymbol.Kind != SymbolKind.RangeVariable &&
                            (symbol == this.renamedSymbol || SymbolKey.GetComparer(ignoreCase: true, ignoreAssemblyKeys: false).Equals(symbol.GetSymbolKey(), this.renamedSymbol.GetSymbolKey())))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private SyntaxToken UpdateAliasAnnotation(SyntaxToken newToken)
            {
                if (this.aliasSymbol != null && !this.AnnotateForComplexification && newToken.HasAnnotations(AliasAnnotation.Kind))
                {
                    newToken = (SyntaxToken)RenameUtilities.UpdateAliasAnnotation(newToken, this.aliasSymbol, this.replacementText);
                }

                return newToken;
            }

            private SyntaxToken RenameToken(SyntaxToken oldToken, SyntaxToken newToken, string suffix, bool isAccessorLocation)
            {
                var parent = oldToken.Parent;
                string currentNewIdentifier = this.isVerbatim ? this.replacementText.Substring(1) : this.replacementText;
                var oldIdentifier = newToken.ValueText;
                var isAttributeName = SyntaxFacts.IsAttributeName(parent);

                if (isAttributeName)
                {
                    Debug.Assert(this.renamedSymbol.IsAttribute() || this.aliasSymbol.Target.IsAttribute());
                    if (oldIdentifier != this.renamedSymbol.Name)
                    {
                        string withoutSuffix;
                        if (currentNewIdentifier.TryGetWithoutAttributeSuffix(out withoutSuffix))
                        {
                            currentNewIdentifier = withoutSuffix;
                        }
                    }
                }
                else if (isAccessorLocation)
                {
                    var prefix = oldIdentifier.Substring(0, oldIdentifier.IndexOf("_") + 1);
                    currentNewIdentifier = prefix + currentNewIdentifier;
                }
                else if (!string.IsNullOrEmpty(suffix))
                {
                    currentNewIdentifier = this.replacementText + suffix;
                }

                // determine the canonical identifier name (unescaped, no unicode escaping, ...)
                string valueText;
                var kind = SyntaxFacts.GetKeywordKind(currentNewIdentifier);
                if (kind != SyntaxKind.None)
                {
                    valueText = SyntaxFacts.GetText(kind);
                }
                else
                {
                    var parsedIdentifier = (IdentifierNameSyntax)SyntaxFactory.ParseName(currentNewIdentifier);
                    Debug.Assert(parsedIdentifier.Kind() == SyntaxKind.IdentifierName);
                    valueText = parsedIdentifier.Identifier.ValueText;
                }

                // TODO: we can't use escaped unicode characters in xml doc comments, so we need to pass the valuetext as text as well.
                // <param name="\u... is invalid.

                // if it's an attribute name we don't mess with the escaping because it might change overload resolution
                newToken = this.isVerbatim || (isAttributeName && oldToken.IsVerbatimIdentifier())
                    ? newToken = newToken.CopyAnnotationsTo(SyntaxFactory.VerbatimIdentifier(newToken.LeadingTrivia, currentNewIdentifier, valueText, newToken.TrailingTrivia))
                    : newToken = newToken.CopyAnnotationsTo(SyntaxFactory.Identifier(newToken.LeadingTrivia, SyntaxKind.IdentifierToken, currentNewIdentifier, valueText, newToken.TrailingTrivia));

                if (this.replacementTextValid)
                {
                    if (newToken.IsVerbatimIdentifier())
                    {
                        // a reference location should always be tried to be unescaped, whether it was escaped before rename 
                        // or the replacement itself is escaped.
                        newToken = newToken.WithAdditionalAnnotations(Simplifier.Annotation);
                    }
                    else
                    {
                        var semanticModel = GetSemanticModelForNode(parent, this.speculativeModel ?? this.semanticModel);
                        newToken = Simplification.CSharpSimplificationService.TryEscapeIdentifierToken(newToken, parent, semanticModel);
                    }
                }

                return newToken;
            }

            private SyntaxToken RenameInStringLiteral(SyntaxToken oldToken, SyntaxToken newToken, Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> createNewStringLiteral)
            {
                var originalString = newToken.ToString();
                string replacedString = RenameLocationSet.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, originalText, replacementText);
                if (replacedString != originalString)
                {
                    var oldSpan = oldToken.Span;
                    newToken = createNewStringLiteral(newToken.LeadingTrivia, replacedString, replacedString, newToken.TrailingTrivia);
                    AddModifiedSpan(oldSpan, newToken.Span);
                    return newToken.CopyAnnotationsTo(this.renameAnnotations.WithAdditionalAnnotations(newToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
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
                string replacedString = RenameLocationSet.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, originalText, replacementText);
                if (replacedString != originalString)
                {
                    var oldSpan = trivia.Span;
                    var newTrivia = SyntaxFactory.Comment(replacedString);
                    AddModifiedSpan(oldSpan, newTrivia.Span);
                    return trivia.CopyAnnotationsTo(this.renameAnnotations.WithAdditionalAnnotations(newTrivia, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
                }

                return trivia;
            }

            private SyntaxToken RenameWithinToken(SyntaxToken oldToken, SyntaxToken newToken)
            {
                if (this.isProcessingComplexifiedSpans ||
                    (this.isProcessingTrivia == 0 &&
                    !this.stringAndCommentTextSpans.Contains(oldToken.Span)))
                {
                    return newToken;
                }

                if (this.isRenamingInStrings && newToken.IsKind(SyntaxKind.StringLiteralToken))
                {
                    newToken = RenameInStringLiteral(oldToken, newToken, SyntaxFactory.Literal);
                }

                if (this.isRenamingInComments)
                {
                    if (newToken.IsKind(SyntaxKind.XmlTextLiteralToken))
                    {
                        newToken = RenameInStringLiteral(oldToken, newToken, SyntaxFactory.XmlTextLiteral);
                    }
                    else if (newToken.IsKind(SyntaxKind.IdentifierToken) && newToken.Parent.IsKind(SyntaxKind.XmlName) && newToken.ValueText == this.originalText)
                    {
                        var newIdentifierToken = SyntaxFactory.Identifier(newToken.LeadingTrivia, replacementText, newToken.TrailingTrivia);
                        newToken = newToken.CopyAnnotationsTo(this.renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldToken.Span }));
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
            SyntaxToken token)
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
                        return true;
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
                    var property = await RenameLocationSet.ReferenceProcessing.GetPropertyFromAccessorOrAnOverride(
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
                var token = location.SourceTree.GetTouchingToken(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true);
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
                var token = location.SourceTree.GetTouchingToken(location.SourceSpan.Start, cancellationToken, findInsideTrivia: true);
                var currentTypeParameter = token.Parent;

                foreach (var typeParameter in ((TypeParameterListSyntax)currentTypeParameter.Parent).Parameters)
                {
                    if (typeParameter != currentTypeParameter && token.ValueText == typeParameter.Identifier.ValueText)
                    {
                        conflicts.Add(reverseMappedLocations[typeParameter.Identifier.GetLocation()]);
                    }
                }
            }

            // if the renamed symbol is a type member, it's name should not coflict with a type parameter
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

        private static async Task<ISymbol> GetVBPropertyFromAccessorOrAnOverrideAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
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

        private void AddSymbolSourceSpans(List<Location> conflicts, IEnumerable<ISymbol> symbols, IDictionary<Location, Location> reverseMappedLocations)
        {
            foreach (var symbol in symbols)
            {
                foreach (var location in symbol.Locations)
                {
                    if (location.IsInSource)
                    {
                        conflicts.Add(reverseMappedLocations[location]);
                    }
                }
            }
        }

        public IEnumerable<Location> ComputeImplicitReferenceConflicts(ISymbol renameSymbol, ISymbol renamedSymbol, IEnumerable<ReferenceLocation> implicitReferenceLocations, CancellationToken cancellationToken)
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
                        var token = implicitReferenceLocation.Location.SourceTree.GetTouchingToken(implicitReferenceLocation.Location.SourceSpan.Start, cancellationToken, false);

                        switch (token.Kind())
                        {
                            case SyntaxKind.ForEachKeyword:
                                return SpecializedCollections.SingletonEnumerable<Location>(((ForEachStatementSyntax)token.Parent).Expression.GetLocation());
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
            if (replacementText.EndsWith("Attribute") && replacementText.Length > 9)
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

            // see if there's an enclosing lambda expression
            SyntaxNode possibleLambdaExpression = enclosingStatement == null
                ? token.GetAncestors(n => n is SimpleLambdaExpressionSyntax || n is ParenthesizedLambdaExpressionSyntax).FirstOrDefault()
                : null;

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

            // there seems to be no statement above this one. Let's see if we can at least get an SimpleNameSyntax
            return enclosingStatement ?? enclosingNameMemberCrefOrnull ?? token.GetAncestors(n => n is SimpleNameSyntax).FirstOrDefault();
        }

        #region "Helper Methods"

        public bool IsIdentifierValid(string replacementText, ISyntaxFactsService syntaxFactsService)
        {
            string escapedIdentifier;
            if (replacementText.StartsWith("@"))
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
            return SpeculationAnalyzer.CreateSpeculativeSemanticModelForNode(nodeToSpeculate, (SemanticModel)originalSemanticModel, position, isInNamespaceOrTypeContext);
        }

        #endregion
    }
}
