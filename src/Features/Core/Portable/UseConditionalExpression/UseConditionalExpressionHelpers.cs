// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionHelpers
    {
        public static readonly SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public static async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            Func<Document, Diagnostic, SyntaxEditor, CancellationToken, Task> fixOneAsync,
            IFormattingRule multiLineFormattingRule, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Defer to our callback to actually make the edits for each diagnostic. In turn, it
            // will return 'true' if it made a multi-line conditional expression. In that case,
            // we'll need to explicitly format this node so we can get our special multi-line
            // formatting in VB and C#.
            var nestedEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            foreach (var diagnostic in diagnostics)
            {
                await fixOneAsync(
                    document, diagnostic, nestedEditor, cancellationToken).ConfigureAwait(false);
            }

            var changedRoot = nestedEditor.GetChangedRoot();
            // Get the language specific rule for formatting this construct and call into the
            // formatted to explicitly format things.  Note: all we will format is the new
            // conditional expression as that's the only node that has the appropriate
            // annotation on it.
            var rules = new List<IFormattingRule> { multiLineFormattingRule };

            var formattedRoot = await Formatter.FormatAsync(changedRoot,
                SpecializedFormattingAnnotation,
                document.Project.Solution.Workspace,
                await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                rules, cancellationToken).ConfigureAwait(false);
            changedRoot = formattedRoot;

            editor.ReplaceNode(root, changedRoot);
        }

        public static bool CanConvert(
            ISyntaxFactsService syntaxFacts, IConditionalOperation ifOperation, 
            IOperation whenTrue, IOperation whenFalse)
        {
            // Will likely screw things up if the if directive spans any preprocessor directives.
            // So do not offer for now.
            if (syntaxFacts.SpansPreprocessorDirective(ifOperation.Syntax))
            {
                return false;
            }

            // User may have comments on the when-true/when-false statements.  These statements can
            // be very important. Often they indicate why the true/false branches are important in
            // the first place.  We don't have any place to put these, so we don't offer here.
            if (HasLeadingRegularComments(syntaxFacts, whenTrue.Syntax) ||
                HasLeadingRegularComments(syntaxFacts, whenFalse.Syntax))
            {
                return false;
            }

            return true;
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
        /// Checks if we should wrap the conditional expression over multiple lines.
        /// </summary>
        public static async Task<bool> MakeMultiLineAsync(
            Document document, SyntaxNode condition, SyntaxNode trueSyntax, SyntaxNode falseSyntax, 
            IEnumerable<SyntaxTrivia> trueTrailingTrivia, IEnumerable<SyntaxTrivia> falseTrailingTrivia,
            CancellationToken cancellationToken)
        {
            // If there is trivia on the true/false statement then make this multiline so that
            // the trailing trivia will go at the end of each part of the conditional.
            if (trueTrailingTrivia != null || falseTrailingTrivia != null)
            {
                return true;
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (!sourceText.AreOnSameLine(condition.GetFirstToken(), condition.GetLastToken()) || 
                !sourceText.AreOnSameLine(trueSyntax.GetFirstToken(), trueSyntax.GetLastToken()) ||
                !sourceText.AreOnSameLine(falseSyntax.GetFirstToken(), falseSyntax.GetLastToken()))
            {
                return true;
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var wrappingLength = options.GetOption(UseConditionalExpressionOptions.ConditionalExpressionWrappingLength);
            if (condition.Span.Length + trueSyntax.Span.Length + falseSyntax.Span.Length > wrappingLength)
            {
                return true;
            }

            return false;
        }

        public static IEnumerable<SyntaxTrivia> GetTrailingComments(
            ISyntaxFactsService syntaxFacts, IOperation statement)
        {
            var trailingTrivia = statement.Syntax.GetTrailingTrivia();
            if (!trailingTrivia.Any(t => syntaxFacts.IsRegularComment(t)))
            {
                return null;
            }

            return trailingTrivia.Where(t => syntaxFacts.IsWhitespaceTrivia(t) || syntaxFacts.IsRegularComment(t));
        }

        public static SyntaxNode CastValueIfNecessary(
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
            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepTrailingTrivia;
            }

            return removeOptions;
        }

        private static bool HasLeadingRegularComments(ISyntaxFactsService syntaxFacts, SyntaxNode syntax)
            => HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia());

        private static bool HasRegularCommentTrivia(ISyntaxFactsService syntaxFacts, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (syntaxFacts.IsRegularComment(trivia))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
