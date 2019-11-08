using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Wrapping;
using Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping.InitializerExpression
{
    internal abstract partial class AbstractInitializerExpression<TListSyntax, TListItemSyntax>
    {
        private class InitializerExpressionCodeActionComputer : AbstractCodeActionComputer<AbstractInitializerExpression<TListSyntax, TListItemSyntax>>
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

            public InitializerExpressionCodeActionComputer(
                AbstractInitializerExpression<TListSyntax, TListItemSyntax> service,
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
                //result.Add(await GetWrapLongGroupAsync().ConfigureAwait(false));
            }

            private async Task<WrappingGroup> GetWrapEveryGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_every_item;

                var codeActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

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

            private SyntaxTrivia GetIndentationTrivia(WrappingStyle wrappingStyle)
            {
                return wrappingStyle == WrappingStyle.UnwrapFirst_AlignRest
                    ? _afterOpenTokenIndentationTrivia
                    : _singleIndentationTrivia;
            }

            private string GetNestedCodeActionTitle(WrappingStyle wrappingStyle)
                => wrappingStyle switch
                {
                    WrappingStyle.WrapFirst_IndentRest => Wrapper.Indent_all_items,
                    _ => throw ExceptionUtilities.UnexpectedValue(wrappingStyle),
                };

            private ImmutableArray<Edit> GetWrapEachEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

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

                result.Add(Edit.UpdateBetween(_listItems.Last(), NewLineTrivia, _elasticTrivia, _listSyntax.GetLastToken()));

                return result.ToImmutableAndFree();
            }

            private void AddTextChangeBetweenOpenAndFirstItem(
                WrappingStyle wrappingStyle, ArrayBuilder<Edit> result)
            {
                result.Add(wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? Edit.UpdateBetween(_listSyntax.GetFirstToken(), NewLineTrivia, _singleIndentationTrivia, _listItems[0])
                    : Edit.DeleteBetween(_listSyntax.GetFirstToken(), _listItems[0]));
            }

            private async Task<WrappingGroup> GetUnwrapGroupAsync()
            {
                var unwrapActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                var parentTitle = Wrapper.Unwrap_list;

                // MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(isInlinable: true, unwrapActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(string parentTitle, WrappingStyle wrappingStyle)
            {
                var edits = GetUnwrapAllEdits(wrappingStyle);
                var title = Wrapper.Unwrap_all_items;

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
        }
    }
}
