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
using Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping
{
    internal abstract partial class AbstractSeparatedListWrapper<TListSyntax, TListItemSyntax>
    {
        protected abstract class AbstractSeparatedListCodeComputer<TWrapper> : AbstractCodeActionComputer<TWrapper>
            where TWrapper : AbstractSeparatedListWrapper<TListSyntax, TListItemSyntax>
        {
            protected readonly TListSyntax _listSyntax;
            protected readonly SeparatedSyntaxList<TListItemSyntax> _listItems;

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
            protected readonly SyntaxTrivia _afterOpenTokenIndentationTrivia;

            /// <summary>
            /// Indentation amount for any items that have been wrapped to a new line.  Valid if we're
            /// not aligning with the first item. i.e.
            /// 
            ///     void Goobar(
            ///         ^
            ///         |
            /// </summary>
            protected readonly SyntaxTrivia _singleIndentationTrivia;

            public AbstractSeparatedListCodeComputer(
                TWrapper service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems,
                CancellationToken cancellationToken)
                : base(service, document, sourceText, options, cancellationToken)
            {
                _listSyntax = listSyntax;
                _listItems = listItems;

                var generator = SyntaxGenerator.GetGenerator(OriginalDocument);

                _afterOpenTokenIndentationTrivia = generator.Whitespace(GetAfterOpenTokenIdentation());
                _singleIndentationTrivia = generator.Whitespace(GetSingleIdentation());
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

                return GetIndentationAfter(openToken, FormattingOptions.IndentStyle.Smart);
            }

            protected void AddTextChangeBetweenOpenAndFirstItem(
                WrappingStyle wrappingStyle, ArrayBuilder<Edit> result)
            {
                result.Add(wrappingStyle == WrappingStyle.WrapFirst_IndentRest
                    ? Edit.UpdateBetween(_listSyntax.GetFirstToken(), NewLineTrivia, _singleIndentationTrivia, _listItems[0])
                    : Edit.DeleteBetween(_listSyntax.GetFirstToken(), _listItems[0]));
            }

            protected abstract string GetNestedCodeActionTitle(WrappingStyle wrappingStyle);

            protected async Task AddWrappingGroupsAsync(ArrayBuilder<WrappingGroup> result)
            {
                result.Add(await GetWrapEveryGroupAsync().ConfigureAwait(false));
                result.Add(await GetUnwrapGroupAsync().ConfigureAwait(false));
                result.Add(await GetWrapLongGroupAsync().ConfigureAwait(false));
            }

            #region unwrap group

            protected abstract Task<WrappingGroup> GetUnwrapGroupAsync();

            protected abstract Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(string parentTitle, WrappingStyle wrappingStyle);

            protected abstract ImmutableArray<Edit> GetUnwrapAllEdits(WrappingStyle wrappingStyle);

            // This computes edits for the content of the list excluding the opening token
            protected ImmutableArray<Edit> GetSeparatedListEdits(WrappingStyle wrappingStyle)
            {
                using var _ = ArrayBuilder<Edit>.GetInstance(out var result);

                AddTextChangeBetweenOpenAndFirstItem(wrappingStyle, result);

                foreach (var comma in _listItems.GetSeparators())
                {
                    result.Add(Edit.DeleteBetween(comma.GetPreviousToken(), comma));
                    result.Add(Edit.DeleteBetween(comma, comma.GetNextToken()));
                }

                result.Add(Edit.DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));
                return result.ToImmutableAndClear();
            }

            #endregion

            #region wrap long line

            protected abstract Task<WrappingGroup> GetWrapLongGroupAsync();

            protected Task<WrapItemsAction> GetWrapLongLineCodeActionAsync(
                string parentTitle, WrappingStyle wrappingStyle)
            {
                var indentationTrivia = GetIndentationTrivia(wrappingStyle);

                var edits = GetWrapLongLinesEdits(wrappingStyle, indentationTrivia);
                var title = GetNestedCodeActionTitle(wrappingStyle);

                return TryCreateCodeActionAsync(edits, parentTitle, title);
            }

            protected abstract ImmutableArray<Edit> GetWrapLongLinesEdits(
                WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia);

            #endregion

            #region Wrap every

            protected abstract Task<WrappingGroup> GetWrapEveryGroupAsync();

            protected Task<WrapItemsAction> GetWrapEveryNestedCodeActionAsync(
                string parentTitle, WrappingStyle wrappingStyle)
            {
                var indentationTrivia = GetIndentationTrivia(wrappingStyle);

                var edits = GetWrapEachEdits(wrappingStyle, indentationTrivia);
                var title = GetNestedCodeActionTitle(wrappingStyle);

                return TryCreateCodeActionAsync(edits, parentTitle, title);
            }

            protected abstract ImmutableArray<Edit> GetWrapEachEdits(WrappingStyle wrappingStyle, SyntaxTrivia indentationTrivia);

            private SyntaxTrivia GetIndentationTrivia(WrappingStyle wrappingStyle)
            {
                return wrappingStyle == WrappingStyle.UnwrapFirst_AlignRest
                    ? _afterOpenTokenIndentationTrivia
                    : _singleIndentationTrivia;
            }

            #endregion
        }
    }
}
