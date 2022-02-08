// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping.InitializerExpression
{
    internal abstract partial class AbstractInitializerExpressionWrapper<TListSyntax, TListItemSyntax>
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// </summary>
        private sealed class InitializerExpressionCodeActionComputer : AbstractSeparatedListCodeComputer<AbstractInitializerExpressionWrapper<TListSyntax, TListItemSyntax>>
        {
            private readonly SyntaxTrivia _braceIndentationTrivia;
            private readonly bool _doMoveOpenBraceToNewLine;

            public InitializerExpressionCodeActionComputer(
                AbstractInitializerExpressionWrapper<TListSyntax, TListItemSyntax> service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems,
                bool doMoveOpenBraceToNewLine, CancellationToken cancellationToken)
                : base(service, document, sourceText, options, listSyntax, listItems, cancellationToken)
            {
                var generator = SyntaxGenerator.GetGenerator(OriginalDocument);

                _braceIndentationTrivia = generator.Whitespace(GetBraceTokenIndentation());
                _doMoveOpenBraceToNewLine = doMoveOpenBraceToNewLine;
            }

            private string GetBraceTokenIndentation()
            {
                var previousToken = _listSyntax.GetFirstToken().GetPreviousToken();

                // Block indentation is the only style that correctly indents across all initializer expressions
                return GetIndentationAfter(previousToken, Formatting.FormattingOptions.IndentStyle.Block);
            }

            protected sealed override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync()
            {
                using var _ = ArrayBuilder<WrappingGroup>.GetInstance(out var result);
                await AddWrappingGroupsAsync(result).ConfigureAwait(false);
                return result.ToImmutableAndClear();
            }

            protected sealed override ImmutableArray<Edit> GetWrapEachEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_doMoveOpenBraceToNewLine)
                {
                    result.Add(Edit.UpdateBetween(_listSyntax.GetFirstToken().GetPreviousToken(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetFirstToken()));
                }

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

                result.Add(Edit.UpdateBetween(_listItems.Last(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetLastToken()));

                return result.ToImmutableAndClear();
            }

            protected sealed override ImmutableArray<Edit> GetUnwrapAllEdits(WrappingStyle wrappingStyle)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_doMoveOpenBraceToNewLine)
                {
                    result.Add(Edit.DeleteBetween(_listSyntax.GetFirstToken().GetPreviousToken(), _listSyntax.GetFirstToken()));
                }

                result.AddRange(GetSeparatedListEdits(wrappingStyle));

                return result.ToImmutableAndClear();
            }

            protected sealed override ImmutableArray<Edit> GetWrapLongLinesEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                if (_doMoveOpenBraceToNewLine)
                {
                    result.Add(Edit.UpdateBetween(_listSyntax.GetFirstToken().GetPreviousToken(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetFirstToken()));
                }

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
                    currentOffset += nextToken.Span.Length;
                }

                result.Add(Edit.UpdateBetween(_listItems.Last(), NewLineTrivia, _braceIndentationTrivia, _listSyntax.GetLastToken()));

                return result.ToImmutableAndClear();
            }

            protected sealed override string GetNestedCodeActionTitle(WrappingStyle wrappingStyle)
                => wrappingStyle switch
                {
                    WrappingStyle.WrapFirst_IndentRest => Wrapper.Indent_all_items,
                    WrappingStyle.UnwrapFirst_IndentRest => Wrapper.Unwrap_all_items,
                    _ => throw ExceptionUtilities.UnexpectedValue(wrappingStyle),
                };

            protected sealed override async Task<WrappingGroup> GetWrapEveryGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_every_item;

                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                codeActions.Add(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                return new WrappingGroup(isInlinable: false, codeActions.ToImmutableAndClear());
            }

            protected sealed override async Task<WrappingGroup> GetUnwrapGroupAsync()
            {
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var unwrapActions);

                var parentTitle = Wrapper.Unwrap_list;
                unwrapActions.Add(await GetUnwrapAllCodeActionAsync(parentTitle, WrappingStyle.UnwrapFirst_IndentRest).ConfigureAwait(false));

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(isInlinable: true, unwrapActions.ToImmutableAndClear());
            }

            protected sealed override Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(string parentTitle, WrappingStyle wrappingStyle)
            {
                var edits = GetUnwrapAllEdits(wrappingStyle);
                var title = GetNestedCodeActionTitle(wrappingStyle);

                return TryCreateCodeActionAsync(edits, parentTitle, title);
            }

            protected sealed override async Task<WrappingGroup> GetWrapLongGroupAsync()
            {
                var parentTitle = Wrapper.Wrap_long_list;
                using var _ = ArrayBuilder<WrapItemsAction>.GetInstance(out var codeActions);

                codeActions.Add(await GetWrapLongLineCodeActionAsync(
                    parentTitle, WrappingStyle.WrapFirst_IndentRest).ConfigureAwait(false));

                return new WrappingGroup(isInlinable: false, codeActions.ToImmutableAndClear());
            }
        }
    }
}
