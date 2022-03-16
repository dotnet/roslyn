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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractIfSnippetProvider : AbstractSnippetProvider
    {
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

            var ifStatement = generator.IfStatement(generator.TrueLiteralExpression(), Array.Empty<SyntaxNode>(), Array.Empty<SyntaxNode>());
            return new TextChange(TextSpan.FromBounds(position, position), ifStatement.ToFullString());
        }

        protected override int? GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
        {
            var invocationExpression = caretTarget.DescendantNodes().Where(syntaxFacts.Blo).FirstOrDefault();
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
    }
}
