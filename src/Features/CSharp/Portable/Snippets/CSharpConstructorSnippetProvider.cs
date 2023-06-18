// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
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
    internal sealed class CSharpConstructorSnippetProvider : AbstractConstructorSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConstructorSnippetProvider()
        {
        }

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            return
                syntaxContext.IsMemberDeclarationContext(
                    validTypeDeclarations: SyntaxKindSet.ClassStructRecordTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets the start of the BlockSyntax of the constructor declaration
        /// to be able to insert the caret position at that location.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax)caretTarget;
            var blockStatement = constructorDeclaration.Body;

            var triviaSpan = blockStatement!.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        protected override async Task<Document> AddIndentationToDocumentAsync(Document document, int position, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var snippet = root.GetAnnotatedNodes(_findSnippetAnnotation).FirstOrDefault();

            if (snippet is not ConstructorDeclarationSyntax constructorDeclaration)
                return document;

            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            var indentationString = СSharpSnippetIndentationHelpers.GetBlockLikeIndentationString(document, constructorDeclaration.Body!.SpanStart, syntaxFormattingOptions, cancellationToken);

            var blockStatement = constructorDeclaration.Body;
            blockStatement = blockStatement!.WithCloseBraceToken(blockStatement.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));
            var newConstructorDeclaration = constructorDeclaration.ReplaceNode(constructorDeclaration.Body!, blockStatement);

            var newRoot = root.ReplaceNode(constructorDeclaration, newConstructorDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
