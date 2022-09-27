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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractIfSnippetProvider : AbstractSnippetProvider
    {
        public override string SnippetIdentifier => "if";

        public override string SnippetDescription => FeaturesResources.if_statement;

        public override ImmutableArray<string> AdditionalFilterTexts { get; } = ImmutableArray.Create("statement");

        protected abstract void GetIfStatementCondition(SyntaxNode node, out SyntaxNode condition);
        protected abstract void GetIfStatementCursorPosition(SourceText text, SyntaxNode node, out int position);

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

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsIfStatement;
        }

        private static TextChange GenerateSnippetTextChange(Document document, int position)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var ifStatement = generator.IfStatement(generator.TrueLiteralExpression(), Array.Empty<SyntaxNode>());

            return new TextChange(TextSpan.FromBounds(position, position), ifStatement.ToFullString());
        }

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            GetIfStatementCursorPosition(sourceText, caretTarget, out var cursorPosition);

            // Place at the end of the node specified for cursor position.
            // Is the statement node in C# and the "Then" keyword
            return cursorPosition;
        }

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            GetIfStatementCondition(node, out var condition);
            arrayBuilder.Add(new SnippetPlaceholder(identifier: condition.ToString(), placeholderPositions: ImmutableArray.Create(condition.SpanStart)));

            return arrayBuilder.ToImmutableArray();
        }
    }
}
