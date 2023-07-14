// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    internal abstract class AbstractCSharpTypeSnippetProvider : AbstractTypeSnippetProvider
    {
        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.AbstractKeyword,
            SyntaxKind.SealedKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword
        };

        protected override async Task<bool> HasPrecedingAccessibilityModifiersAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return tree.GetPrecedingModifiers(position, cancellationToken).Any(SyntaxFacts.IsAccessibilityModifier);
        }

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            return
                syntaxContext.IsGlobalStatementContext ||
                syntaxContext.IsTypeDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            var typeDeclaration = (BaseTypeDeclarationSyntax)caretTarget;
            var triviaSpan = typeDeclaration.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        protected override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, Func<SyntaxNode?, bool> isCorrectContainer)
        {
            var node = root.FindNode(TextSpan.FromBounds(position, position));
            return node.GetAncestorOrThis<BaseTypeDeclarationSyntax>();
        }

        protected override async Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var snippet = root.GetAnnotatedNodes(FindSnippetAnnotation).FirstOrDefault();

            if (snippet is not BaseTypeDeclarationSyntax originalTypeDeclaration)
                return document;

            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            var indentationString = СSharpSnippetIndentationHelpers.GetBlockLikeIndentationString(document, originalTypeDeclaration.OpenBraceToken.SpanStart, syntaxFormattingOptions, cancellationToken);

            var newTypeDeclaration = originalTypeDeclaration.WithCloseBraceToken(
                originalTypeDeclaration.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));

            var newRoot = root.ReplaceNode(originalTypeDeclaration, newTypeDeclaration.WithAdditionalAnnotations(CursorAnnotation, FindSnippetAnnotation));
            return document.WithSyntaxRoot(newRoot);
        }

        protected override void GetTypeDeclarationIdentifier(SyntaxNode node, out SyntaxToken identifier)
        {
            var typeDeclaration = (BaseTypeDeclarationSyntax)node;
            identifier = typeDeclaration.Identifier;
        }
    }
}
