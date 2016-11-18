// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Code refactoring that converts expresions of the form:  a + b + " str " + d + e
    /// into:
    ///     $"{a + b} str {d}{e}".
    /// </summary>
    internal abstract class AbstractConvertConcatenationToInterpolatedStringRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // Currently only supported if there is no selection.  We could consider relaxing
            // this if the selection is of a string concatenation expression.
            if (context.Span.Length > 0)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var document = context.Document;
            var position = context.Span.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            // Cursor has to at least be touching a string token.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (!token.Span.IntersectsWith(position) || 
                !syntaxFacts.IsStringLiteral(token))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // The string literal has to at least be contained in a concatenation of some form.
            // i.e.  "foo" + a      or     a + "foo".  However, those concats could be in larger
            // concats as well.  Walk to the top of that entire chain.

            var literalExpression = token.Parent;
            var top = literalExpression;
            while (IsStringConcat(syntaxFacts, top.Parent, semanticModel, cancellationToken))
            {
                top = top.Parent;
            }

            if (top == literalExpression)
            {
                // We weren't in a concatenation at all.
                return;
            }

            // Now walk down the concatenation collecting all the pieces that we are 
            // concatenating.
            var pieces = new List<SyntaxNode>();
            CollectPiecesDown(syntaxFacts, pieces, top, semanticModel, cancellationToken);

            // If the entire expression is just concatenated strings, then don't offer to
            // make an interpolated string.  The user likely manually split this for 
            // readability.
            if (pieces.All(syntaxFacts.IsStringLiteralExpression))
            {
                return;
            }

            // Make sure that all the string tokens we're concatenating are the same type
            // of string literal.  i.e. if we have an expression like: @" "" " + " \r\n "
            // then we don't merge this.  We don't want to be munging differnet types of
            // escape sequences in these strings, so we only support combining the string
            // tokens if they're all teh same type.
            var firstStringToken = pieces.First(syntaxFacts.IsStringLiteralExpression).GetFirstToken();
            var isVerbatimStringLiteral = syntaxFacts.IsVerbatimStringLiteral(firstStringToken);
            if (pieces.Where(syntaxFacts.IsStringLiteralExpression).Any(
                    lit => isVerbatimStringLiteral != syntaxFacts.IsVerbatimStringLiteral(lit.GetFirstToken())))
            {
                return;
            }

            var interpolatedString = CreateInterpolatedString(document, isVerbatimStringLiteral, pieces);
            context.RegisterRefactoring(new MyCodeAction(
                c => UpdateDocumentAsync(document, root, top, interpolatedString, c)));
        }

        private Task<Document> UpdateDocumentAsync(Document document, SyntaxNode root, SyntaxNode top, SyntaxNode interpolatedString, CancellationToken c)
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

            var content = new List<SyntaxNode>();
            foreach (var piece in pieces)
            {
                if (syntaxFacts.IsStringLiteralExpression(piece))
                {
                    var text = piece.GetFirstToken().Text;
                    var textWithoutQuotes = GetTextWithoutQuotes(text, isVerbatimStringLiteral);
                    content.Add(generator.InterpolatedStringText(
                        generator.InterpolatedStringTextToken(textWithoutQuotes)));
                }
                else
                {
                    content.Add(generator.Interpolation(piece.WithoutTrivia()));
                }
            }

            return generator.InterpolatedStringExpression(startToken, content, endToken);
        }

        protected abstract string GetTextWithoutQuotes(string text, bool isVerbatimStringLiteral);
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

            SyntaxNode left, right;
            syntaxFacts.GetPartsOfBinaryExpression(node, out left, out right);

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

            var method = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol as IMethodSymbol;
            return method?.MethodKind == MethodKind.BuiltinOperator &&
                   method.ContainingType.SpecialType == SpecialType.System_String &&
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