// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpParameterWrappingCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static ImmutableArray<string> s_mruTitles = ImmutableArray<string>.Empty;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var span = context.Span;
            if (!span.IsEmpty)
            {
                return;
            }

            var position = span.Start;
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (!token.Span.Contains(position))
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var generator = document.GetLanguageService<SyntaxGenerator>();
            var declaration = token.Parent.GetAncestors()
                                          .FirstOrDefault(n => generator.GetParameterListNode(n) != null);

            if (declaration == null)
            {
                return;
            }

            var parameterList = generator.GetParameterListNode(declaration) as BaseParameterListSyntax;
            if (parameterList == null)
            {
                return;
            }

            // Make sure we don't have any syntax errors here.  Don't want to format if we don't
            // really understand what's going on.
            if (parameterList.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            var attributes = generator.GetAttributes(declaration);

            // We want to offer this feature in the header of the member.  For now, we consider
            // the header to be the part after the attributes, to the end of the parameter list.
            var firstToken = attributes?.Count > 0
                ? attributes.Last().GetLastToken().GetNextToken()
                : declaration.GetFirstToken();

            var lastToken = parameterList.GetLastToken();

            var headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
            if (!headerSpan.IntersectsWith(position))
            {
                return;
            }

            var parameters = generator.GetParameters(declaration);
            if (parameters.Count <= 1)
            {
                // nothing to do with 0-1 parameters.  Simple enough for users to just edit
                // themselves, and this prevents constant clutter with formatting that isn't
                // really that useful.
                return;
            }

            // For now, don't offer if any parameter spans multiple lines.  We'll very likely screw
            // up formatting badly.  If this is really important to support, we can put in the
            // effort to properly move multi-line items around (which would involve properly fixing
            // up the indentation of lines within them.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var parameter in parameters)
            {
                if (parameter.Span.IsEmpty ||
                    !sourceText.AreOnSameLine(parameter.GetFirstToken(), parameter.GetLastToken()))
                {
                    return;
                }
            }

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var computer = new CodeActionComputer(document, options, parameterList);
            var codeActions = await computer.DoAsync(cancellationToken).ConfigureAwait(false);

            context.RegisterRefactorings(codeActions);
        }

        private static ImmutableArray<CodeAction> SortActionsByMRU(ImmutableArray<CodeAction> codeActions)
        {
            // make a local so this array can't change out from under us.
            var mruTitles = s_mruTitles;
            return codeActions.Sort((ca1, ca2) =>
            {
                var titleIndex1 = mruTitles.IndexOf(GetSortTitle(ca1));
                var titleIndex2 = mruTitles.IndexOf(GetSortTitle(ca2));

                if (titleIndex1 >= 0 && titleIndex2 >= 0)
                {
                    // we've invoked both of these before.  Order by how recently it was invoked.
                    return titleIndex1 - titleIndex2;
                }

                // one of these has never been invoked.  It's always after an item that has been
                // invoked.
                if (titleIndex1 >= 0)
                {
                    return -1;
                }

                if (titleIndex2 >= 0)
                {
                    return 1;
                }

                // Neither of these has been invoked.   Keep it in the same order we found it in the
                // array.  Note: we cannot return 0 here as ImmutableArray/Array are not guaranteed
                // to sort stably.
                return codeActions.IndexOf(ca1) - codeActions.IndexOf(ca2);
            });
        }

        private static string GetSortTitle(CodeAction codeAction)
            => (codeAction as MyCodeAction)?.SortTitle ?? codeAction.Title;

        private bool AllEditsAffectWhitespace(SourceText sourceText, ImmutableArray<TextChange> edits)
        {
            foreach (var edit in edits)
            {
                var text = sourceText.ToString(edit.Span);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }
            }

            return true;
        }

        private class MyCodeAction : DocumentChangeAction
        {
            private readonly string _parentTitle;

            public string SortTitle { get; }

            public MyCodeAction(string title, string parentTitle, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
                _parentTitle = parentTitle;
                SortTitle = parentTitle + "_" + title;
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                // For preview, we don't want to compute the normal operations.  Specifically, we don't
                // want to compute the stateful operation that tracks which code action was triggered.
                return base.ComputeOperationsAsync(cancellationToken);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var operations = await base.ComputeOperationsAsync(cancellationToken).ConfigureAwait(false);
                var operationsList = operations.ToList();

                operationsList.Add(new RecordCodeActionOperation(this.SortTitle, _parentTitle));
                return operationsList;
            }

            private class RecordCodeActionOperation : CodeActionOperation
            {
                private readonly string _sortTitle;
                private readonly string _parentTitle;

                public RecordCodeActionOperation(string sortTitle, string parentTitle)
                {
                    _sortTitle = sortTitle;
                    _parentTitle = parentTitle;
                }

                internal override bool ApplyDuringTests => false;

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    // Record both the sortTitle of the nested action and the tile of the parent
                    // action.  This way we any invocation of a code action helps prioritize both
                    // the parent lists and the nested lists.
                    s_mruTitles = s_mruTitles.Remove(_sortTitle).Remove(_parentTitle)
                                           .Insert(0, _sortTitle).Insert(0, _parentTitle);
                }
            }
        }

        private class CodeActionComputer
        {
            private readonly Document _originalDocument;
            private readonly DocumentOptionSet _options;
            private readonly BaseParameterListSyntax _parameterList;

            private readonly bool _useTabs;
            private readonly int _tabSize;
            private readonly string _newLine;
            private readonly int _wrappingColumn;

            private SourceText _originalSourceText;
            private string _singleIndentionOpt;
            private string _afterOpenTokenIndentation;

            public CodeActionComputer(
                Document document, DocumentOptionSet options,
                BaseParameterListSyntax parameterList)
            {
                _originalDocument = document;
                _options = options;
                _parameterList = parameterList;

                _useTabs = options.GetOption(FormattingOptions.UseTabs);
                _tabSize = options.GetOption(FormattingOptions.TabSize);
                _newLine = options.GetOption(FormattingOptions.NewLine);
                _wrappingColumn = options.GetOption(FormattingOptions.PreferredWrappingColumn);
            }

            private static void DeleteAroundCommas(BaseParameterListSyntax parameterList, ArrayBuilder<TextChange> result)
            {
                foreach (var comma in parameterList.Parameters.GetSeparators())
                {
                    result.Add(DeleteBetween(comma.GetPreviousToken(), comma));
                    result.Add(DeleteBetween(comma, comma.GetNextToken()));
                }
            }

            private static TextChange DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
                => UpdateBetween(left, right, "");

            private static TextChange UpdateBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right, string text)
                => new TextChange(TextSpan.FromBounds(left.Span.End, right.Span.Start), text);

            private void AddTextChangeBetweenOpenAndFirstItem(bool indentFirst, ArrayBuilder<TextChange> result)
            {
                result.Add(indentFirst
                    ? UpdateBetween(_parameterList.GetFirstToken(), _parameterList.Parameters[0], _newLine + _singleIndentionOpt)
                    : DeleteBetween(_parameterList.GetFirstToken(), _parameterList.Parameters[0]));
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
                var openToken = _parameterList.GetFirstToken();
                var afterOpenTokenOffset = _originalSourceText.GetOffset(openToken.Span.End);

                var indentString = afterOpenTokenOffset.CreateIndentationString(_useTabs, _tabSize);
                return indentString;
            }

            private string TryGetSingleIdentation(CancellationToken cancellationToken)
            {
                // Insert a newline after the open token of the parameter list.  Then ask the
                // ISynchronousIndentationService where it thinks that the next line should be indented.
                var openToken = _parameterList.GetFirstToken();

                var newSourceText = _originalSourceText.WithChanges(new TextChange(new TextSpan(openToken.Span.End, 0), _newLine));
                var newDocument = _originalDocument.WithText(newSourceText);

                var indentationService = newDocument.GetLanguageService<ISynchronousIndentationService>();
                var originalLineNumber = newSourceText.Lines.GetLineFromPosition(openToken.Span.Start).LineNumber;
                var desiredIndentation = indentationService.GetDesiredIndentation(
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
                    var newOpenToken = newRoot.FindToken(_parameterList.SpanStart);
                    var newParameterList = newOpenToken.Parent;

                    var formattedDocument = await Formatter.FormatAsync(
                        newDocument, newParameterList.Span, cancellationToken: cancellationToken).ConfigureAwait(false);

                    // make sure we've actually made a textual change.
                    var finalSourceText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var originalText = _originalSourceText.ToString();
                    var finalText = finalSourceText.ToString();

                    if (!seenDocuments.Add(finalText) ||
                        originalText == finalText)
                    {
                        return null;
                    }

                    return new MyCodeAction(title, parentTitle, _ => Task.FromResult(formattedDocument));
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

                codeActions.AddIfNotNull(await GetUnwrapTopLevelCodeActionsAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                codeActions.AddIfNotNull(await GetWrapLongTopLevelCodeActionAsync(
                    seenDocuments, cancellationToken).ConfigureAwait(false));

                return SortActionsByMRU(codeActions.ToImmutableAndFree());
            }

            private async Task<CodeAction> GetWrapLongTopLevelCodeActionAsync(
                HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_long_0, FeaturesResources.parameter_list);
                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                // Wrap at long length, indent all parameters:
                //      MethodName(
                //          int a, int b, int c, int d, int e,
                //          int f, int g, int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, indent wrapped parameters:
                //      MethodName(int a, int b, int c, 
                //          int d, int e, int f, int g,
                //          int h, int i, int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

                // Wrap at long length, align with first parameter:
                //      MethodName(int a, int b, int c,
                //                 int d, int e, int f,
                //                 int g, int h, int i,
                //                 int j)
                codeActions.AddIfNotNull(await GetWrapLongLineCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMRU(codeActions.ToImmutableAndFree());
                if (sorted.Length == 0)
                {
                    return null;
                }

                return new CodeActionWithNestedActions(parentTitle, sorted, isInlinable: false);
            }

            private async Task<CodeAction> GetWrapEveryTopLevelCodeActionAsync(HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var parentTitle = string.Format(FeaturesResources.Wrap_every_0, FeaturesResources.parameter);

                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                // Wrap each parameter, indent all parameters
                //      MethodName(
                //          int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: true, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                // Wrap each parameter. indent wrapped parameters:
                //      MethodName(int a,
                //          int b,
                //          ...
                //          int j)
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: false, cancellationToken).ConfigureAwait(false));

                // Wrap each parameter, align with first parameter
                //      MethodName(int a,
                //                 int b,
                //                 ...
                //                 int j);
                codeActions.AddIfNotNull(await GetWrapEveryNestedCodeActionAsync(
                    seenDocuments, parentTitle, indentFirst: false, alignWithFirst: true, cancellationToken).ConfigureAwait(false));

                var sorted = SortActionsByMRU(codeActions.ToImmutableAndFree());
                if (sorted.Length == 0)
                {
                    return null;
                }

                return new CodeActionWithNestedActions(parentTitle, sorted, isInlinable: false);
            }

            private async Task<CodeAction> GetUnwrapTopLevelCodeActionsAsync(
                HashSet<string> seenDocuments, CancellationToken cancellationToken)
            {
                var unwrapActions = ArrayBuilder<CodeAction>.GetInstance();

                var parentTitle = string.Format(FeaturesResources.Unwrap_0, FeaturesResources.parameter_list);

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

            #region unwrap all

            private Task<CodeAction> GetUnwrapAllCodeActionAsync(
                HashSet<string> seenDocuments, string parentTitle,
                bool indentFirst, CancellationToken cancellationToken)
            {
                if (indentFirst && _singleIndentionOpt == null)
                {
                    return null;
                }

                var edits = GetUnwrapAllEdits(indentFirst);
                var title = indentFirst
                    ? string.Format(FeaturesResources.Unwrap_and_indent_all_0, FeaturesResources.parameters)
                    : string.Format(FeaturesResources.Unwrap_all_0, FeaturesResources.parameters);
                
                return CreateCodeActionAsync(seenDocuments, edits, parentTitle, title, cancellationToken);
            }

            private ImmutableArray<TextChange> GetUnwrapAllEdits(bool indentFirst)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);
                DeleteAroundCommas(_parameterList, result);
                result.Add(DeleteBetween(_parameterList.Parameters.Last(), _parameterList.GetLastToken()));

                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap long line

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

            private Task<CodeAction> GetWrapLongLineCodeActionAsync(
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

                return CreateCodeActionAsync(seenDocuments, edits, parentTitle, title, cancellationToken);
            }

            private ImmutableArray<TextChange> GetWrapLongLinesEdits(bool indentFirst, string indentation)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                var currentOffset = indentation.Length;
                var parametersAndSeparators = _parameterList.Parameters.GetWithSeparators();

                for (var i = 0; i < parametersAndSeparators.Count; i += 2)
                {
                    var parameter = parametersAndSeparators[i].AsNode();
                    if (i < parametersAndSeparators.Count - 1)
                    {
                        // intermediary parameter
                        var comma = parametersAndSeparators[i + 1].AsToken();
                        result.Add(DeleteBetween(parameter, comma));

                        // Move past this parameter and comma.  Update our current offset accordingly.
                        // this ensures we always place at least one parameter before wrapping.
                        currentOffset += parameter.Span.Length + comma.Span.Length;
                         
                        if (currentOffset > _wrappingColumn)
                        {
                            // Current line is too long.  Wrap between this comma and the next item.
                            result.Add(UpdateBetween(comma, parametersAndSeparators[i + 2], _newLine + indentation));
                            currentOffset = indentation.Length;
                        }
                        else
                        {
                            result.Add(DeleteBetween(comma, parametersAndSeparators[i + 2]));
                        }
                    }
                    else
                    {
                        // last parameter.  Delete whatever is between it and the close token of the list.
                        result.Add(DeleteBetween(parameter, _parameterList.GetLastToken()));
                    }
                }

                return result.ToImmutableAndFree();
            }

            #endregion

            #region wrap each

            private Task<CodeAction> GetWrapEveryNestedCodeActionAsync(
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

                return CreateCodeActionAsync(
                    seenDocuments, edits, parentTitle, title, cancellationToken);
            }

            private static string GetNestedCodeActionTitle(bool indentFirst, bool alignWithFirst)
            {
                return indentFirst
                    ? string.Format(FeaturesResources.Indent_all_0, FeaturesResources.parameters)
                    : alignWithFirst
                        ? string.Format(FeaturesResources.Align_wrapped_0, FeaturesResources.parameters)
                        : string.Format(FeaturesResources.Indent_wrapped_0, FeaturesResources.parameters);
            }

            private ImmutableArray<TextChange> GetWrapEachEdits(bool indentFirst, string indentation)
            {
                var result = ArrayBuilder<TextChange>.GetInstance();

                AddTextChangeBetweenOpenAndFirstItem(indentFirst, result);

                var parametersAndSeparators = _parameterList.Parameters.GetWithSeparators();

                for (var i = 0; i < parametersAndSeparators.Count; i += 2)
                {
                    var parameter = parametersAndSeparators[i].AsNode();
                    if (i < parametersAndSeparators.Count - 1)
                    {
                        // intermediary parameter
                        var comma = parametersAndSeparators[i + 1].AsToken();
                        result.Add(DeleteBetween(parameter, comma));

                        // Always wrap between this comma and the next item.
                        result.Add(UpdateBetween(comma, parametersAndSeparators[i + 2], _newLine + indentation));
                    }
                    else
                    {
                        // last parameter.  Delete whatever is between it and the close token of the list.
                        result.Add(DeleteBetween(parameter, _parameterList.GetLastToken()));
                    }
                }

                return result.ToImmutableAndFree();
            }

            #endregion
        }
    }
}
