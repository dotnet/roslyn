// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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
        private class SeparatedSyntaxListCodeActionComputer : AbstractCodeActionComputer<AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>>
        {
            private readonly TListSyntax _listSyntax;
            private readonly SeparatedSyntaxList<TListItemSyntax> _listItems;

            /// <summary>
            /// The indentation string necessary to indent an item in a list such that the start of
            /// that item will exact start at the end of the open-token for the containing list. i.e.
            /// 
            ///     void Goobar(
            ///                 ^
            ///                 |
            /// 
            /// This is the indentation we want when we're aligning wrapped items with the first item 
            /// in the list.
            /// </summary>
            private readonly SyntaxTrivia _afterOpenTokenIndentationTrivia;

            /// <summary>
            /// Indentation amount for any items that have been wrapped to a new line.  Valid if we're
            /// not aligning with the first item. i.e.
            /// 
            ///     void Goobar(
            ///         ^
            ///         |
            /// </summary>
            private readonly SyntaxTrivia _singleIndentationTrivia;

            private readonly SyntaxTrivia _elasticTrivia;

            public SeparatedSyntaxListCodeActionComputer(
                AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax> service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems,
                CancellationToken cancellationToken)
                : base(service, document, sourceText, options, cancellationToken)
            {
                _listSyntax = listSyntax;
                _listItems = listItems;

                var generator = SyntaxGenerator.GetGenerator(this.OriginalDocument);

                _afterOpenTokenIndentationTrivia = generator.Whitespace(GetAfterOpenTokenIdentation());
                _singleIndentationTrivia = generator.Whitespace(GetSingleIdentation());
                _elasticTrivia = generator.ElasticCarriageReturnLineFeed;
            }

            private void AddTextChangeBetweenOpenAndFirstItem(
                WrappingStyle wrappingStyle, ArrayBuilder<Edit> result)
            {
                result.Add(wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? Edit.UpdateBetween(_listSyntax.GetFirstToken(), NewLineTrivia, _singleIndentationTrivia, _listItems[0])
                    : Edit.DeleteBetween(_listSyntax.GetFirstToken(), _listItems[0]));
            }

            private string GetAfterOpenTokenIdentation()
            {
                var openToken = _listSyntax.GetFirstToken();
                var afterOpenTokenOffset = OriginalSourceText.GetOffset(openToken.Span.End);

                var indentString = afterOpenTokenOffset.CreateIndentationString(UseTabs, TabSize);
                return indentString;
            }

            private string GetSingleIdentation()
            {
                // Insert a newline after the open token of the list.  Then ask the
                // ISynchronousIndentationService where it thinks that the next line should be
                // indented.
                var openToken = _listSyntax.GetFirstToken();

                return GetSmartIndentationAfter(openToken);
            }

            private SyntaxTrivia GetIndentationTrivia(WrappingStyle wrappingStyle)
            {
                return wrappingStyle == WrappingStyle.UnwrapFirst_AlignRest
                    ? _afterOpenTokenIndentationTrivia
                    : _singleIndentationTrivia;
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            {
                var result = ArrayBuilder<WrappingGroup>.GetInstance();
                await AddWrappingGroups(result).ConfigureAwait(false);
                return result.ToImmutableAndFree();
            }

            private async Task AddWrappingGroups(ArrayBuilder<WrappingGroup> result)
            {
                result.Add(await GetWrapEveryGroupAsync().ConfigureAwait(false));
                result.Add(await GetUnwrapGroupAsync().ConfigureAwait(false));
                result.Add(await GetWrapLongGroupAsync().ConfigureAwait(false));
            }

            #region unwrap group

            private async Task<WrappingGroup> GetUnwrapGroupAsync()
            {
                var unwrapActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                var parentTitle = Wrapper.Unwrap_list;

                // MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // MethodName(
                //      int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(isInlinable: true, unwrapActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(string parentTitle, WrappingStyle wrappingStyle)
            {
                var edits = GetUnwrapAllEdits(wrappingStyle);
                var title = wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? Wrapper.Unwrap_and_indent_all_items
                    : Wrapper.Unwrap_all_items;

                return await TryCreateCodeActionAsync(edits, parentTitle, title).ConfigureAwait(false);
            }

            private ImmutableArray<Edit> GetUnwrapAllEdits(WrappingStyle wrappingStyle)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                foreach (var comma in _listItems.GetSeparators())
                {
                    result.Add(Edit.DeleteBetween(comma.GetPreviousToken(), comma));
                    result.Add(Edit.DeleteBetween(comma, comma.GetNextToken()));
                }

                result.Add(Edit.DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));
                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap long line

            private async Task<WrappingGroup> GetWrapLongGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_long_list;
                var codeActions = ArrayBuilder<WrapItemsAction>.GetInstance();

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

                return new WrappingGroup(isInlinable: false, codeActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetWrapLongLineCodeActionAsync(
                string parentTitle, WrappingStyle wrappingStyle)
            {
                var indentationTrivia = GetIndentationTrivia(wrappingStyle);

                var edits = GetWrapLongLinesEdits(wrappingStyle, indentationTrivia);
                var title = GetNestedCodeActionTitle(wrappingStyle);

                return await TryCreateCodeActionAsync(edits, parentTitle, title).ConfigureAwait(false);
            }

            private ImmutableArray<Edit> GetWrapLongLinesEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

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

                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap every

            private async Task<WrappingGroup> GetWrapEveryGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_every_item;

                var codeActions = ArrayBuilder<WrapItemsAction>.GetInstance();

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
                return new WrappingGroup(isInlinable: false, codeActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetWrapEveryNestedCodeActionAsync(
                string parentTitle, WrappingStyle wrappingStyle)
            {
                var indentationTrivia = GetIndentationTrivia(wrappingStyle);

                var edits = GetWrapEachEdits(wrappingStyle, indentationTrivia);
                var title = GetNestedCodeActionTitle(wrappingStyle);

                return await TryCreateCodeActionAsync(edits, parentTitle, title).ConfigureAwait(false);
            }

            private string GetNestedCodeActionTitle(WrappingStyle wrappingStyle)
                => wrappingStyle switch
                {
                    WrappingStyle.WrapFirst_IndentRest => Wrapper.Indent_all_items,
                    WrappingStyle.UnwrapFirst_AlignRest => Wrapper.Align_wrapped_items,
                    WrappingStyle.UnwrapFirst_IndentRest => Wrapper.Indent_wrapped_items,
                    _ => throw ExceptionUtilities.UnexpectedValue(wrappingStyle),
                };

            private ImmutableArray<Edit> GetWrapEachEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                if (_listSyntax.RawKind == 8645 || _listSyntax.RawKind == 8646)
                {
                    Edit.UpdateBetween(_listSyntax.GetFirstToken(), NewLineTrivia, _elasticTrivia, _listItems[0]);
                }
                else
                {
                    AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);
                }

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

                if (_listSyntax.RawKind == 8645 || _listSyntax.RawKind == 8646)
                {
                    result.Add(Edit.UpdateBetween(_listItems.Last(), NewLineTrivia, _elasticTrivia, _listSyntax.GetLastToken()));
                }
                else
                {
                    // last item.  Delete whatever is between it and the close token of the list.
                    result.Add(Edit.DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));
                }

                return result.ToImmutableAndFree();
            }

            #endregion
        }
    }
}
