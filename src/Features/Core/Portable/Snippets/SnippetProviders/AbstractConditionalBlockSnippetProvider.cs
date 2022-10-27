// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders
{
    /// <summary>
    /// Base class for "if" and "while" snippet providers
    /// </summary>
    internal abstract class AbstractConditionalBlockSnippetProvider : AbstractSnippetProvider
    {
        protected abstract TextChange GenerateSnippetTextChange(Document document, int position);
        protected abstract SyntaxNode GetCondition(SyntaxNode node);

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

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            var condition = GetCondition(node);
            arrayBuilder.Add(new SnippetPlaceholder(identifier: condition.ToString(), placeholderPositions: ImmutableArray.Create(condition.SpanStart)));

            return arrayBuilder.ToImmutableArray();
        }
    }
}
