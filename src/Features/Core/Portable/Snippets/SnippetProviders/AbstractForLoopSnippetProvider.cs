// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractForLoopSnippetProvider : AbstractSnippetProvider
    {
        public override string SnippetIdentifier => "for";

        public override string SnippetDisplayName => FeaturesResources.Write_to_the_console;
        protected abstract SyntaxNode CreateForLoopStatementSyntax(SyntaxGenerator generator);

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            return syntaxContext.IsStatementContext || syntaxContext.IsGlobalStatementContext;
        }

        protected override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var snippetTextChange = await GenerateSnippetTextChangeAsync(document, position, cancellationToken).ConfigureAwait(false);
            return ImmutableArray.Create(snippetTextChange);
        }

        private async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var options = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (options != null)
            {
                options.
            }
            var generator = SyntaxGenerator.GetGenerator(document);
            var forLoopSyntax = CreateForLoopStatementSyntax(generator);
            return new TextChange(TextSpan.FromBounds(position, position), forLoopSyntax.ToFullString());
        }

        /// <summary>
        /// Tries to get the location after the open parentheses in the argument list.
        /// If it can't, then we default to the end of the snippet's span.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
        {
            var invocationExpression = caretTarget.DescendantNodes().Where(syntaxFacts.IsInvocationExpression).FirstOrDefault();
            if (invocationExpression is null)
            {
                return caretTarget.Span.End;
            }

            var argumentListNode = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);
            if (argumentListNode is null)
            {
                return caretTarget.Span.End;
            }

            syntaxFacts.GetPartsOfArgumentList(argumentListNode, out var openParenToken, out _, out _);
            return openParenToken.Span.End;
        }

        protected override async Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document,
            SyntaxAnnotation findSnippetAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position, syntaxFacts);
            if (snippetExpressionNode is null)
            {
                return root;
            }

            var consoleSymbol = await GetSymbolFromMetaDataNameAsync(document, cancellationToken).ConfigureAwait(false);

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, SymbolAnnotation.Create(consoleSymbol!), Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            return ImmutableArray<SnippetPlaceholder>.Empty;
        }

        private static SyntaxToken? GetOpenParenToken(SyntaxNode node, ISyntaxFacts syntaxFacts)
        {
            var invocationExpression = node.DescendantNodes().Where(syntaxFacts.IsInvocationExpression).FirstOrDefault();
            if (invocationExpression is null)
            {
                return null;
            }

            var argumentListNode = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);
            if (argumentListNode is null)
            {
                return null;
            }

            syntaxFacts.GetPartsOfArgumentList(argumentListNode, out var openParenToken, out _, out _);

            return openParenToken;
        }

        protected override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, ISyntaxFacts syntaxFacts)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
            var nearestExpressionStatement = closestNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsExpressionStatement);
            if (nearestExpressionStatement is null)
            {
                return null;
            }

            // Checking to see if that expression statement that we found is
            // starting at the same position as the position we inserted
            // the Console WriteLine expression statement.
            if (nearestExpressionStatement.SpanStart != position)
            {
                return null;
            }

            return nearestExpressionStatement;
        }

        private static async Task<INamedTypeSymbol?> GetSymbolFromMetaDataNameAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = compilation.GetBestTypeByMetadataName(typeof(Console).FullName!);
            return symbol;
        }
    }
}

