// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            /// <summary>
            /// Indentation to use when placing brace.  e.g.:
            /// 
            ///     var v = new List {
            ///     ^
            ///     |
            /// </summary>
            private readonly SyntaxTrivia _braceIndentationTrivia;

            /// <summary>
            /// Whether or not we should move the open brace of this separated list to a new line.  Many separated lists
            /// will never move the brace (like a parameter list).  And some separated lists may move the brace
            /// depending on if a particular option is set (like the collection initializer brace in C#).
            /// </summary>
            private readonly bool _shouldMoveOpenBraceToNewLine;

            /// <summary>
            /// Whether or not we should move the close brace of this separated list to a new line.  Some lists will
            /// never move the close brace (like a parameter list), while some will always move it (like a collection
            /// initializer in both C# or VB).
            /// </summary>
            private readonly bool _shouldMoveCloseBraceToNewLine;

            public SeparatedSyntaxListCodeActionComputer(
                AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax> service,
                Document document,
                SourceText sourceText,
                SyntaxWrappingOptions options,
                TListSyntax listSyntax,
                SeparatedSyntaxList<TListItemSyntax> listItems,
                CancellationToken cancellationToken)
                : base(service, document, sourceText, options, cancellationToken)
            {
                _listSyntax = listSyntax;
                _listItems = listItems;

                _shouldMoveOpenBraceToNewLine = service.ShouldMoveOpenBraceToNewLine(options);
                _shouldMoveCloseBraceToNewLine = service.ShouldMoveCloseBraceToNewLine;

                var generator = SyntaxGenerator.GetGenerator(OriginalDocument);

                _afterOpenTokenIndentationTrivia = generator.Whitespace(GetAfterOpenTokenIdentation());
                _singleIndentationTrivia = generator.Whitespace(GetSingleIdentation());
                _braceIndentationTrivia = generator.Whitespace(GetBraceTokenIndentation());
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

                var indentString = afterOpenTokenOffset.CreateIndentationString(Options.UseTabs, Options.TabSize);
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

            private string GetBraceTokenIndentation()
            {
                var previousToken = _listSyntax.GetFirstToken().GetPreviousToken();

                // Block indentation is the only style that correctly indents across all initializer expressions
                return GetIndentationAfter(previousToken, Formatting.FormattingOptions.IndentStyle.Block);
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            {
                using var _ = ArrayBuilder<WrappingGroup>.GetInstance(out var result);
                await AddWrappingGroupsAsync(result).ConfigureAwait(false);
                return result.ToImmutable();
            }

            private async Task AddWrappingGroupsAsync(ArrayBuilder<WrappingGroup> result)
            {
                result.Add(await GetWrapEveryGroupAsync().ConfigureAwait(false));
                result.Add(await GetUnwrapGroupAsync().ConfigureAwait(false));
                result.Add(await GetWrapLongGroupAsync().ConfigureAwait(false));
            }

            #region unwrap group

            private async Task<WrappingGroup> GetUnwrapGroupAsync()
            {
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var unwrapActions);

                var parentTitle = Wrapper.Unwrap_list;

                // MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                if (this.Wrapper.Supports_UnwrapGroup_WrapFirst_IndentRest)
                {
                    // MethodName(
                    //      int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                    unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));
                }

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(isInlinable: true, unwrapActions.ToImmutable());
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
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_shouldMoveOpenBraceToNewLine)
                    result.Add(Edit.DeleteBetween(_listSyntax.GetFirstToken().GetPreviousToken(), _listSyntax.GetFirstToken()));

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                foreach (var comma in _listItems.GetSeparators())
                {
                    result.Add(Edit.DeleteBetween(comma.GetPreviousToken(), comma));
                    result.Add(Edit.DeleteBetween(comma, comma.GetNextToken()));
                }

                var last = _listItems.GetWithSeparators().Last();
                if (last.IsNode)
                    result.Add(Edit.DeleteBetween(last, _listSyntax.GetLastToken()));

                return result.ToImmutable();
            }

            #endregion

            #region wrap long line

            private async Task<WrappingGroup> GetWrapLongGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_long_list;
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                if (this.Wrapper.Supports_WrapLongGroup_UnwrapFirst)
                {
                    // MethodName(int a, int b, int c,
                    //            int d, int e, int f,
                    //            int g, int h, int i,
                    //            int j)
                    codeActions.Add(await GetWrapLongLineCodeActionAsync(
                        parentTitle, WrappingStyle.UnwrapFirst_AlignRest).ConfigureAwait(false));
                }

                // MethodName(
                //     int a, int b, int c, int d, int e,
                //     int f, int g, int h, int i, int j)
                codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                if (this.Wrapper.Supports_WrapLongGroup_UnwrapFirst)
                {
                    // MethodName(int a, int b, int c, 
                    //     int d, int e, int f, int g,
                    //     int h, int i, int j)
                    codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));
                }

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
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_shouldMoveOpenBraceToNewLine)
                    result.Add(Edit.UpdateBetween(_listSyntax.GetFirstToken().GetPreviousToken(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetFirstToken()));

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                var currentOffset = wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? indentationTrivia.FullWidth()
                    : _afterOpenTokenIndentationTrivia.FullWidth();
                var itemsAndSeparators = _listItems.GetWithSeparators();

                for (var i = 0; i < itemsAndSeparators.Count; i += 2)
                {
                    var item = itemsAndSeparators[i].AsNode()!;

                    // Figure out where we'd be after this item.
                    currentOffset += item.Span.Length;

                    if (i > 0)
                    {
                        if (currentOffset < Options.WrappingColumn)
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

                    // Get rid of any spaces between the list item and the following comma token
                    if (i + 1 < itemsAndSeparators.Count)
                    {
                        var comma = itemsAndSeparators[i + 1];
                        Contract.ThrowIfFalse(comma.IsToken);
                        result.Add(Edit.DeleteBetween(item, comma));
                        currentOffset += comma.Span.Length;
                    }
                }

                if (this.Wrapper.ShouldMoveCloseBraceToNewLine)
                {
                    result.Add(Edit.UpdateBetween(itemsAndSeparators.Last(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetLastToken()));
                }
                else
                {
                    result.Add(Edit.DeleteBetween(itemsAndSeparators.Last(), _listSyntax.GetLastToken()));
                }

                return result.ToImmutable();
            }

            #endregion

            #region wrap every

            private async Task<WrappingGroup> GetWrapEveryGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_every_item;

                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                if (this.Wrapper.Supports_WrapEveryGroup_UnwrapFirst)
                {
                    // MethodName(int a,
                    //            int b,
                    //            ...
                    //            int j);
                    codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                        parentTitle, WrappingStyle.UnwrapFirst_AlignRest).ConfigureAwait(false));
                }

                // MethodName(
                //     int a,
                //     int b,
                //     ...
                //     int j)
                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                if (this.Wrapper.Supports_WrapEveryGroup_UnwrapFirst)
                {
                    // MethodName(int a,
                    //     int b,
                    //     ...
                    //     int j)
                    codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                        parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));
                }

                // See comment in GetWrapLongTopLevelCodeActionAsync for explanation of why we're
                // not inlinable.
                return new WrappingGroup(isInlinable: false, codeActions.ToImmutable());
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
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_shouldMoveOpenBraceToNewLine)
                    result.Add(Edit.UpdateBetween(_listSyntax.GetFirstToken().GetPreviousToken(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetFirstToken()));

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                var itemsAndSeparators = _listItems.GetWithSeparators();

                for (var i = 1; i < itemsAndSeparators.Count; i += 2)
                {
                    var comma = itemsAndSeparators[i].AsToken();

                    var item = itemsAndSeparators[i - 1];
                    result.Add(Edit.DeleteBetween(item, comma));

                    if (i < itemsAndSeparators.Count - 1)
                    {
                        // Always wrap between this comma and the next item.
                        result.Add(Edit.UpdateBetween(
                            comma, NewLineTrivia, indentationTrivia, itemsAndSeparators[i + 1]));
                    }
                }

                if (_shouldMoveCloseBraceToNewLine)
                {
                    result.Add(Edit.UpdateBetween(itemsAndSeparators.Last(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetLastToken()));
                }
                else
                {
                    // last item.  Delete whatever is between it and the close token of the list.
                    result.Add(Edit.DeleteBetween(itemsAndSeparators.Last(), _listSyntax.GetLastToken()));
                }

                return result.ToImmutable();
            }

            #endregion
        }
    }
}
