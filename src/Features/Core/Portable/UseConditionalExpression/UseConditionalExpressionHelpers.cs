// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionHelpers
    {
        public static readonly SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public static async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, 
            Func<Document, Diagnostic, SyntaxEditor, CancellationToken, Task<bool>> fixOneAsync,
            IFormattingRule multiLineFormattingRule, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Defer to our callback to actually make the edits for each diagnostic. In turn, it
            // will return 'true' if it made a multi-line conditional expression. In that case,
            // we'll need to explicitly format this node so we can get our special multi-line
            // formatting in VB and C#.
            var nestedEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var needsFormatting = false;
            foreach (var diagnostic in diagnostics)
            {
                needsFormatting |= await fixOneAsync(
                    document, diagnostic, nestedEditor, cancellationToken).ConfigureAwait(false);
            }

            var changedRoot = nestedEditor.GetChangedRoot();
            if (needsFormatting)
            {
                // Get the language specific rule for formatting this construct and call into the
                // formatted to explicitly format things.  Note: all we will format is the new
                // conditional expression as that's the only node that has the appropriate
                // annotation on it.
                var rules = new List<IFormattingRule> { multiLineFormattingRule };
                rules.AddRange(Formatter.GetDefaultFormattingRules(document));

                var formattedRoot = await Formatter.FormatAsync(changedRoot,
                    SpecializedFormattingAnnotation,
                    document.Project.Solution.Workspace,
                    await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                    rules, cancellationToken).ConfigureAwait(false);
                changedRoot = formattedRoot;
            }

            editor.ReplaceNode(root, changedRoot);
        }

        /// <summary>
        /// Will unwrap a block with a single statement in it to just that block.  Used so we can
        /// support both ```if (expr) { statement }``` and ```if (expr) statement```
        /// </summary>
        public static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;

        public static IOperation UnwrapImplicitConversion(IOperation value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;

        /// <summary>
        /// Checks if either the whenTrue or whenFalse parts of the conditional are multi-line.  If
        /// so, we'll specially format the new conditional expression so it looks decent.
        /// </summary>
        private static async Task<bool> IsMultiLineAsync(
            Document document, SyntaxNode trueSyntax, SyntaxNode falseSyntax, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return !sourceText.AreOnSameLine(trueSyntax.GetFirstToken(), trueSyntax.GetLastToken()) ||
                   !sourceText.AreOnSameLine(falseSyntax.GetFirstToken(), falseSyntax.GetLastToken());
        }

        /// <summary>
        /// Helper to create a conditional expression out of two original IOperation values
        /// corresponding to the whenTrue and whenFalse parts. The helper will add the appropriate
        /// annotations and casts to ensure that the conditional expression preserves semantics, but
        /// is also properly simplified and formatted.
        /// </summary>
        public static async Task<(TExpressionSyntax, bool isMultiLine)> CreateConditionalExpressionAsync<TExpressionSyntax>(
            Document document, IConditionalOperation ifOperation, 
            IOperation trueValue, IOperation falseValue, CancellationToken cancellationToken)
            where TExpressionSyntax : SyntaxNode
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var conditionalExpression = (TExpressionSyntax)generator.ConditionalExpression(
                ifOperation.Condition.Syntax.WithoutTrivia(),
                CastValueIfNecessary(generator, trueValue),
                CastValueIfNecessary(generator, falseValue));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);
            var isMultiLine = await IsMultiLineAsync(
                document, trueValue.Syntax, falseValue.Syntax, cancellationToken).ConfigureAwait(false);
            if (isMultiLine)
            {
                conditionalExpression = conditionalExpression.WithAdditionalAnnotations(
                    SpecializedFormattingAnnotation);
            }

            return (conditionalExpression, isMultiLine);
        }

        private static SyntaxNode CastValueIfNecessary(
            SyntaxGenerator generator, IOperation value)
        {
            var sourceSyntax = value.Syntax.WithoutTrivia();

            // If there was an implicit conversion generated by the compiler, then convert that to an
            // explicit conversion inside the condition.  This is needed as there is no type
            // inference in conditional expressions, so we need to ensure that the same conversions
            // that were occurring previously still occur after conversion. Note: the simplifier
            // will remove any of these casts that are unnecessary.
            if (value is IConversionOperation conversion &&
                conversion.IsImplicit &&
                conversion.Type != null &&
                conversion.Type.TypeKind != TypeKind.Error)
            {
                // Note we only add the cast if the source had no type (like the null literal), or a
                // non-error type itself.  We don't want to insert lots of casts in error code.
                if (conversion.Operand.Type == null || conversion.Operand.Type.TypeKind != TypeKind.Error)
                {
                    return generator.CastExpression(conversion.Type, sourceSyntax);
                }
            }

            return sourceSyntax;
        }

        public static SyntaxRemoveOptions GetRemoveOptions(
            ISyntaxFactsService syntaxFacts, SyntaxNode syntax)
        {
            var removeOptions = SyntaxGenerator.DefaultRemoveOptions;
            if (HasNonWhitespaceOrEndOfLineTrivia(syntaxFacts, syntax.GetLeadingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            if (HasNonWhitespaceOrEndOfLineTrivia(syntaxFacts, syntax.GetTrailingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepTrailingTrivia;
            }

            return removeOptions;
        }

        private static bool HasNonWhitespaceOrEndOfLineTrivia(ISyntaxFactsService syntaxFacts, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (!syntaxFacts.IsWhitespaceTrivia(trivia) && !syntaxFacts.IsEndOfLineTrivia(trivia))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
