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

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractIfSnippetProvider : AbstractSnippetProvider
    {
        protected abstract void GetPartsOfIfStatement(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode condition, out SyntaxNode statement);
        public override string SnippetIdentifier => "if";

        public override string SnippetDisplayName => FeaturesResources.Insert_an_if_statement;

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            return syntaxContext.IsStatementContext || syntaxContext.IsGlobalStatementContext;
        }

        protected override Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var snippetTextChange = GenerateSnippetTextChange(document, position);
            return Task.FromResult(ImmutableArray.Create(snippetTextChange));
        }

        private static TextChange GenerateSnippetTextChange(Document document, int position)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var ifStatement = generator.IfStatement(generator.TrueLiteralExpression(), Array.Empty<SyntaxNode>());

            return new TextChange(TextSpan.FromBounds(position, position), ifStatement.ToFullString());
        }

        protected override int? GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
        {
            GetPartsOfIfStatement(caretTarget, out _, out _, out var statement);
            return statement.SpanStart + 1;
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

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        protected override List<(string, List<TextSpan>)> GetRenameLocationsMap(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var renameLocationsMap = new List<(string, List<TextSpan>)>();
            GetPartsOfIfStatement(node, out _, out var condition, out var statement);

            var list1 = new List<TextSpan>
            {
                // Need to add 1 to the span start because we are retrieving a block and want the cursor to appear inside the block.
                new TextSpan(statement.SpanStart + 1, statement.Span.Length)
            };

            renameLocationsMap.Add(("", list1));

            var list2 = new List<TextSpan>
            {
                new TextSpan(condition.SpanStart, condition.Span.Length)
            };

            renameLocationsMap.Add((condition.ToString(), list2));

            return renameLocationsMap;
        }

        protected override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, ISyntaxFacts syntaxFacts)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position));

            SyntaxNode? statementOfGlobalStatement = null;
            if (syntaxFacts.IsGlobalStatement(closestNode))
            {
                statementOfGlobalStatement = syntaxFacts.GetStatementOfGlobalStatement(closestNode);
            }

            var nearestStatement = statementOfGlobalStatement is not null
                ? syntaxFacts.IsIfStatement(statementOfGlobalStatement)
                    ? statementOfGlobalStatement
                    : null
                : closestNode.DescendantNodesAndSelf(syntaxFacts.IsIfStatement).FirstOrDefault();

            if (nearestStatement is null)
            {
                return null;
            }

            // Checking to see if that expression statement that we found is
            // starting at the same position as the position we inserted
            // the if statement.
            if (nearestStatement.SpanStart != position)
            {
                return null;
            }

            return nearestStatement;
        }
    }
}
