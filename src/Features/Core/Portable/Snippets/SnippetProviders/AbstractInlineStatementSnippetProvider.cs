// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    /// <summary>
    /// Base class for snippets, that can be both executed as normal statement snippets
    /// or constructed from a member access expression when accessing members of a specific type
    /// </summary>
    internal abstract class AbstractInlineStatementSnippetProvider : AbstractStatementSnippetProvider
    {
        /// <summary>
        /// Tells if accessing type of a member access expression is valid for that snippet
        /// </summary>
        /// <param name="type">Type of right-hand side of a member access expression</param>
        protected abstract bool IsValidAccessingType(ITypeSymbol type);

        /// <summary>
        /// Generate statement node
        /// </summary>
        /// <param name="inlineExpression">Right-hand side of a member access expression. <see langword="null"/> if snippet is executed in normal statement context</param>
        protected abstract SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, SyntaxNode? inlineExpression);

        /// <summary>
        /// Tells whether the original snippet was constructed from member access expression.
        /// Can be used by snippet providers to not mark that expression as a placeholder
        /// </summary>
        protected bool ConstructedFromInlineExpression { get; private set; }

        protected sealed override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            var targetToken = syntaxContext.TargetToken;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (TryGetInlineExpression(targetToken, syntaxFacts, out var expression))
            {
                var accessingType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;

                if (accessingType is not null)
                    return IsValidAccessingType(accessingType);
            }

            return await base.IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false);
        }

        protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            var targetToken = syntaxContext.TargetToken;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            _ = TryGetInlineExpression(targetToken, syntaxFacts, out var inlineExpression);

            var statement = GenerateStatement(SyntaxGenerator.GetGenerator(document), syntaxContext, inlineExpression);

            if (inlineExpression is not null)
            {
                ConstructedFromInlineExpression = true;

                // We need to trim leading trivia of `inlineExpression` to remove unwanted comments and so on.
                // Need to do it after the statement was generated, so snippet can ask semantic questions about given `inlineExpression`.
                // But we cannot just use `ReplaceNode` with `inlineExpression` as argument, since corresponding node now belongs to a different syntax tree.
                // So we manually walk statement's descendant nodes to find equivalent one to `inlineExpression` we have
                var inlineExpressionInStatement = FindInlineExpressionInGeneratedStatement(statement, inlineExpression);
                if (inlineExpressionInStatement is not null)
                    statement = statement.ReplaceNode(inlineExpressionInStatement, inlineExpressionInStatement.WithoutLeadingTrivia());
            }

            return new TextChange(inlineExpression?.Parent?.Span ?? TextSpan.FromBounds(position, position), statement.ToFullString());

            static SyntaxNode? FindInlineExpressionInGeneratedStatement(SyntaxNode statement, SyntaxNode inlineExpression)
            {
                foreach (var node in statement.DescendantNodes())
                {
                    if (node.IsEquivalentTo(inlineExpression))
                        return node;
                }

                // Generally we shouldn't appear here in a normal flow.
                // But it is theoretically possible if:
                // 1. Snippet didn't use `inlineExpression`. Most likely something it wrong with its implementation. Or it is WIP and not everything is wired up at this point
                // 2. Snippet modified `inlineExpression` when used it. In this case responsibility of handling trivia lies on the snippet, since we don't have any means to find it
                return null;
            }
        }

        protected sealed override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, Func<SyntaxNode?, bool> isCorrectContainer)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
            return closestNode.FirstAncestorOrSelf<SyntaxNode>(isCorrectContainer);
        }

        private static bool TryGetInlineExpression(SyntaxToken targetToken, ISyntaxFactsService syntaxFacts, [NotNullWhen(true)] out SyntaxNode? expression)
        {
            expression = null;

            var parentNode = targetToken.Parent;

            if (syntaxFacts.IsMemberAccessExpression(parentNode) &&
                syntaxFacts.IsExpressionStatement(parentNode?.Parent))
            {
                expression = syntaxFacts.GetExpressionOfMemberAccessExpression(parentNode)!;
                return true;
            }

            return false;
        }
    }
}
