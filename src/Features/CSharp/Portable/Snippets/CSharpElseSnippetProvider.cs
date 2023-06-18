// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpElseSnippetProvider : AbstractElseSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpElseSnippetProvider()
        {
        }

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            var token = syntaxContext.TargetToken;

            // We have to consider all ancestor if statements of the last token until we find a match for this 'else':
            // while (true)
            //     if (true)
            //         while (true)
            //             if (true)
            //                 Console.WriteLine();
            //             else
            //                 Console.WriteLine();
            //     $$
            var isAfterIfStatement = false;

            foreach (var ifStatement in token.GetAncestors<IfStatementSyntax>())
            {
                // If there's a missing token at the end of the statement, it's incomplete and we do not offer 'else'.
                // context.TargetToken does not include zero width so in that case these will never be equal.
                if (ifStatement.Statement.GetLastToken(includeZeroWidth: true) == token)
                {
                    isAfterIfStatement = true;
                    break;
                }
            }

            return isAfterIfStatement && await base.IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false);
        }

        protected override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var elseClause = SyntaxFactory.ElseClause(SyntaxFactory.Block());
            return Task.FromResult(new TextChange(TextSpan.FromBounds(position, position), elseClause.ToFullString()));
        }

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            var elseClauseSyntax = (ElseClauseSyntax)caretTarget;
            var blockStatement = (BlockSyntax)elseClauseSyntax.Statement;

            var triviaSpan = blockStatement.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return СSharpSnippetIndentationHelpers.AddBlockIndentationToDocumentAsync<ElseClauseSyntax>(
                document,
                FindSnippetAnnotation,
                static c => (BlockSyntax)c.Statement,
                cancellationToken);
        }
    }
}
