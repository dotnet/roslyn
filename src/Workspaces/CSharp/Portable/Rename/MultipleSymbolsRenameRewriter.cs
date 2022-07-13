// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Rename
{
    internal class MultipleSymbolsRenameRewriter : CSharpAbstractRenameRewriter
    {
        private readonly Dictionary<TextSpan, TextSpanRenameContext> _textSpanToRenameContexts;
        private readonly Dictionary<SymbolKey, RenameSymbolContext> _renameContexts;
        private readonly Dictionary<TextSpan, HashSet<TextSpanRenameContext>> _stringAndCommentRenameContexts;

        public MultipleSymbolsRenameRewriter(
            RenameRewriterParametersNextGen parameters)
            : base(parameters.Document,
                  parameters.OriginalSolution,
                  parameters.ConflictLocationSpans,
                  parameters.SemanticModel,
                  parameters.RenameSpansTracker,
                  parameters.RenameAnnotations,
                  parameters.CancellationToken)
        {
            _renameContexts = GroupRenameContextBySymbolKey(parameters.RenameSymbolContexts);
            _textSpanToRenameContexts = GroupTextRenameContextsByTextSpan(parameters.TokenTextSpanRenameContexts);
            _stringAndCommentRenameContexts = GroupStringAndCommentsTextSpanRenameContexts(parameters.StringAndCommentsTextSpanRenameContexts);
        }

        private bool TryFindSymbolContextForComplexifiedToken(SyntaxToken token, [NotNullWhen(true)] out RenameSymbolContext? renameSymbolContext)
        {
            renameSymbolContext = null;
            if (_isProcessingComplexifiedSpans)
            {
                RoslynDebug.Assert(_speculativeModel != null);
                if (token.Parent == null)
                {
                    return false;
                }

                // Some tokens might be introduced for the complexified node, for example
                // document1:
                // class Bar
                // {
                //     Someothertype Method() => SomeOtherType.Instance;
                // }
                // document2:
                // public class X
                // {
                //    public class SomeOtherType { public static SomeOtherType Instance = new (); }
                // }
                // if we are going to rename 'SomeOtherType' to 'Bar', and 'class X' to 'Y', then when processing document1,
                // 'SomeOtherType' needs to be replaced by its fully qualified name. so here we need to check if the token is linked to other rename contexts.
                var symbol = _speculativeModel.GetSymbolInfo(token.Parent, _cancellationToken).Symbol;
                if (symbol != null
                    && _renameContexts.TryGetValue(symbol.GetSymbolKey(_cancellationToken), out var symbolContext)
                    && token.IsKind(SyntaxKind.IdentifierToken)
                    && token.ValueText == symbolContext.OriginalText)
                {
                    renameSymbolContext = symbolContext;
                    return true;
                }
            }

            renameSymbolContext = null;
            return false;
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            var newTrivia = base.VisitTrivia(trivia);
            if (!trivia.HasStructure && _stringAndCommentRenameContexts.TryGetValue(trivia.Span, out var textSpanRenameContexts))
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
            if (!_isProcessingComplexifiedSpans && TryGetLocationRenameContext(token, out var textSpanRenameContext))
            {
                var symbolContext = textSpanRenameContext.SymbolContext;
                newToken = RenameAndAnnotateAsync(
                    token,
                    newToken,
                    isRenameLocation: true,
                    isOldText: false,
                    _syntaxFactsService.IsVerbatimIdentifier(symbolContext.ReplacementText),
                    symbolContext.ReplacementTextValid,
                    textSpanRenameContext.RenameLocation.IsRenamableAccessor,
                    symbolContext.ReplacementText,
                    symbolContext.OriginalText,
                    symbolContext.RenamableSymbolDeclarationAnnotation,
                    symbolContext.RenamableDeclarationLocation).WaitAndGetResult_CanCallOnBackground(_cancellationToken);
                _invocationExpressionsNeedingConflictChecks.AddRange(token.GetAncestors<InvocationExpressionSyntax>());

                return newToken;
            }

            if (_isProcessingComplexifiedSpans)
            {
                if (TryGetLocationRenameContext(token, out var textSpanRenameContextForComplexifiedToken))
                {
                    return RenameComplexifiedToken(token, newToken, textSpanRenameContextForComplexifiedToken);
                }

                if (TryFindSymbolContextForComplexifiedToken(token, out var renameSymbolContext))
                {
                    return RenameComplexifiedToken(token, newToken, renameSymbolContext);
                }
            }

            return AnnotateNonRenameLocation(token, newToken);
        }

        private bool TryGetLocationRenameContext(SyntaxToken token, [NotNullWhen(true)] out TextSpanRenameContext? textSpanRenameContext)
        {
            if (!_isProcessingComplexifiedSpans)
            {
                return _textSpanToRenameContexts.TryGetValue(token.Span, out textSpanRenameContext);
            }
            else
            {
                if (token.HasAnnotations(AliasAnnotation.Kind))
                {
                    textSpanRenameContext = null;
                    return false;
                }

                if (!token.HasAnnotations(RenameAnnotation.Kind))
                {
                    textSpanRenameContext = null;
                    return false;
                }

                // After a node is complexfied, try to find the original rename context for the given token based on the original span.
                var annotation = _renameAnnotations.GetAnnotations(token).OfType<RenameActionAnnotation>().SingleOrDefault(annotation => annotation.IsRenameLocation);
                if (annotation != null && _textSpanToRenameContexts.TryGetValue(annotation.OriginalSpan, out var originalContext))
                {
                    textSpanRenameContext = originalContext;
                    return true;
                }
            }

            textSpanRenameContext = null;
            return false;
        }

        private SyntaxToken AnnotateNonRenameLocation(SyntaxToken oldToken, SyntaxToken newToken)
        {
            if (!_isProcessingComplexifiedSpans)
            {
                // Handle Alias annotations
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
                        textSpanRenameContext.SymbolContext.OriginalText,
                        textSpanRenameContext.SymbolContext.ReplacementText,
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
                return CSharpAbstractRenameRewriter.RenameToken(
                    token,
                    newToken,
                    prefix: null,
                    suffix: null,
                    _syntaxFactsService.IsVerbatimIdentifier(renameSymbolContext.ReplacementText),
                    renameSymbolContext.OriginalText,
                    renameSymbolContext.ReplacementText,
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

        private SyntaxTrivia RenameInCommentTrivia(SyntaxTrivia trivia, ImmutableSortedDictionary<TextSpan, string> subSpanToReplacementString)
        {
            var originalString = trivia.ToString();
            var replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, trivia.SpanStart, subSpanToReplacementString);
            if (replacedString != originalString)
            {
                var oldSpan = trivia.Span;
                var newTrivia = SyntaxFactory.Comment(replacedString);
                AddModifiedSpan(oldSpan, newTrivia.Span);
                return trivia.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newTrivia, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = oldSpan }));
            }

            return trivia;
        }

        private SyntaxToken RenameInStringLiteral(SyntaxToken oldToken, SyntaxToken newToken, ImmutableSortedDictionary<TextSpan, string> subSpanToReplacementString, Func<SyntaxTriviaList, string, string, SyntaxTriviaList, SyntaxToken> createNewStringLiteral)
        {
            var originalString = newToken.ToString();
            var replacedString = RenameLocations.ReferenceProcessing.ReplaceMatchingSubStrings(originalString, oldToken.SpanStart, subSpanToReplacementString);
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

            if (newToken.IsKind(SyntaxKind.XmlTextLiteralToken))
            {
                newToken = RenameInStringLiteral(token, newToken, subSpanToReplacementText, SyntaxFactory.XmlTextLiteral);
            }
            else if (newToken.IsKind(SyntaxKind.IdentifierToken) && newToken.Parent.IsKind(SyntaxKind.XmlName))
            {
                var matchingContext = textSpanSymbolContexts.OrderByDescending(c => c.Priority).FirstOrDefault(c => c.SymbolContext.OriginalText == newToken.ValueText);
                if (matchingContext != null)
                {
                    var newIdentifierToken = SyntaxFactory.Identifier(newToken.LeadingTrivia, matchingContext.SymbolContext.ReplacementText, newToken.TrailingTrivia);
                    newToken = newToken.CopyAnnotationsTo(_renameAnnotations.WithAdditionalAnnotations(newIdentifierToken, new RenameTokenSimplificationAnnotation() { OriginalTextSpan = token.Span }));
                    AddModifiedSpan(token.Span, newToken.Span);
                }
            }

            return newToken;
        }

        private static Dictionary<SymbolKey, RenameSymbolContext> GroupRenameContextBySymbolKey(
            ImmutableArray<RenameSymbolContext> symbolContexts)
        {
            var renameContexts = new Dictionary<SymbolKey, RenameSymbolContext>();
            foreach (var context in symbolContexts)
            {
                renameContexts[context.RenamedSymbol.GetSymbolKey()] = context;
            }

            return renameContexts;
        }

        private static Dictionary<TextSpan, TextSpanRenameContext> GroupTextRenameContextsByTextSpan(
            ImmutableArray<TextSpanRenameContext> textSpanRenameContexts)
        {
            var textSpanToRenameContext = new Dictionary<TextSpan, TextSpanRenameContext>();
            foreach (var context in textSpanRenameContexts)
            {
                var textSpan = context.RenameLocation.Location.SourceSpan;
                if (!textSpanToRenameContext.ContainsKey(textSpan))
                {
                    textSpanToRenameContext[textSpan] = context;
                }
            }

            return textSpanToRenameContext;
        }

        private static Dictionary<TextSpan, HashSet<TextSpanRenameContext>> GroupStringAndCommentsTextSpanRenameContexts(
            ImmutableArray<TextSpanRenameContext> renameSymbolContexts)
        {
            var textSpanToRenameContexts = new Dictionary<TextSpan, HashSet<TextSpanRenameContext>>();
            foreach (var context in renameSymbolContexts)
            {
                var containingSpan = context.RenameLocation.ContainingLocationForStringOrComment;
                if (textSpanToRenameContexts.TryGetValue(containingSpan, out var existingContexts))
                {
                    existingContexts.Add(context);
                }
                else
                {
                    textSpanToRenameContexts[containingSpan] = new HashSet<TextSpanRenameContext>() { context };
                }
            }

            return textSpanToRenameContexts;
        }

        private static ImmutableHashSet<RenameSymbolContext> GetMatchedContexts(
            IEnumerable<RenameSymbolContext> renameContexts, Func<RenameSymbolContext, bool> predicate)
        {
            using var _ = PooledHashSet<RenameSymbolContext>.GetInstance(out var builder);

            foreach (var renameSymbolContext in renameContexts)
            {
                if (predicate(renameSymbolContext))
                    builder.Add(renameSymbolContext);
            }

            return builder.ToImmutableHashSet();
        }

        private static ImmutableSortedDictionary<TextSpan, string> CreateSubSpanToReplacementTextDictionary(
            HashSet<TextSpanRenameContext> textSpanRenameContexts)
        {
            var subSpanToReplacementTextBuilder = ImmutableSortedDictionary.CreateBuilder<TextSpan, string>();
            foreach (var context in textSpanRenameContexts.OrderByDescending(c => c.Priority))
            {
                var location = context.RenameLocation.Location;
                if (location.IsInSource)
                {
                    var subSpan = location.SourceSpan;

                    // If two symbols tries to rename a same sub span,
                    // e.g.
                    //      // Comment Hello
                    // class Hello
                    // {
                    //    
                    // }
                    // class World
                    // {
                    //    void Hello() { }
                    // }
                    // If try to rename both 'class Hello' to 'Bar' and 'void Hello()' to 'Goo'.
                    // For '// Comment Hello', igore the one with lower priority
                    if (!subSpanToReplacementTextBuilder.ContainsKey(subSpan))
                    {
                        subSpanToReplacementTextBuilder[subSpan] = context.SymbolContext.ReplacementText;
                    }
                }
            }

            return subSpanToReplacementTextBuilder.ToImmutable();
        }
    }
}
