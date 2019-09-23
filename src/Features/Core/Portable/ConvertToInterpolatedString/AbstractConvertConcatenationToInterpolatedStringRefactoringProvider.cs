// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    /// <summary>
    /// Code refactoring that converts expressions of the form:  a + b + " str " + d + e
    /// into:
    ///     $"{a + b} str {d}{e}".
    /// </summary>
    internal abstract class AbstractConvertConcatenationToInterpolatedStringRefactoringProvider<TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var possibleExpressions = await context.GetRelevantNodesAsync<TExpressionSyntax>().ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // let's take the largest (last) StringConcat we can given current textSpan
            var top = possibleExpressions
                .Where(expr => IsStringConcat(syntaxFacts, expr, semanticModel, cancellationToken))
                .LastOrDefault();

            if (top == null)
            {
                return;
            }

            // Currently we can concatenate only full subtrees. Therefore we can't support arbitrary selection. We could
            // theoretically support selecting the selections that correspond to full sub-trees (e.g. prefixes of 
            // correct length but from UX point of view that it would feel arbitrary). 
            // Thus, we only support selection that takes the whole topmost expression. It breaks some leniency around under-selection
            // but it's the best solution so far.
            if (CodeRefactoringHelpers.IsNodeUnderselected(top, textSpan) ||
                IsStringConcat(syntaxFacts, top.Parent, semanticModel, cancellationToken))
            {
                return;
            }

            // Now walk down the concatenation collecting all the pieces that we are
            // concatenating.
            var pieces = new List<SyntaxNode>();
            CollectPiecesDown(syntaxFacts, pieces, top, semanticModel, cancellationToken);

            var stringLiterals = pieces
                .Where(x => syntaxFacts.IsStringLiteralExpression(x) || syntaxFacts.IsCharacterLiteralExpression(x))
                .ToImmutableArray();

            // If the entire expression is just concatenated strings, then don't offer to
            // make an interpolated string.  The user likely manually split this for
            // readability.
            if (stringLiterals.Length == pieces.Count)
            {
                return;
            }

            var isVerbatimStringLiteral = false;
            if (stringLiterals.Length > 0)
            {

                // Make sure that all the string tokens we're concatenating are the same type
                // of string literal.  i.e. if we have an expression like: @" "" " + " \r\n "
                // then we don't merge this.  We don't want to be munging different types of
                // escape sequences in these strings, so we only support combining the string
                // tokens if they're all the same type.
                var firstStringToken = stringLiterals[0].GetFirstToken();
                isVerbatimStringLiteral = syntaxFacts.IsVerbatimStringLiteral(firstStringToken);
                if (stringLiterals.Any(
                        lit => isVerbatimStringLiteral != syntaxFacts.IsVerbatimStringLiteral(lit.GetFirstToken())))
                {
                    return;
                }
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var interpolatedString = CreateInterpolatedString(document, isVerbatimStringLiteral, pieces);
            context.RegisterRefactoring(
                new MyCodeAction(
                    _ => UpdateDocumentAsync(document, root, top, interpolatedString)),
                top.Span);
        }

        private Task<Document> UpdateDocumentAsync(Document document, SyntaxNode root, SyntaxNode top, SyntaxNode interpolatedString)
        {
            var newRoot = root.ReplaceNode(top, interpolatedString);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        protected SyntaxNode CreateInterpolatedString(
            Document document, bool isVerbatimStringLiteral, List<SyntaxNode> pieces)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var generator = SyntaxGenerator.GetGenerator(document);
            var startToken = CreateInterpolatedStringStartToken(isVerbatimStringLiteral)
                                .WithLeadingTrivia(pieces.First().GetLeadingTrivia());
            var endToken = CreateInterpolatedStringEndToken()
                                .WithTrailingTrivia(pieces.Last().GetTrailingTrivia());

            var content = new List<SyntaxNode>(pieces.Count);
            var previousContentWasStringLiteralExpression = false;
            foreach (var piece in pieces)
            {
                var isCharacterLiteral = syntaxFacts.IsCharacterLiteralExpression(piece);
                var currentContentIsStringOrCharacterLiteral = syntaxFacts.IsStringLiteralExpression(piece) || isCharacterLiteral;
                if (currentContentIsStringOrCharacterLiteral)
                {
                    var text = piece.GetFirstToken().Text;
                    var textWithEscapedBraces = text.Replace("{", "{{").Replace("}", "}}");
                    var textWithoutQuotes = GetTextWithoutQuotes(textWithEscapedBraces, isVerbatimStringLiteral, isCharacterLiteral);
                    if (previousContentWasStringLiteralExpression)
                    {
                        // Last part we added to the content list was also an interpolated-string-text-node.
                        // We need to combine these as the API for creating an interpolated strings
                        // does not expect to be given a list containing non-contiguous string nodes.
                        // Essentially if we combine '"A" + 1 + "B" + "C"' into '$"A{1}BC"' it must be:
                        //      {InterpolatedStringText}{Interpolation}{InterpolatedStringText}
                        // not:
                        //      {InterpolatedStringText}{Interpolation}{InterpolatedStringText}{InterpolatedStringText}
                        var existingInterpolatedStringTextNode = content.Last();
                        var newText = ConcatinateTextToTextNode(generator, existingInterpolatedStringTextNode, textWithoutQuotes);
                        content[content.Count - 1] = newText;
                    }
                    else
                    {
                        // This is either the first string literal we have encountered or it is the most recent one we've seen
                        // after adding an interpolation.  Add a new interpolated-string-text-node to the list.
                        content.Add(generator.InterpolatedStringText(generator.InterpolatedStringTextToken(textWithoutQuotes)));
                    }
                }
                else
                {
                    content.Add(generator.Interpolation(piece.WithoutTrivia()));
                }
                // Update this variable to be true every time we encounter a new string literal expression
                // so we know to concatenate future string literals together if we encounter them.
                previousContentWasStringLiteralExpression = currentContentIsStringOrCharacterLiteral;
            }

            return generator.InterpolatedStringExpression(startToken, content, endToken);
        }

        private static SyntaxNode ConcatinateTextToTextNode(SyntaxGenerator generator, SyntaxNode interpolatedStringTextNode, string textWithoutQuotes)
        {
            var existingText = interpolatedStringTextNode.GetFirstToken().Text;
            var newText = existingText + textWithoutQuotes;
            return generator.InterpolatedStringText(generator.InterpolatedStringTextToken(newText));
        }

        protected abstract string GetTextWithoutQuotes(string text, bool isVerbatimStringLiteral, bool isCharacterLiteral);
        protected abstract SyntaxToken CreateInterpolatedStringStartToken(bool isVerbatimStringLiteral);
        protected abstract SyntaxToken CreateInterpolatedStringEndToken();

        private void CollectPiecesDown(
            ISyntaxFactsService syntaxFacts,
            List<SyntaxNode> pieces,
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (!IsStringConcat(syntaxFacts, node, semanticModel, cancellationToken))
            {
                pieces.Add(node);
                return;
            }

            syntaxFacts.GetPartsOfBinaryExpression(node, out var left, out var right);

            CollectPiecesDown(syntaxFacts, pieces, left, semanticModel, cancellationToken);
            pieces.Add(right);
        }

        private bool IsStringConcat(
            ISyntaxFactsService syntaxFacts, SyntaxNode expression,
            SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!syntaxFacts.IsBinaryExpression(expression))
            {
                return false;
            }

            return semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol is IMethodSymbol method &&
                   method.MethodKind == MethodKind.BuiltinOperator &&
                   method.ContainingType?.SpecialType == SpecialType.System_String &&
                   (method.MetadataName == WellKnownMemberNames.AdditionOperatorName ||
                    method.MetadataName == WellKnownMemberNames.ConcatenateOperatorName);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_interpolated_string, createChangedDocument)
            {
            }
        }
    }
}
