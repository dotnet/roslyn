using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.Wrapping.InitializerExpression
{
    internal abstract partial class AbstractInitializerExpression<
        TListSyntax,
        TListItemSyntax>
        : AbstractSyntaxWrapper
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected abstract string Unwrap_list { get; }
        protected abstract string Wrap_long_list { get; }
        protected abstract string Unwrap_all_items { get; }
        protected abstract string Wrap_every_item { get; }
        protected abstract string Indent_all_items { get; }

        protected abstract bool DoWrapInitializerOpenBrace { get; }

        protected AbstractInitializerExpression(IIndentationService indentationService)
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
            return new InitializerExpressionCodeActionComputer(
                this, document, sourceText, options, listSyntax, listItems, DoWrapInitializerOpenBrace, cancellationToken);
        }
    }
}
