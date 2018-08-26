// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    // Code for the CodeRefactoringProvider ("Refactoring") portion of the feature.

    internal partial class UseExpressionBodyForLambdaCodeStyleProvider
    {
        protected override async Task<ImmutableArray<CodeAction>> ComputeOpposingRefactoringsWhenAnalyzerActiveAsync(
            Document document, TextSpan span, ExpressionBodyPreference option, CancellationToken cancellationToken)
        {
            if (option == ExpressionBodyPreference.Never)
            {
                // the user wants block-bodies (and the analyzer will be trying to enforce that). So
                // the reverse of this is that we want to offer the refactoring to convert a
                // block-body to an expression-body whenever possible.
                return await ComputeRefactoringsAsync(
                    document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);
            }
            else if (option == ExpressionBodyPreference.WhenPossible)
            {
                // the user likes expression-bodies whenever possible, and the analyzer will be
                // trying to enforce that.  So the reverse of this is that we want to offer the
                // refactoring to convert an expression-body to a block-body whenever possible.
                return await ComputeRefactoringsAsync(
                    document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);
            }
            else if (option == ExpressionBodyPreference.WhenOnSingleLine)
            {
                // the user likes expression-bodies *if* the body would be on a single line. this
                // means if we hit an block-body with an expression on a single line, then the
                // analyzer will handle it for us.

                // So we need to handle the cases of either hitting an expression-body and wanting
                // to convert it to a block-body *or* hitting an block-body over *multiple* lines and
                // wanting to offer to convert to an expression-body.

                // Always offer to convert an expression to a block since the analyzer will never
                // offer that. For this option setting.
                var useBlockRefactorings = await ComputeRefactoringsAsync(
                    document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);

                var whenOnSingleLineRefactorings = await ComputeRefactoringsAsync(
                    document, span, ExpressionBodyPreference.WhenOnSingleLine, cancellationToken).ConfigureAwait(false);
                if (whenOnSingleLineRefactorings.Length > 0)
                {
                    // this block lambda would be converted to an expression lambda based on the
                    // analyzer alone.  So we don't want to offer that as a refactoring ourselves.
                    return useBlockRefactorings;
                }

                // The lambda block statement wasn't on a single line.  So the analyzer would
                // not offer to convert it to an expression body.  So we should can offer that
                // as a refactoring if possible.
                var whenPossibleRefactorings = await ComputeRefactoringsAsync(
                    document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);
                return useBlockRefactorings.AddRange(whenPossibleRefactorings);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(option);
            }
        }

        protected override async Task<ImmutableArray<CodeAction>> ComputeAllRefactoringsWhenAnalyzerInactiveAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            // If the analyzer is inactive, then we want to offer refactorings in any viable
            // direction.  So we want to offer to convert expression-bodies to block-bodies, and
            // vice-versa if applicable.

            var toExpressionBodyRefactorings = await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);

            var toBlockBodyRefactorings = await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);

            return toExpressionBodyRefactorings.AddRange(toBlockBodyRefactorings);
        }

        private async Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
            Document document, TextSpan span, ExpressionBodyPreference option, CancellationToken cancellationToken)
        {
            if (span.Length > 0)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var position = span.Start;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var lambdaNode = root.FindToken(position).Parent.FirstAncestorOrSelf<LambdaExpressionSyntax>();
            if (lambdaNode == null)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var result = ArrayBuilder<CodeAction>.GetInstance();
            if (CanOfferUseExpressionBody(option, lambdaNode))
            {
                result.Add(new MyCodeAction(
                    UseExpressionBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, lambdaNode, c)));
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (CanOfferUseBlockBody(semanticModel, option, lambdaNode, cancellationToken))
            {
                result.Add(new MyCodeAction(
                    UseBlockBodyTitle.ToString(),
                    c => UpdateDocumentAsync(
                        document, root, lambdaNode, c)));
            }

            return result.ToImmutableAndFree();
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // We're only replacing a single declaration in the refactoring.  So pass 'declaration'
            // as both the 'original' and 'current' declaration.
            var updatedDeclaration = Update(semanticModel, declaration, declaration);

            var newRoot = root.ReplaceNode(declaration, updatedDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
