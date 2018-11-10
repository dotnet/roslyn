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

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract partial class AbstractWrappingCodeRefactoringProvider<
        TListSyntax,
        TListItemSyntax>
    {
        private class CodeActionComputer
        {
            private readonly AbstractWrappingCodeRefactoringProvider<TListSyntax, TListItemSyntax> _service;
            private readonly Document _originalDocument;
            private readonly DocumentOptionSet _options;
            private readonly TListSyntax _listSyntax;
            private readonly SeparatedSyntaxList<TListItemSyntax> _listItems;

            private readonly bool _useTabs;
            private readonly int _tabSize;
            private readonly string _newLine;
            private readonly int _wrappingColumn;

            private readonly ISynchronousIndentationService _indentationService;

            private SourceText _originalSourceText;
            private string _singleIndentionOpt;
            private string _afterOpenTokenIndentation;

            public CodeActionComputer(
                AbstractWrappingCodeRefactoringProvider<TListSyntax, TListItemSyntax> service,
                Document document, DocumentOptionSet options,
                TListSyntax listSyntax, SeparatedSyntaxList<TListItemSyntax> listItems)
            {
                _service = service;
                _originalDocument = document;
                _options = options;
                _listSyntax = listSyntax;
                _listItems = listItems;

                _useTabs = options.GetOption(FormattingOptions.UseTabs);
                _tabSize = options.GetOption(FormattingOptions.TabSize);
                _newLine = options.GetOption(FormattingOptions.NewLine);
                _wrappingColumn = options.GetOption(FormattingOptions.PreferredWrappingColumn);

                _indentationService = service.GetIndentationService();
            }

            private static TextChange DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
                => UpdateBetween(left, right, "");

            private static TextChange UpdateBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right, string text)
                => new TextChange(TextSpan.FromBounds(left.Span.End, right.Span.Start), text);

            private void AddTextChangeBetweenOpenAndFirstItem(bool indentFirst, ArrayBuilder<TextChange> result)
            {
                result.Add(indentFirst
                    ? UpdateBetween(_listSyntax.GetFirstToken(), _listItems[0], _newLine + _singleIndentionOpt)
                    : DeleteBetween(_listSyntax.GetFirstToken(), _listItems[0]));
            }

            public async Task<ImmutableArray<CodeAction>> DoAsync(CancellationToken cancellationToken)
            {
                _originalSourceText = await _originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                _afterOpenTokenIndentation = GetAfterOpenTokenIdentation(cancellationToken);
                _singleIndentionOpt = TryGetSingleIdentation(cancellationToken);

                return await GetTopLevelCodeActionsAsync(cancellationToken).ConfigureAwait(false);
            }

            private string GetAfterOpenTokenIdentation(CancellationToken cancellationToken)
            {
                var openToken = _listSyntax.GetFirstToken();
                var afterOpenTokenOffset = _originalSourceText.GetOffset(openToken.Span.End);

                var indentString = afterOpenTokenOffset.CreateIndentationString(_useTabs, _tabSize);
                return indentString;
            }

            private string TryGetSingleIdentation(CancellationToken cancellationToken)
            {
                // Insert a newline after the open token of the list.  Then ask the
                // ISynchronousIndentationService where it thinks that the next line should be
                // indented.
                var openToken = _listSyntax.GetFirstToken();

                var newSourceText = _originalSourceText.WithChanges(new TextChange(new TextSpan(openToken.Span.End, 0), _newLine));
                newSourceText = newSourceText.WithChanges(
                    new TextChange(TextSpan.FromBounds(openToken.Span.End + _newLine.Length, newSourceText.Length), ""));
                var newDocument = _originalDocument.WithText(newSourceText);

                var originalLineNumber = newSourceText.Lines.GetLineFromPosition(openToken.Span.Start).LineNumber;
                var desiredIndentation = _indentationService.GetDesiredIndentation(
                    newDocument, originalLineNumber + 1, cancellationToken);

                if (desiredIndentation == null)
                {
                    return null;
                }

                var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.Value.BasePosition);
                var baseOffsetInLine = desiredIndentation.Value.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + desiredIndentation.Value.Offset;

                var indentString = indent.CreateIndentationString(_useTabs, _tabSize);
                return indentString;
            }

            private string GetIndentationString(bool indentFirst, bool alignWithFirst)
            {
                if (indentFirst)
                {
                    return _singleIndentionOpt;
                }

                if (!alignWithFirst)
                {
                    return _singleIndentionOpt;
                }

                return _afterOpenTokenIndentation;
            }

            private async Task<CodeAction> CreateCodeActionAsync(
                HashSet<string> seenDocuments, ImmutableArray<TextChange> edits,
                string parentTitle, string title, CancellationToken cancellationToken)
            {
                if (edits.Length == 0)
                {
                    return null;
                }

                var finalEdits = ArrayBuilder<TextChange>.GetInstance();

                try
                {
                    foreach (var edit in edits)
                    {
                        var text = _originalSourceText.ToString(edit.Span);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // editing some piece of non-whitespace trivia.  We don't support this.
                            return null;
                        }

                        // Make sure we're not about to make an edit that just changes the code to what
                        // is already there.
                        if (text != edit.NewText)
                        {
                            finalEdits.Add(edit);
                        }
                    }

                    if (finalEdits.Count == 0)
                    {
                        return null;
                    }

                    var newSourceText = _originalSourceText.WithChanges(finalEdits);
                    var newDocument = _originalDocument.WithText(newSourceText);

                    var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newOpenToken = newRoot.FindToken(_listSyntax.SpanStart);
                    var newList = newOpenToken.Parent;

                    var formattedDocument = await Formatter.FormatAsync(
                        newDocument, newList.Span, cancellationToken: cancellationToken).ConfigureAwait(false);

                    // make sure we've actually made a textual change.
                    var finalSourceText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var originalText = _originalSourceText.ToString();
                    var finalText = finalSourceText.ToString();

                    if (!seenDocuments.Add(finalText) ||
                        originalText == finalText)
                    {
                        return null;
                    }

                    return new WrapItemsAction(title, parentTitle, _ => Task.FromResult(formattedDocument));
                }
                finally
                {
                    finalEdits.Free();
                }
            }

            private async Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync(CancellationToken cancellationToken)
            {
                var codeActions = ArrayBuilder<CodeAction>.GetInstance();
                var seenDocuments = new HashSet<string>();

                codeActions.AddIfNotNull(await GetWrapEveryTopLevelCodeActionAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                codeActions.AddIfNotNull(await GetUnwrapAllTopLevelCodeActionsAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                codeActions.AddIfNotNull(await GetWrapLongTopLevelCodeActionAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                return SortActionsByMRU(codeActions.ToImmutableAndFree());
            }

            #region unwrap all

            private async Task<CodeAction> GetUnwrapAllTopLevelCodeActionsAsync(
                HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var unwrapActions = ArrayBuilder<CodeAction>.GetInstance();

                var parentTitle = string.Format(FeaturesResources.Unwrap_0, _service.ListName);

                // 1. Unwrap:
                //      MethodName(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                //
                // 2. Unwrap with indent:
                //      MethodName(
                //          int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(seenDocuments, parentTitle, indentFirst: false, cancellationToken).ConfigureAwait(false));
                unwrapActions.AddIfNotNull(await GetUnwrapAllCodeActionAsync(seenDocuments, parentTitle, indentFirst: true, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMRU(unwrapActions.ToImmutableAndFree());
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
                if (indentFirst && _singleIndentionOpt == null)
                {
                    return null;
                }

                var edits = GetUnwrapAllEdits(indentFirst);
                var title = indentFirst
                    ? string.Format(FeaturesResources.Unwrap_and_indent_all_0, _service.ItemNamePlural)
                    : string.Format(FeaturesResources.Unwrap_all_0, _service.ItemNamePlural);
                
                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<TextChange> GetUnwrapAllEdits(bool indentFirst)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

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
                var parentTitle = string.Format(FeaturesResources.Wrap_long_0, _service.ListName);
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

                var sorted = SortActionsByMRU(codeActions.ToImmutableAndFree());
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
                if (indentation == null)
                {
                    return null;
                }

                var edits = GetWrapLongLinesEdits(indentFirst, indentation);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<TextChange> GetWrapLongLinesEdits(bool indentFirst, string indentation)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

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
                        if (currentOffset < _wrappingColumn)
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
                            result.Add(UpdateBetween(itemsAndSeparators[i - 1], item, _newLine + indentation));
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
                var parentTitle = string.Format(FeaturesResources.Wrap_every_0, _service.ItemNameSingular);

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

                var sorted = SortActionsByMRU(codeActions.ToImmutableAndFree());
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
                if (indentation == null)
                {
                    return null;
                }

                var edits = GetWrapEachEdits(indentFirst, indentation);
                var title = GetNestedCodeActionTitle(indentFirst, alignWithFirst);

                return await CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken).ConfigureAwait(false);
            }

            private string GetNestedCodeActionTitle(bool indentFirst, bool alignWithFirst)
            {
                return indentFirst
                    ? string.Format(FeaturesResources.Indent_all_0, _service.ItemNamePlural)
                    : alignWithFirst
                        ? string.Format(FeaturesResources.Align_wrapped_0, _service.ItemNamePlural)
                        : string.Format(FeaturesResources.Indent_wrapped_0, _service.ItemNamePlural);
            }

            private ImmutableArray<TextChange> GetWrapEachEdits(bool indentFirst, string indentation)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

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
                        result.Add(UpdateBetween(comma, itemsAndSeparators[i + 2], _newLine + indentation));
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
