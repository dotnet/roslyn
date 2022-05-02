// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InvertConditional
{
    internal abstract class AbstractInvertConditionalCodeRefactoringProvider<TConditionalExpressionSyntax>
        : SyntaxEditorBasedCodeRefactoringProvider
        where TConditionalExpressionSyntax : SyntaxNode
    {
        protected abstract bool ShouldOffer(TConditionalExpressionSyntax conditional);

        protected override ImmutableArray<CodeFixes.FixAllScope> SupportedFixAllScopes => AllFixAllScopes;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var conditional = await FindConditionalAsync(document, span, cancellationToken).ConfigureAwait(false);

            if (conditional == null || !ShouldOffer(conditional))
            {
                return;
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Invert_conditional,
                    c => InvertConditionalAsync(document, conditional, c),
                    nameof(FeaturesResources.Invert_conditional)),
                conditional.Span);
        }

        private static async Task<TConditionalExpressionSyntax?> FindConditionalAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
            => await document.TryGetRelevantNodeAsync<TConditionalExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);

        protected override async Task FixAllAsync(Document document, ImmutableArray<TextSpan> fixAllSpans, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var originalRoot = editor.OriginalRoot;

            // Get all conditional nodes in the given fixAllSpans.
            var conditionals = originalRoot.DescendantNodes().OfType<TConditionalExpressionSyntax>()
                .Where(node => fixAllSpans.Any(fixAllSpan => fixAllSpan.IntersectsWith(node.Span)));

            // We're going to be continually editing this tree. Track all the nodes we
            // care about so we can find them across each edit.
            document = document.WithSyntaxRoot(originalRoot.TrackNodes(conditionals));

            // Process the conditional expressions in reverse so the nested conditionals are processed before the outer ones.
            foreach (var originalConditional in conditionals.Reverse())
            {
                // Only process conditionals fully within fixAllSpan
                if (!fixAllSpans.Any(fixAllSpan => fixAllSpan.Contains(originalConditional.Span)))
                    continue;

                var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var currentConditional = currentRoot.GetCurrentNodes(originalConditional).Single();
                document = await InvertConditionalAsync(document, currentConditional, cancellationToken).ConfigureAwait(false);
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(originalRoot, root);
        }

        private static async Task<Document> InvertConditionalAsync(
            Document document, TConditionalExpressionSyntax conditional, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

            editor.Generator.SyntaxFacts.GetPartsOfConditionalExpression(conditional,
                out var condition, out var whenTrue, out var whenFalse);

            editor.ReplaceNode(condition, editor.Generator.Negate(editor.Generator.SyntaxGeneratorInternal, condition, semanticModel, cancellationToken));
            editor.ReplaceNode(whenTrue, whenFalse.WithTriviaFrom(whenTrue));
            editor.ReplaceNode(whenFalse, whenTrue.WithTriviaFrom(whenFalse));

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }
    }
}
