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

namespace Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList
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

            private readonly IBlankLineIndentationService _indentationService;

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
            private SyntaxTrivia _afterOpenTokenIndentationTrivia;

            /// <summary>
            /// Indentation amount for any items that have been wrapped to a new line.  Valid if we're
            /// not aligning with the first item. i.e.
            /// 
            ///     void Goobar(
            ///         ^
            ///         |
            /// </summary>
            private SyntaxTrivia _singleIndentationTrivia;

            public SeparatedSyntaxListCodeActionComputer(
                AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax> service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems)
                : base(service, document, sourceText, options)
            {
                _listSyntax = listSyntax;
                _listItems = listItems;

                _indentationService = service.GetIndentationService();
            }

            private void AddTextChangeBetweenOpenAndFirstItem(
                bool indentFirst, ArrayBuilder<Edit> result)
            {
                result.Add(indentFirst
                    ? UpdateBetween(_listSyntax.GetFirstToken(), NewLineTrivia, _listItems[0], _singleIndentationTrivia)
                    : DeleteBetween(_listSyntax.GetFirstToken(), _listItems[0]));
            }

            private string GetAfterOpenTokenIdentation(CancellationToken cancellationToken)
            {
                var openToken = _listSyntax.GetFirstToken();
                var afterOpenTokenOffset = OriginalSourceText.GetOffset(openToken.Span.End);

                var indentString = afterOpenTokenOffset.CreateIndentationString(UseTabs, TabSize);
                return indentString;
            }

            private string GetSingleIdentation(CancellationToken cancellationToken)
            {
                // Insert a newline after the open token of the list.  Then ask the
                // ISynchronousIndentationService where it thinks that the next line should be
                // indented.
                var openToken = _listSyntax.GetFirstToken();

                var newSourceText = OriginalSourceText.WithChanges(new TextChange(new TextSpan(openToken.Span.End, 0), NewLine));
                newSourceText = newSourceText.WithChanges(
                    new TextChange(TextSpan.FromBounds(openToken.Span.End + NewLine.Length, newSourceText.Length), ""));
                var newDocument = OriginalDocument.WithText(newSourceText);

                var originalLineNumber = newSourceText.Lines.GetLineFromPosition(openToken.Span.Start).LineNumber;
                var desiredIndentation = _indentationService.GetBlankLineIndentation(
                    newDocument, originalLineNumber + 1,
                    FormattingOptions.IndentStyle.Smart, cancellationToken);

                var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition);
                var baseOffsetInLine = desiredIndentation.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + desiredIndentation.Offset;

                var indentString = indent.CreateIndentationString(UseTabs, TabSize);
                return indentString;
            }

            private SyntaxTrivia GetIndentationTrivia(bool indentFirst, bool alignWithFirst)
            {
                if (indentFirst)
                {
                    return _singleIndentationTrivia;
                }

                if (!alignWithFirst)
                {
                    return _singleIndentationTrivia;
                }

                return _afterOpenTokenIndentationTrivia;
            }

            protected override async Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync(CancellationToken cancellationToken)
            {
                var result = ArrayBuilder<WrappingGroup>.GetInstance();
                await AddTopLevelCodeActionsAsync(result, cancellationToken).ConfigureAwait(false);
                return result.ToImmutableAndFree();
            }

            private async Task AddTopLevelCodeActionsAsync(
                ArrayBuilder<WrappingGroup> result, CancellationToken cancellationToken)
            {
                var generator = SyntaxGenerator.GetGenerator(this.OriginalDocument);

                _afterOpenTokenIndentationTrivia = generator.Whitespace(GetAfterOpenTokenIdentation(cancellationToken));
                _singleIndentationTrivia = generator.Whitespace(GetSingleIdentation(cancellationToken));

                result.Add(await GetWrapEveryTopLevelCodeActionAsync(cancellationToken).ConfigureAwait(false));
                result.Add(await GetUnwrapAllTopLevelCodeActionsAsync(cancellationToken).ConfigureAwait(false));
                result.Add(await GetWrapLongTopLevelCodeActionAsync(cancellationToken).ConfigureAwait(false));
            }

            #region unwrap all

            private async Task<WrappingGroup> GetUnwrapAllTopLevelCodeActionsAsync(CancellationToken cancellationToken)
            {
                var unwrapActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                var parentTitle = string.Format(FeaturesResources.Unwrap_0, Wrapper.ListName);

                // 1. Unwrap:
                //      MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                //
                // 2. Unwrap with indent:
                //      MethodName(
                //          int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(parentTitle, indentFirst: false, cancellationToken).ConfigureAwait(false));
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(parentTitle, indentFirst: true, cancellationToken).ConfigureAwait(false));

                // The 'unwrap' title strings are unique and do not collide with any other code
                // actions we're computing.  So they can be inlined if possible.
                return new WrappingGroup(parentTitle, isInlinable: true, unwrapActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetUnwrapAllCodeActionAsync(
                string parentTitle, bool indentFirst, CancellationToken cancellationToken)
            {
                var edits = GetUnwrapAllEdits(indentFirst);
                var title = indentFirst
                    ? string.Format(FeaturesResources.Unwrap_and_indent_all_0, Wrapper.ItemNamePlural)
                    : string.Format(FeaturesResources.Unwrap_all_0, Wrapper.ItemNamePlural);
                
                return await TryCreateCodeActionAsync(
                    edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<Edit> GetUnwrapAllEdits(bool indentFirst)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                foreach (var comma in _listItems.GetSeparators())
                {
                    result.Add(DeleteBetween(comma.GetPreviousToken(), comma));
                    result.Add(DeleteBetween(comma, comma.GetNextToken()));
                }

                result.Add(DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));
                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap long line

            private async Task<WrappingGroup> GetWrapLongTopLevelCodeActionAsync(CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_long_0, Wrapper.ListName);
                var codeActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                // Wrap at long length, align with first item:
                //      MethodName(int a, int b, int c,
                //                 int d, int e, int f,
                //                 int g, int h, int i,
                //                 int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, indent all items:
                //      MethodName(
                //          int a, int b, int c, int d, int e,
                //          int f, int g, int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, indent wrapped items:
                //      MethodName(int a, int b, int c, 
                //          int d, int e, int f, int g,
                //          int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

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

                return new WrappingGroup(parentTitle, isInlinable: false, codeActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetWrapLongLineCodeActionAsync(
                string parentTitle, bool indentFirst, bool alignWithFirst, CancellationToken cancellationToken)
            {
                var indentationTrivia = GetIndentationTrivia(indentFirst, alignWithFirst);

                var edits = GetWrapLongLinesEdits(indentFirst, indentationTrivia);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await TryCreateCodeActionAsync(
                    edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<Edit> GetWrapLongLinesEdits(
                bool indentFirst, SyntaxTrivia indentationTrivia)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                var currentOffset = indentFirst
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
                            result.Add(UpdateBetween(itemsAndSeparators[i - 1], SingleWhitespaceTrivia, item, NoTrivia));
                            currentOffset += " ".Length;
                        }
                        else
                        {
                            // not the first item on the line and this item makes us go past the wrapping
                            // limit.  We want to wrap before this item.
                            result.Add(UpdateBetween(itemsAndSeparators[i - 1], NewLineTrivia, item, indentationTrivia));
                            currentOffset = indentationTrivia.FullWidth() + item.Span.Length;
                        }
                    }

                    // Get rid of any spaces between the list item and the following token (a
                    // comma or close token).
                    var nextToken = item.GetLastToken().GetNextToken();

                    result.Add(DeleteBetween(item, nextToken));
                    currentOffset += nextToken.Span.Length;
                }

                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap every

            private async Task<WrappingGroup> GetWrapEveryTopLevelCodeActionAsync(CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_every_0, Wrapper.ItemNameSingular);

                var codeActions = ArrayBuilder<WrapItemsAction>.GetInstance();

                // Wrap each item, align with first item
                //      MethodName(int a,
                //                 int b,
                //                 ...
                //                 int j);
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap each item, indent all items
                //      MethodName(
                //          int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap each item. indent wrapped items:
                //      MethodName(int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

                // See comment in GetWrapLongTopLevelCodeActionAsync for explanation of why we're
                // not inlineable.
                return new WrappingGroup(parentTitle, isInlinable: false, codeActions.ToImmutableAndFree());
            }

            private async Task<WrapItemsAction> GetWrapEveryNestedCodeActionAsync(
                string parentTitle, bool indentFirst, bool alignWithFirst, CancellationToken cancellationToken)
            {
                var indentationTrivia = GetIndentationTrivia(indentFirst, alignWithFirst);

                var edits = GetWrapEachEdits(indentFirst, indentationTrivia);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await TryCreateCodeActionAsync(
                    edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private string GetNestedCodeActionTitle(bool indentFirst, bool alignWithFirst)
            {
                return indentFirst
                    ? string.Format(FeaturesResources.Indent_all_0, Wrapper.ItemNamePlural)
                    : alignWithFirst
                        ? string.Format(FeaturesResources.Align_wrapped_0, Wrapper.ItemNamePlural)
                        : string.Format(FeaturesResources.Indent_wrapped_0, Wrapper.ItemNamePlural);
            }

            private ImmutableArray<Edit> GetWrapEachEdits(
                bool indentFirst, SyntaxTrivia indentationTrivia)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                var itemsAndSeparators = _listItems.GetWithSeparators();

                for (var i = 0; i < itemsAndSeparators.Count; i += 2)
                {
                    var item = itemsAndSeparators[i].AsNode();
                    if (i < itemsAndSeparators.Count - 1)
                    {
                        // intermediary item
                        var comma = itemsAndSeparators[i + 1].AsToken();
                        result.Add(DeleteBetween(item, comma));

                        // Always wrap between this comma and the next item.
                        result.Add(UpdateBetween(
                            comma, NewLineTrivia, itemsAndSeparators[i + 2], indentationTrivia));
                    }
                }

                // last item.  Delete whatever is between it and the close token of the list.
                result.Add(DeleteBetween(_listItems.Last(), _listSyntax.GetLastToken()));

                return result.ToImmutableAndFree();
            }

            #endregion
        }
    }
}
