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
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpParameterWrappingCodeRefactoringProvider : CodeRefactoringProvider
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
    }
}
