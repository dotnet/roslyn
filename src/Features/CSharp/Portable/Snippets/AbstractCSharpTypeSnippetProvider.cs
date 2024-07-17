// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

internal abstract class AbstractCSharpTypeSnippetProvider<TTypeDeclarationSyntax> : AbstractTypeSnippetProvider<TTypeDeclarationSyntax>
    where TTypeDeclarationSyntax : BaseTypeDeclarationSyntax
{
    protected abstract ISet<SyntaxKind> ValidModifiers { get; }

    protected override bool IsValidSnippetLocation(in SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = (CSharpSyntaxContext)context.SyntaxContext;

        return
            syntaxContext.IsGlobalStatementContext ||
            syntaxContext.IsTypeDeclarationContext(
                validModifiers: ValidModifiers,
                validTypeDeclarations: SyntaxKindSet.NonEnumTypeDeclarations,
                canBePartial: true,
                cancellationToken: cancellationToken);
    }

    protected override async Task<TextChange?> GetAccessibilityModifiersChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        if (!await AreAccessibilityModifiersRequiredAsync(document, cancellationToken).ConfigureAwait(false))
            return null;

        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

        if (tree.GetPrecedingModifiers(position, cancellationToken).Any(SyntaxFacts.IsAccessibilityModifier))
            return null;

        var targetToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position);
        var targetPosition = position;

        var analyzerOptionsProvider = await document.GetAnalyzerOptionsProviderAsync(cancellationToken).ConfigureAwait(false);
        var preferredModifierOrderString = analyzerOptionsProvider.GetAnalyzerConfigOptions().GetOption(CSharpCodeStyleOptions.PreferredModifierOrder).Value;

        if (CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(preferredModifierOrderString, out var preferredOrder) &&
            preferredOrder.TryGetValue((int)SyntaxKind.PublicKeyword, out var publicModifierOrder))
        {
            while (targetToken.IsPotentialModifier(out var modifierKind))
            {
                if (preferredOrder.TryGetValue((int)modifierKind, out var targetTokenOrder) &&
                    targetTokenOrder > publicModifierOrder)
                {
                    targetPosition = targetToken.SpanStart;
                }

                targetToken = targetToken.GetPreviousToken();
            }
        }

        // If we are right after 'partial' token we need to insert modifier before it
        targetPosition = targetToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword) ? targetToken.SpanStart : targetPosition;

        return new TextChange(TextSpan.FromBounds(targetPosition, targetPosition), SyntaxFacts.GetText(SyntaxKind.PublicKeyword) + " ");
    }

    protected override int GetTargetCaretPosition(TTypeDeclarationSyntax typeDeclaration, SourceText sourceText)
    {
        var triviaSpan = typeDeclaration.CloseBraceToken.LeadingTrivia.Span;
        var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
        // Getting the location at the end of the line before the newline.
        return line.Span.End;
    }

    protected override TTypeDeclarationSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
    {
        var node = root.FindNode(TextSpan.FromBounds(position, position));
        return node.GetAncestorOrThis<TTypeDeclarationSyntax>();
    }

    protected override async Task<Document> AddIndentationToDocumentAsync(Document document, TTypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var indentationString = CSharpSnippetHelpers.GetBlockLikeIndentationString(document, typeDeclaration.OpenBraceToken.SpanStart, syntaxFormattingOptions, cancellationToken);

        var newTypeDeclaration = typeDeclaration.WithCloseBraceToken(
            typeDeclaration.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));

        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration.WithAdditionalAnnotations(FindSnippetAnnotation));
        return document.WithSyntaxRoot(newRoot);
    }

    protected sealed override SyntaxToken GetTypeDeclarationIdentifier(TTypeDeclarationSyntax baseTypeDeclaration)
        => baseTypeDeclaration.Identifier;
}
