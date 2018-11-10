// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract partial class AbstractWrappingCodeRefactoringProvider<
        TListSyntax,
        TListItemSyntax>
        : CodeRefactoringProvider
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        private static ImmutableArray<string> s_mruTitles = ImmutableArray<string>.Empty;

        protected abstract string ListName { get; }
        protected abstract string ItemNamePlural { get; }
        protected abstract string ItemNameSingular { get; }

        protected abstract ISynchronousIndentationService GetIndentationService();

        protected abstract TListSyntax GetApplicableList(SyntaxNode node);
        protected abstract SeparatedSyntaxList<TListItemSyntax> GetListItems(TListSyntax listSyntax);
        protected abstract bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, TListSyntax listSyntax);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var span = context.Span;
            if (!span.IsEmpty)
            {
                return;
            }

            var position = span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var generator = document.GetLanguageService<SyntaxGenerator>();

            var declaration = token.Parent.AncestorsAndSelf().FirstOrDefault(n => GetApplicableList(n) != null);
            if (declaration == null)
            {
                return;
            }

            var listSyntax = GetApplicableList(declaration);
            // Make sure we don't have any syntax errors here.  Don't want to format if we don't
            // really understand what's going on.
            if (listSyntax.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            if (!PositionIsApplicable(root, position, declaration, listSyntax))
            {
                return;
            }

            var listItems = GetListItems(listSyntax);
            if (listItems.Count <= 1)
            {
                // nothing to do with 0-1 items.  Simple enough for users to just edit
                // themselves, and this prevents constant clutter with formatting that isn't
                // really that useful.
                return;
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
                    return;
                }
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var computer = new CodeActionComputer(this, document, options, listSyntax, listItems);
            var codeActions = await computer.DoAsync(cancellationToken).ConfigureAwait(false);

            context.RegisterRefactorings(codeActions);
        }

        private static ImmutableArray<CodeAction> SortActionsByMRU(ImmutableArray<CodeAction> codeActions)
        {
            // make a local so this array can't change out from under us.
            var mruTitles = s_mruTitles;
            return codeActions.Sort((ca1, ca2) =>
            {
                var titleIndex1 = mruTitles.IndexOf(GetSortTitle(ca1));
                var titleIndex2 = mruTitles.IndexOf(GetSortTitle(ca2));

                if (titleIndex1 >= 0 && titleIndex2 >= 0)
                {
                    // we've invoked both of these before.  Order by how recently it was invoked.
                    return titleIndex1 - titleIndex2;
                }

                // one of these has never been invoked.  It's always after an item that has been
                // invoked.
                if (titleIndex1 >= 0)
                {
                    return -1;
                }

                if (titleIndex2 >= 0)
                {
                    return 1;
                }

                // Neither of these has been invoked.   Keep it in the same order we found it in the
                // array.  Note: we cannot return 0 here as ImmutableArray/Array are not guaranteed
                // to sort stably.
                return codeActions.IndexOf(ca1) - codeActions.IndexOf(ca2);
            });
        }

        private static string GetSortTitle(CodeAction codeAction)
            => (codeAction as WrapItemsAction)?.SortTitle ?? codeAction.Title;
    }
}
