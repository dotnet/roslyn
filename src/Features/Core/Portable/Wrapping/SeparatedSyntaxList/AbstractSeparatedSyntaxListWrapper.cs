// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList
{
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
        protected abstract string Unwrap_list { get; }
        protected abstract string Wrap_long_list { get; }

        protected abstract string Unwrap_and_indent_all_items { get; }
        protected abstract string Unwrap_all_items { get; }
        protected abstract string Indent_all_items { get; }
        protected abstract string Align_wrapped_items { get; }
        protected abstract string Indent_wrapped_items { get; }

        protected abstract string Wrap_every_item { get; }

        public abstract bool Supports_WrapEveryGroup_UnwrapFirst { get; }
        public abstract bool Supports_UnwrapGroup_WrapFirst_IndentRest { get; }
        public abstract bool Supports_WrapLongGroup_UnwrapFirst { get; }

        protected AbstractSeparatedSyntaxListWrapper(IIndentationService indentationService)
            : base(indentationService)
        {
        }

        protected abstract bool ShouldMoveCloseBraceToNewLine { get; }
        protected abstract bool ShouldMoveOpenBraceToNewLine(SyntaxWrappingOptions options);

        protected abstract TListSyntax? TryGetApplicableList(SyntaxNode node);
        protected abstract SeparatedSyntaxList<TListItemSyntax> GetListItems(TListSyntax listSyntax);
        protected abstract bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, bool containsSyntaxError, TListSyntax listSyntax);

        public override async Task<ICodeActionComputer?> TryCreateComputerAsync(
            Document document, int position, SyntaxNode declaration, SyntaxWrappingOptions options, bool containsSyntaxError, CancellationToken cancellationToken)
        {
            var listSyntax = TryGetApplicableList(declaration);
            if (listSyntax == null)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (!PositionIsApplicable(root, position, declaration, containsSyntaxError, listSyntax))
                return null;

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
                return null;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new SeparatedSyntaxListCodeActionComputer(
                this, document, sourceText, options, listSyntax, listItems, cancellationToken);
        }
    }
}
