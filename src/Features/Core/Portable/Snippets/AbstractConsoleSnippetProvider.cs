// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractConsoleSnippetProvider : AbstractSnippetProvider
    {
        protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

        public override string SnippetIdentifier => "cw";

        public override string SnippetDisplayName => FeaturesResources.Write_to_the_Console;

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var consoleSymbol = await GetSymbolFromMetaDataNameAsync(document, cancellationToken).ConfigureAwait(false);
            if (consoleSymbol is null)
            {
                return false;
            }

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
            var symbol = await GetSymbolFromMetaDataNameAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);

            // We know symbol is not null at this point since it was checked when determining
            // if we are in a valid location to insert the snippet.
            var typeExpression = generator.TypeExpression(symbol!);
            var declaration = GetAsyncSupportingDeclaration(token);
            var isAsync = declaration is not null && generator.GetModifiers(declaration).IsAsync;

            var invocation = isAsync
                ? generator.AwaitExpression(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.MemberAccessExpression(typeExpression, generator.IdentifierName(nameof(Console.Out))), generator.IdentifierName(nameof(Console.Out.WriteLineAsync)))))
                : generator.InvocationExpression(generator.MemberAccessExpression(typeExpression, generator.IdentifierName(nameof(Console.WriteLine))));
            var expressionStatement = generator.ExpressionStatement(invocation);
            return new TextChange(TextSpan.FromBounds(position, position), expressionStatement.NormalizeWhitespace().ToFullString());
        }

        protected override int? GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
        {
            var invocationExpression = caretTarget.DescendantNodes().Where(syntaxFacts.IsInvocationExpression).FirstOrDefault();
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
            return openParenToken.Span.End;
        }

        protected override async Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document,
            SyntaxAnnotation findSnippetAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var snippetExpressionNode = GetConsoleExpressionStatement(syntaxFacts, root, position);
            if (snippetExpressionNode is null)
            {
                return root;
            }

            var symbol = await GetSymbolFromMetaDataNameAsync(document, cancellationToken).ConfigureAwait(false);

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, SymbolAnnotation.Create(symbol!), Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        private static SyntaxNode? GetConsoleExpressionStatement(ISyntaxFactsService syntaxFacts, SyntaxNode root, int position)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
            var nearestExpressionStatement = closestNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsExpressionStatement);
            if (nearestExpressionStatement is null)
            {
                return null;
            }

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
