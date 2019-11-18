// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionCodeFixProvider<
        TStatementSyntax,
        TIfStatementSyntax,
        TExpressionSyntax,
        TConditionalExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TStatementSyntax : SyntaxNode
        where TIfStatementSyntax : TStatementSyntax
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
    {
        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected abstract AbstractFormattingRule GetMultiLineFormattingRule();
        protected abstract TStatementSyntax WrapWithBlockIfAppropriate(TIfStatementSyntax ifStatement, TStatementSyntax statement);

        protected abstract Task FixOneAsync(
            Document document, Diagnostic diagnostic,
            SyntaxEditor editor, CancellationToken cancellationToken);

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Defer to our callback to actually make the edits for each diagnostic. In turn, it
            // will return 'true' if it made a multi-line conditional expression. In that case,
            // we'll need to explicitly format this node so we can get our special multi-line
            // formatting in VB and C#.
            var nestedEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, diagnostic, nestedEditor, cancellationToken).ConfigureAwait(false);
            }

            var changedRoot = nestedEditor.GetChangedRoot();
            // Get the language specific rule for formatting this construct and call into the
            // formatted to explicitly format things.  Note: all we will format is the new
            // conditional expression as that's the only node that has the appropriate
            // annotation on it.
            var rules = new List<AbstractFormattingRule> { GetMultiLineFormattingRule() };

            var formattedRoot = Formatter.Format(changedRoot,
                SpecializedFormattingAnnotation,
                document.Project.Solution.Workspace,
                await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                rules, cancellationToken);
            changedRoot = formattedRoot;

            editor.ReplaceNode(root, changedRoot);
        }

        /// <summary>
        /// Helper to create a conditional expression out of two original IOperation values
        /// corresponding to the whenTrue and whenFalse parts. The helper will add the appropriate
        /// annotations and casts to ensure that the conditional expression preserves semantics, but
        /// is also properly simplified and formatted.
        /// </summary>
        protected async Task<TExpressionSyntax> CreateConditionalExpressionAsync(
            Document document, IConditionalOperation ifOperation,
            IOperation trueStatement, IOperation falseStatement,
            IOperation trueValue, IOperation falseValue,
            bool isRef, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var condition = ifOperation.Condition.Syntax;
            if (!isRef)
            {
                // If we are going to generate "expr ? true : false" then just generate "expr"
                // instead.
                if (IsBooleanLiteral(trueValue, true) && IsBooleanLiteral(falseValue, false))
                {
                    return (TExpressionSyntax)condition.WithoutTrivia();
                }

                // If we are going to generate "expr ? false : true" then just generate "!expr"
                // instead.
                if (IsBooleanLiteral(trueValue, false) && IsBooleanLiteral(falseValue, true))
                {
                    return (TExpressionSyntax)generator.Negate(
                        condition, semanticModel, cancellationToken).WithoutTrivia();
                }
            }

            var conditionalExpression = (TConditionalExpressionSyntax)generator.ConditionalExpression(
                condition.WithoutTrivia(),
                MakeRef(generator, isRef, CastValueIfNecessary(generator, trueValue)),
                MakeRef(generator, isRef, CastValueIfNecessary(generator, falseValue)));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);
            var makeMultiLine = await MakeMultiLineAsync(
                document, condition,
                trueValue.Syntax, falseValue.Syntax, cancellationToken).ConfigureAwait(false);
            if (makeMultiLine)
            {
                conditionalExpression = conditionalExpression.WithAdditionalAnnotations(
                    SpecializedFormattingAnnotation);
            }

            return MakeRef(generator, isRef, conditionalExpression);
        }

        private static bool IsBooleanLiteral(IOperation trueValue, bool val)
        {
            if (trueValue is ILiteralOperation)
            {
                var constant = trueValue.ConstantValue;
                return constant is { HasValue: true, Value: bool b } && b == val;
            }

            return false;
        }

        private TExpressionSyntax MakeRef(SyntaxGenerator generator, bool isRef, TExpressionSyntax syntaxNode)
            => isRef ? (TExpressionSyntax)generator.RefExpression(syntaxNode) : syntaxNode;

        /// <summary>
        /// Checks if we should wrap the conditional expression over multiple lines.
        /// </summary>
        private static async Task<bool> MakeMultiLineAsync(
            Document document, SyntaxNode condition, SyntaxNode trueSyntax, SyntaxNode falseSyntax,
            CancellationToken cancellationToken)
        {
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

        private static TExpressionSyntax CastValueIfNecessary(
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
                    return (TExpressionSyntax)generator.CastExpression(conversion.Type, sourceSyntax);
                }
            }

            return (TExpressionSyntax)sourceSyntax;
        }
    }
}
