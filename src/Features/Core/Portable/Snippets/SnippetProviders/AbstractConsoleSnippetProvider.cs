// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractConsoleSnippetProvider : AbstractStatementSnippetProvider
    {
        protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

        public override string Identifier => "cw";

        public override string Description => FeaturesResources.console_writeline;

        public override ImmutableArray<string> AdditionalFilterTexts { get; } = ImmutableArray.Create("WriteLine");

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var consoleSymbol = await GetSymbolFromMetaDataNameAsync(document, cancellationToken).ConfigureAwait(false);
            if (consoleSymbol is null)
            {
                return false;
            }

            return await base.IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false);
        }

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsExpressionStatement;
        }

        protected override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);

            // We know symbol is not null at this point since it was checked when determining
            // if we are in a valid location to insert the snippet.
            var declaration = GetAsyncSupportingDeclaration(token);
            var isAsync = declaration is not null && generator.GetModifiers(declaration).IsAsync;

            var invocation = isAsync
                ? generator.AwaitExpression(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName(nameof(Console)), generator.IdentifierName(nameof(Console.Out))), nameof(Console.Out.WriteLineAsync))))
                : generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName(nameof(Console)), nameof(Console.WriteLine)));
            var expressionStatement = generator.ExpressionStatement(invocation);

            // Need to normalize the whitespace for the asynchronous case because it doesn't insert a space following the await
            return new TextChange(TextSpan.FromBounds(position, position), expressionStatement.NormalizeWhitespace().ToFullString());
        }

        /// <summary>
        /// Tries to get the location after the open parentheses in the argument list.
        /// If it can't, then we default to the end of the snippet's span.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
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
            var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position, syntaxFacts.IsExpressionStatement);
            Contract.ThrowIfNull(snippetExpressionNode);

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

        private static async Task<INamedTypeSymbol?> GetSymbolFromMetaDataNameAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = compilation.GetBestTypeByMetadataName(typeof(Console).FullName!);
            return symbol;
        }

        protected override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, Func<SyntaxNode?, bool> isCorrectContainer)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
            var nearestExpressionStatement = closestNode.FirstAncestorOrSelf<SyntaxNode>(isCorrectContainer);
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
    }
}
