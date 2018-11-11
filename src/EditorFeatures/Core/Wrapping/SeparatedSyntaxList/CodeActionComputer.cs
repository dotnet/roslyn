// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList
{
    internal abstract partial class AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// </summary>
        private class CodeActionComputer : AbstractComputer<AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax>>
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
            private string _afterOpenTokenIndentation;

            /// <summary>
            /// Indentation amount for any items that have been wrapped to a new line.  Valid if we're
            /// not aligning with the first item. i.e.
            /// 
            ///     void Goobar(
            ///         ^
            ///         |
            /// </summary>
            private string _singleIndention;

            public CodeActionComputer(
                AbstractSeparatedSyntaxListWrapper<TListSyntax, TListItemSyntax> service,
                Document document, SourceText sourceText, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems)
                : base(service, document, sourceText, options)
            {
                _listSyntax = listSyntax;
                _listItems = listItems;

                _indentationService = service.GetIndentationService();
            }

            private void AddTextChangeBetweenOpenAndFirstItem(bool indentFirst, ArrayBuilder<Edit> result)
            {
                result.Add(indentFirst
                    ? UpdateBetween(_listSyntax.GetFirstToken(), _listItems[0], NewLine + _singleIndention)
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

            private string GetIndentationString(bool indentFirst, bool alignWithFirst)
            {
                if (indentFirst)
                {
                    return _singleIndention;
                }

                if (!alignWithFirst)
                {
                    return _singleIndention;
                }

                return _afterOpenTokenIndentation;
            }

            //protected override async Task<TextSpan> GetSpanToFormatAsync(
            //    Document newDocument, CancellationToken cancellationToken)
            //{
            //    var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            //    var newOpenToken = newRoot.FindToken(_listSyntax.SpanStart);
            //    var newList = newOpenToken.Parent;
            //    var spanToFormat = newList.Span;
            //    return spanToFormat;
            //}

            protected override async Task AddTopLevelCodeActionsAsync(ArrayBuilder<CodeAction> codeActions, HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                _afterOpenTokenIndentation = GetAfterOpenTokenIdentation(cancellationToken);
                _singleIndention = GetSingleIdentation(cancellationToken);

                codeActions.AddIfNotNull(await GetWrapEveryTopLevelCodeActionAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                codeActions.AddIfNotNull(await GetUnwrapAllTopLevelCodeActionsAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                codeActions.AddIfNotNull(await GetWrapLongTopLevelCodeActionAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));
            }

            #region unwrap all

            private async Task<CodeAction> GetUnwrapAllTopLevelCodeActionsAsync(
                HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var unwrapActions = ArrayBuilder<CodeAction>.GetInstance();

                var parentTitle = string.Format(FeaturesResources.Unwrap_0, Service.ListName);

                // 1. Unwrap:
                //      MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                //
                // 2. Unwrap with indent:
                //      MethodName(
                //          int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(seenDocuments, parentTitle, indentFirst: false, cancellationToken).ConfigureAwait(false));
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(seenDocuments, parentTitle, indentFirst: true, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMostRecentlyUsed(unwrapActions.ToImmutableAndFree());
                if (sorted.Length == 0)
                {
                    return null;
                }

                return sorted.Length == 1
                    ? sorted[0]
                    : new CodeActionWithNestedActions(parentTitle, sorted, isInlinable: true);
            }

            private async Task<CodeAction> GetUnwrapAllCodeActionAsync(
                HashSet<string> seenDocuments, string parentTitle,
                bool indentFirst, CancellationToken cancellationToken)
            {
                var edits = GetUnwrapAllEdits(indentFirst);
                var title = indentFirst
                    ? string.Format(FeaturesResources.Unwrap_and_indent_all_0, Service.ItemNamePlural)
                    : string.Format(FeaturesResources.Unwrap_all_0, Service.ItemNamePlural);
                
                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private Task<CodeAction> CreateCodeActionAsync(HashSet<string> seenDocuments, ImmutableArray<Edit> edits, string parentTitle, string title, CancellationToken cancellationToken)
                => base.CreateCodeActionAsync(seenDocuments, _listSyntax, edits, parentTitle, title, cancellationToken);

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

            private async Task<CodeAction> GetWrapLongTopLevelCodeActionAsync(
                HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_long_0, Service.ListName);
                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                // Wrap at long length, align with first item:
                //      MethodName(int a, int b, int c,
                //                 int d, int e, int f,
                //                 int g, int h, int i,
                //                 int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, indent all items:
                //      MethodName(
                //          int a, int b, int c, int d, int e,
                //          int f, int g, int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, indent wrapped items:
                //      MethodName(int a, int b, int c, 
                //          int d, int e, int f, int g,
                //          int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMostRecentlyUsed(codeActions.ToImmutableAndFree());
                if (sorted.Length == 0)
                {
                    return null;
                }

                return new CodeActionWithNestedActions(parentTitle, sorted, isInlinable: false);
            }

            private async Task<CodeAction> GetWrapLongLineCodeActionAsync(
                HashSet<string> seenDocuments, string parentTitle, 
                bool indentFirst, bool alignWithFirst, CancellationToken cancellationToken)
            {
                var indentation = GetIndentationString(indentFirst, alignWithFirst);

                var edits = GetWrapLongLinesEdits(indentFirst, indentation);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<Edit> GetWrapLongLinesEdits(bool indentFirst, string indentation)
            {
                var result = ArrayBuilder<Edit>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                var currentOffset = indentFirst
                    ? indentation.Length
                    : _afterOpenTokenIndentation.Length;
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
                            result.Add(UpdateBetween(itemsAndSeparators[i - 1], item, " "));
                            currentOffset += " ".Length;
                        }
                        else
                        {
                            // not the first item on the line and this item makes us go past the wrapping
                            // limit.  We want to wrap before this item.
                            result.Add(UpdateBetween(itemsAndSeparators[i - 1], item, NewLine + indentation));
                            currentOffset = indentation.Length + item.Span.Length;
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

            private async Task<CodeAction> GetWrapEveryTopLevelCodeActionAsync(HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_every_0, Service.ItemNameSingular);

                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                // Wrap each item, align with first item
                //      MethodName(int a,
                //                 int b,
                //                 ...
                //                 int j);
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap each item, indent all items
                //      MethodName(
                //          int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap each item. indent wrapped items:
                //      MethodName(int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMostRecentlyUsed(codeActions.ToImmutableAndFree());
                if (sorted.Length == 0)
                {
                    return null;
                }

                return new CodeActionWithNestedActions(parentTitle, sorted, isInlinable: false);
            }

            private async Task<CodeAction> GetWrapEveryNestedCodeActionAsync(
                HashSet<string> seenDocuments, string parentTitle, 
                bool indentFirst, bool alignWithFirst, CancellationToken cancellationToken)
            {
                var indentation = GetIndentationString(indentFirst, alignWithFirst);

                var edits = GetWrapEachEdits(indentFirst, indentation);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private string GetNestedCodeActionTitle(bool indentFirst, bool alignWithFirst)
            {
                return indentFirst
                    ? string.Format(FeaturesResources.Indent_all_0, Service.ItemNamePlural)
                    : alignWithFirst
                        ? string.Format(FeaturesResources.Align_wrapped_0, Service.ItemNamePlural)
                        : string.Format(FeaturesResources.Indent_wrapped_0, Service.ItemNamePlural);
            }

            private ImmutableArray<Edit> GetWrapEachEdits(bool indentFirst, string indentation)
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
                        result.Add(UpdateBetween(comma, itemsAndSeparators[i + 2], NewLine + indentation));
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
