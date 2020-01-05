// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList
{
    using Microsoft.CodeAnalysis.Indentation;

    /// <summary>
    /// Base type for all wrappers that involve wrapping a comma-separated list of items.
    /// </summary>
    internal abstract partial class AbstractSeparatedSyntaxListWrapper<
        TListSyntax,
        TListItemSyntax>
        : AbstractSyntaxWrapper
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        //protected abstract string ListName { get; }
        //protected abstract string ItemNamePlural { get; }
        //protected abstract string ItemNameSingular { get; }

        protected abstract string Unwrap_list { get; }
        protected abstract string Wrap_long_list { get; }

        protected abstract string Unwrap_and_indent_all_items { get; }
        protected abstract string Unwrap_all_items { get; }
        protected abstract string Indent_all_items { get; }
        protected abstract string Align_wrapped_items { get; }
        protected abstract string Indent_wrapped_items { get; }

        protected abstract string Wrap_every_item { get; }

        protected AbstractSeparatedSyntaxListWrapper(IIndentationService indentationService)
            : base(indentationService)
        {
        }

        protected abstract TListSyntax TryGetApplicableList(SyntaxNode node);
        protected abstract SeparatedSyntaxList<TListItemSyntax> GetListItems(TListSyntax listSyntax);
        protected abstract bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, TListSyntax listSyntax);

        public override async Task<ICodeActionComputer> TryCreateComputerAsync(
            Document document, int position, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var listSyntax = TryGetApplicableList(declaration);
            if (listSyntax == null)
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!PositionIsApplicable(root, position, declaration, listSyntax))
            {
                return null;
            }

            var listItems = GetListItems(listSyntax);
            if (listItems.Count <= 1)
            {
                // nothing to do with 0-1 items.  Simple enough for users to just edit
                // themselves, and this prevents constant clutter with formatting that isn't
                // really that useful.
                return null;
            }

            var containsUnformattableContent = await ContainsUnformattableContentAsync(
                document, listItems.GetWithSeparators(), cancellationToken).ConfigureAwait(false);

            if (containsUnformattableContent)
            {
                return null;
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new SeparatedSyntaxListCodeActionComputer(
                this, document, sourceText, options, listSyntax, listItems, cancellationToken);
        }
    }
}
