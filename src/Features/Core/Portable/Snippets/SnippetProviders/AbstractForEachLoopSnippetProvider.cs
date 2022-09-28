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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal abstract class AbstractForEachLoopSnippetProvider : AbstractSnippetProvider
    {
        protected abstract Task<SyntaxNode> CreateForEachLoopStatementSyntaxAsync(Document document, int position, CancellationToken cancellationToken);

        public override string SnippetIdentifier => "foreach";

        public override string SnippetDescription => FeaturesResources.foreach_loop;

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            return syntaxContext.IsStatementContext || syntaxContext.IsGlobalStatementContext;
        }

        protected override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var forEachStatementSyntax = await CreateForEachLoopStatementSyntaxAsync(document, position, cancellationToken).ConfigureAwait(false);
            var snippetTextChange = new TextChange(TextSpan.FromBounds(position, position), forEachStatementSyntax.NormalizeWhitespace().ToFullString());
            return ImmutableArray.Create(snippetTextChange);
        }

        protected override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts)
        {
            return syntaxFacts.IsForEachStatement;
        }

        protected override async Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document,
            SyntaxAnnotation findSnippetAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position, GetSnippetContainerFunction(syntaxFacts));
            if (snippetExpressionNode is null)
            {
                return root;
            }

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }
    }
}
