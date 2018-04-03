// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionCodeFixProvider<TConditionalExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TConditionalExpressionSyntax : SyntaxNode
    {
        protected abstract TConditionalExpressionSyntax AddTriviaTo(
            TConditionalExpressionSyntax conditionalExpression, IEnumerable<SyntaxTrivia> trueTrivia, IEnumerable<SyntaxTrivia> falseTrivia);

        /// <summary>
        /// Helper to create a conditional expression out of two original IOperation values
        /// corresponding to the whenTrue and whenFalse parts. The helper will add the appropriate
        /// annotations and casts to ensure that the conditional expression preserves semantics, but
        /// is also properly simplified and formatted.
        /// </summary>
        protected async Task<(TConditionalExpressionSyntax, bool makeMultiLine)> CreateConditionalExpressionAsync(
            Document document, IConditionalOperation ifOperation,
            IOperation trueStatement, IOperation falseStatement,
            IOperation trueValue, IOperation falseValue, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var trueTrailingComments = GetTrailingComments(syntaxFacts, trueStatement);
            var falseTrailingComments = GetTrailingComments(syntaxFacts, falseStatement);
             
            var condition = ifOperation.Condition.Syntax.WithoutTrivia();
            var conditionalExpression = (TConditionalExpressionSyntax)generator.ConditionalExpression(
                condition,
                CastValueIfNecessary(generator, trueValue),
                CastValueIfNecessary(generator, falseValue));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);
            var makeMultiLine = await MakeMultiLineAsync(
                document, condition, trueValue.Syntax, falseValue.Syntax,
                trueTrailingComments, falseTrailingComments, cancellationToken).ConfigureAwait(false);
            if (makeMultiLine)
            {
                conditionalExpression = conditionalExpression.WithAdditionalAnnotations(
                    SpecializedFormattingAnnotation);
            }

            conditionalExpression = AddTriviaTo(
                conditionalExpression, trueTrailingComments, falseTrailingComments);

            return (conditionalExpression, makeMultiLine);
        }
    }
}
