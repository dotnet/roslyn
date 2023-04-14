// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractForEachLoopSnippetProvider : AbstractStatementSnippetProvider
    {
        protected abstract Task<SyntaxNode> CreateForEachLoopStatementSyntaxAsync(Document document, int position, CancellationToken cancellationToken);

        public override string Identifier => "foreach";

        public override string Description => FeaturesResources.foreach_loop;

        protected override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var forEachStatementSyntax = await CreateForEachLoopStatementSyntaxAsync(document, position, cancellationToken).ConfigureAwait(false);
            return new TextChange(TextSpan.FromBounds(position, position), forEachStatementSyntax.NormalizeWhitespace().ToFullString());
        }

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsForEachStatement;
        }
    }
}
