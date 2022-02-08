// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Wrapping.InitializerExpression
{
    internal abstract partial class AbstractInitializerExpressionWrapper<
        TListSyntax,
        TListItemSyntax>
        : AbstractSeparatedListWrapper<TListSyntax, TListItemSyntax>
        where TListSyntax : SyntaxNode
        where TListItemSyntax : SyntaxNode
    {
        protected sealed override string Indent_all_items => FeaturesResources.Indent_all_elements;
        protected sealed override string Unwrap_all_items => FeaturesResources.Unwrap_all_elements;
        protected sealed override string Unwrap_list => FeaturesResources.Unwrap_initializer;
        protected sealed override string Wrap_every_item => FeaturesResources.Wrap_initializer;
        protected sealed override string Wrap_long_list => FeaturesResources.Wrap_long_initializer;

        protected AbstractInitializerExpressionWrapper(IIndentationService indentationService)
            : base(indentationService)
        {
        }

        protected abstract SeparatedSyntaxList<TListItemSyntax> GetListItems(TListSyntax listSyntax);

        protected abstract TListSyntax? TryGetApplicableList(SyntaxNode node);

        protected abstract bool TryGetNewLinesForBracesInObjectCollectionArrayInitializersOption(DocumentOptionSet options);

        public sealed override async Task<ICodeActionComputer?> TryCreateComputerAsync(
            Document document, int position, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var listSyntax = TryGetApplicableList(declaration);
            if (listSyntax == null)
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
                this, document, sourceText, options, listSyntax, listItems, TryGetNewLinesForBracesInObjectCollectionArrayInitializersOption(options), cancellationToken);
        }
    }
}
