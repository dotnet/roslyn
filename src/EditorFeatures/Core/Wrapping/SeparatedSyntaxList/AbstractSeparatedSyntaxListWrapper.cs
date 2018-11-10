// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList
{
    internal abstract partial class AbstractSeparatedSyntaxListWrapper<
        TListSyntax,
        TListItemSyntax>
        : AbstractWrapper
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected abstract string ListName { get; }
        protected abstract string ItemNamePlural { get; }
        protected abstract string ItemNameSingular { get; }

        protected abstract IBlankLineIndentationService GetIndentationService();

        protected abstract TListSyntax GetApplicableList(SyntaxNode node);
        protected abstract SeparatedSyntaxList<TListItemSyntax> GetListItems(TListSyntax listSyntax);
        protected abstract bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, TListSyntax listSyntax);

        public override async Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
            Document document, int position, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var listSyntax = GetApplicableList(declaration);
            if (listSyntax == null)
            {
                return default;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!PositionIsApplicable(root, position, declaration, listSyntax))
            {
                return default;
            }

            var listItems = GetListItems(listSyntax);
            if (listItems.Count <= 1)
            {
                // nothing to do with 0-1 items.  Simple enough for users to just edit
                // themselves, and this prevents constant clutter with formatting that isn't
                // really that useful.
                return default;
            }

            // For now, don't offer if any item spans multiple lines.  We'll very likely screw up
            // formatting badly.  If this is really important to support, we can put in the effort
            // to properly move multi-line items around (which would involve properly fixing up the
            // indentation of lines within them.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in listItems)
            {
                if (item == null ||
                    item.Span.IsEmpty ||
                    !sourceText.AreOnSameLine(item.GetFirstToken(), item.GetLastToken()))
                {
                    return default;
                }
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // If there are comments between any nodes/tokens in the list then don't offer the
            // refactoring.  We'll likely not be able to properly keep the comments in the right
            // place as we move things around.
            var openToken = listSyntax.GetFirstToken();
            var closeToken = listSyntax.GetLastToken();

            if (ContainsNonWhitespaceTrivia(syntaxFacts, openToken.TrailingTrivia) ||
                ContainsNonWhitespaceTrivia(syntaxFacts, closeToken.LeadingTrivia))
            {
                return default;
            }

            foreach (var nodeOrToken in listItems.GetWithSeparators())
            {
                if (ContainsNonWhitespaceTrivia(syntaxFacts, nodeOrToken.GetLeadingTrivia()) ||
                    ContainsNonWhitespaceTrivia(syntaxFacts, nodeOrToken.GetTrailingTrivia()))
                {
                    return default;
                }
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var computer = new CodeActionComputer(this, document, options, listSyntax, listItems);
            var codeActions = await computer.DoAsync(cancellationToken).ConfigureAwait(false);
            return codeActions;
        }

        private bool ContainsNonWhitespaceTrivia(ISyntaxFactsService syntaxFacts, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (!syntaxFacts.IsWhitespaceOrEndOfLineTrivia(trivia))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
