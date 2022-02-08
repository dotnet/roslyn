// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList
{
    internal abstract partial class AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// </summary>
        private sealed class SeparatedSyntaxListCodeActionComputer : AbstractSeparatedListCodeComputer<AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>>
        {
            public SeparatedSyntaxListCodeActionComputer(
                AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax> service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems,
                CancellationToken cancellationToken)
                : base(service, document, sourceText, options, listSyntax, listItems, cancellationToken)
            {
            }

            protected sealed override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            {
                using var _ = ArrayBuilder<WrappingGroup>.GetInstance(out var result);
                await AddWrappingGroupsAsync(result).ConfigureAwait(false);
                return result.ToImmutableAndClear();
            }

            protected sealed override Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(string parentTitle, WrappingStyle wrappingStyle)
            {
                var edits = GetUnwrapAllEdits(wrappingStyle);
                var title = wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? Wrapper.Unwrap_and_indent_all_items
                    : Wrapper.Unwrap_all_items;

                return TryCreateCodeActionAsync(edits, parentTitle, title);
            }

            protected sealed override string GetNestedCodeActionTitle(WrappingStyle wrappingStyle)
                => wrappingStyle switch
                {
                    WrappingStyle.WrapFirst_IndentRest => Wrapper.Indent_all_items,
                    WrappingStyle.UnwrapFirst_AlignRest => Wrapper.Align_wrapped_items,
                    WrappingStyle.UnwrapFirst_IndentRest => Wrapper.Indent_wrapped_items,
                    _ => throw ExceptionUtilities.UnexpectedValue(wrappingStyle),
                };

            protected sealed override async Task<WrappingGroup> GetWrapEveryGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_every_item;

                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                // MethodName(int a,
                //            int b,
                //            ...
                //            int j);
                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.UnwrapFirst_AlignRest).ConfigureAwait(false));

                // MethodName(
                //     int a,
                //     int b,
                //     ...
                //     int j)
                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                // MethodName(int a,
                //     int b,
                //     ...
                //     int j)
                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // See comment in GetWrapLongTopLevelCodeActionAsync for explanation of why we're
                // not inlinable.
                return new WrappingGroup(isInlinable: false, codeActions.ToImmutableAndClear());
            }

            protected sealed override async Task<WrappingGroup> GetUnwrapGroupAsync()
            {
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var unwrapActions);

                var parentTitle = Wrapper.Unwrap_list;

                // MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // MethodName(
                //      int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(isInlinable: true, unwrapActions.ToImmutable());
            }

            protected sealed override async Task<WrappingGroup> GetWrapLongGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_long_list;
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                // MethodName(int a, int b, int c,
                //            int d, int e, int f,
                //            int g, int h, int i,
                //            int j)
                codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.UnwrapFirst_AlignRest).ConfigureAwait(false));

                // MethodName(
                //     int a, int b, int c, int d, int e,
                //     int f, int g, int h, int i, int j)
                codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                // MethodName(int a, int b, int c, 
                //     int d, int e, int f, int g,
                //     int h, int i, int j)
                codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // The wrap-all and wrap-long code action titles are not unique.  i.e. we show them
                // as:
                //      Wrap every parameter:
                //          Align parameters
                //          Indent wrapped parameters
                //      Wrap long parameter list:
                //          Align parameters
                //          Indent wrapped parameters
                //
                // We can't in-line these nested actions because the parent title is necessary to
                // determine which situation each child action applies to.

                return new WrappingGroup(isInlinable: false, codeActions.ToImmutable());
            }

            protected sealed override ImmutableArray<Edit> GetWrapEachEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                var itemsAndSeparators = _listItems.GetWithSeparators();

                for (var i = 0; i < itemsAndSeparators.Count; i += 2)
                {
                    var item = itemsAndSeparators[i].AsNode();
                    if (i < itemsAndSeparators.Count - 1)
                    {
                        // intermediary item
                        var comma = itemsAndSeparators[i + 1].AsToken();
                        result.Add(Edit.DeleteBetween(item, comma));

                        // Always wrap between this comma and the next item.
                        result.Add(Edit.UpdateBetween(
                            comma, NewLineTrivia, indentationTrivia, itemsAndSeparators[i + 2]));
                    }
                }

                // last item.  Delete whatever is between it and the close token of the list.
                result.Add(Edit.DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));

                return result.ToImmutableAndClear();
            }

            protected sealed override ImmutableArray<Edit> GetWrapLongLinesEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                var currentOffset = wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? indentationTrivia.FullWidth()
                    : _afterOpenTokenIndentationTrivia.FullWidth();
                var itemsAndSeparators = _listItems.GetWithSeparators();

                for (var i = 0; i < itemsAndSeparators.Count; i += 2)
                {
                    var item = itemsAndSeparators[i].AsNode();

                    // Figure out where we'd be after this item.
                    currentOffset += item.Span.Length;

                    if (i > 0)
                    {
                        if (currentOffset < WrappingColumn)
                        {
                            // this item would not make us go pass our preferred wrapping column. So
                            // keep it on this line, making sure there's a space between the previous
                            // comma and us.
                            result.Add(Edit.UpdateBetween(itemsAndSeparators[i - 1], SingleWhitespaceTrivia, NoTrivia, item));
                            currentOffset += " ".Length;
                        }
                        else
                        {
                            // not the first item on the line and this item makes us go past the wrapping
                            // limit.  We want to wrap before this item.
                            result.Add(Edit.UpdateBetween(itemsAndSeparators[i - 1], NewLineTrivia, indentationTrivia, item));
                            currentOffset = indentationTrivia.FullWidth() + item.Span.Length;
                        }
                    }

                    // Get rid of any spaces between the list item and the following token (a
                    // comma or close token).
                    var nextToken = item.GetLastToken().GetNextToken();

                    result.Add(Edit.DeleteBetween(item, nextToken));
                    currentOffset += nextToken.Span.Length;
                }

                return result.ToImmutable();
            }

            protected override ImmutableArray<Edit> GetUnwrapAllEdits(WrappingStyle wrappingStyle)
            {
                return GetSeparatedListEdits(wrappingStyle);
            }
        }
    }
}
