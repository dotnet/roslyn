// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider<TIfStatementSyntax> : CodeRefactoringProvider
        where TIfStatementSyntax : SyntaxNode
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var textSpan = context.Span;
            if (!textSpan.IsEmpty)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            var ifStatement = token.GetAncestor<TIfStatementSyntax>();
            if (ifStatement == null)
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            var headerSpan = GetHeaderSpan(ifStatement);
            if (!headerSpan.IntersectsWith(textSpan))
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            if (!CanInvert(ifStatement))
            {
                return;
            }

            var document = context.Document;
            SyntaxNode subsequentSingleExitPointOpt = null;

            InvertIfStyle invertIfStyle;
            if (IsElselessIfStatement(ifStatement))
            {
                invertIfStyle = GetInvertIfStyle(
                    ifStatement,
                    await document.GetSemanticModelAsync().ConfigureAwait(false),
                    ref subsequentSingleExitPointOpt);
            }
            else
            {
                invertIfStyle = InvertIfStyle.Normal;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitle(),
                    c => InvertIfAsync(document, ifStatement, invertIfStyle, subsequentSingleExitPointOpt, c)));
        }

        protected enum InvertIfStyle
        {
            // swap if and else
            Normal,
            // swap subsequent statements and if body
            SwapIfBodyWithSubsequentStatements,
            // move subsequent statements to if body
            MoveSubsequentStatementsToElseBody,
            // invert and generete else
            WithElseClause,
            // invert and generate else, keep if-body empty
            MoveIfBodyToElseClause,
            // invert and copy the exit point statement
            WithSubsequentExitPointStatement,
            // invert and generate return, break, continue
            WithNearmostJumpStatement,
            // just invert the condition
            WithNegatedCondition,
        }

        private InvertIfStyle GetInvertIfStyle(
            TIfStatementSyntax ifStatement,
            SemanticModel semanticModel,
            ref SyntaxNode subsequentSingleExitPointOpt)
        {
            if (IsEmptyIfBody(ifStatement))
            {
                // An empty if-statement: just negate the condition
                //  
                //  if (condition) { }
                //
                // ->
                //
                //  if (!condition) { }
                //
                return InvertIfStyle.WithNegatedCondition;
            }

            if (NoSubsequentStatements(ifStatement))
            {
                // No statements if-statement, return with nearmost jump-statement
                //
                //  void M() {
                //    if (condition) {
                //      Body();
                //    }
                //  }
                //
                // ->
                //
                //  void M() {
                //    if (!condition) {
                //      return;
                //    }
                //    Body();
                //  }
                //
                return InvertIfStyle.WithNearmostJumpStatement;
            }

            AnalyzeIfBodyControlFlow(
                semanticModel, ifStatement,
                out var ifBodyEndPointIsReachable,
                out var ifBodySingleExitPointOpt);

            AnalyzeSubsequentControlFlow(
                semanticModel, ifStatement,
                out var subsequentEndPontIsReachable,
                out subsequentSingleExitPointOpt);

            if (subsequentEndPontIsReachable)
            {
                if (ifBodyEndPointIsReachable)
                {
                    return InvertIfStyle.MoveIfBodyToElseClause;
                }
                else
                {
                    if (SingleIfBodyStatement(ifStatement) &&
                        SubsequentStatementsAreInTheSameBlock(ifStatement) &&
                        ifBodySingleExitPointOpt?.RawKind == GetNearmostParentJumpStatementRawKind(ifStatement))
                    {
                        return InvertIfStyle.MoveSubsequentStatementsToElseBody;
                    }
                    else
                    {
                        return InvertIfStyle.WithElseClause;
                    }
                }
            }
            else
            {
                if (ifBodyEndPointIsReachable)
                {
                    if (subsequentSingleExitPointOpt != null &&
                        SingleSubsequentStatement(ifStatement))
                    {
                        return InvertIfStyle.WithSubsequentExitPointStatement;
                    }
                    else
                    {
                        return InvertIfStyle.MoveIfBodyToElseClause;
                    }
                }
                else
                {
                    if (SubsequentStatementsAreInTheSameBlock(ifStatement))
                    {
                        return InvertIfStyle.SwapIfBodyWithSubsequentStatements;
                    }
                    else
                    {
                        return InvertIfStyle.MoveIfBodyToElseClause;
                    }
                }
            }
        }

        private bool NoSubsequentStatements(TIfStatementSyntax ifStatement)
        {
            foreach (var range in GetSubsequentStatementRanges(ifStatement))
            {
                if (!IsEmptyStatementRange(range))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsEmptyIfBody(TIfStatementSyntax ifStatement)
        {
            return IsEmptyStatementRange(GetIfBodyStatementRange(ifStatement));
        }

        private bool SingleIfBodyStatement(TIfStatementSyntax ifStatement)
        {
            var range = GetIfBodyStatementRange(ifStatement);
            return range.first == range.last;
        }

        private bool SingleSubsequentStatement(TIfStatementSyntax ifStatement)
        {
            using (var e = GetSubsequentStatementRanges(ifStatement).GetEnumerator())
            {
                return e.MoveNext() && e.Current.first == e.Current.last && !e.MoveNext();
            }
        }

        private async Task<Document> InvertIfAsync(
            Document document,
            TIfStatementSyntax ifStatement,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var negatedExpression = Negate(
                GetIfCondition(ifStatement),
                document.GetLanguageService<SyntaxGenerator>(),
                document.GetLanguageService<ISyntaxFactsService>(),
                semanticModel,
                cancellationToken);
            return document.WithSyntaxRoot(
                GetRootWithInvertIfStatement(
                    document,
                    semanticModel,
                    ifStatement,
                    invertIfStyle,
                    subsequentSingleExitPointOpt,
                    negatedExpression,
                    cancellationToken));
        }

        private void AnalyzeSubsequentControlFlow(
            SemanticModel semanticModel,
            TIfStatementSyntax ifStatement,
            out bool subsequentEndPontIsReachable,
            out SyntaxNode subsequentSingleExitPointOpt)
        {
            subsequentEndPontIsReachable = true;
            subsequentSingleExitPointOpt = null;

            foreach (var range in GetSubsequentStatementRanges(ifStatement))
            {
                AnalyzeControlFlow(semanticModel, range, out subsequentEndPontIsReachable, out subsequentSingleExitPointOpt);
                if (!subsequentEndPontIsReachable)
                {
                    return;
                }
            }
        }

        private void AnalyzeIfBodyControlFlow(
            SemanticModel semanticModel,
            TIfStatementSyntax ifStatement,
            out bool ifBodyEndPointIsReachable,
            out SyntaxNode ifBodySingleExitPointOpt)
        {
            AnalyzeControlFlow(
                semanticModel,
                GetIfBodyStatementRange(ifStatement),
                out ifBodyEndPointIsReachable,
                out ifBodySingleExitPointOpt);
        }

        private static void AnalyzeControlFlow(
            SemanticModel semanticModel,
            (SyntaxNode first, SyntaxNode last) range,
            out bool endPointIsReachable,
            out SyntaxNode singleExitPointOpt)
        {
            var flow = semanticModel.AnalyzeControlFlow(range.first, range.last);
            endPointIsReachable = flow.EndPointIsReachable;
            singleExitPointOpt = flow.ExitPoints.Length == 1 ? flow.ExitPoints[0] : null;
        }

        private bool SubsequentStatementsAreInTheSameBlock(TIfStatementSyntax ifStatement)
        {
            var (start, _) = GetSubsequentStatementRanges(ifStatement).First();
            return ifStatement.Parent == start.Parent;
        }

        protected abstract bool CanInvert(TIfStatementSyntax ifStatement);
        protected abstract bool IsElselessIfStatement(TIfStatementSyntax ifStatement);
        protected abstract SyntaxNode GetIfCondition(TIfStatementSyntax ifStatement);

        protected abstract int GetNearmostParentJumpStatementRawKind(TIfStatementSyntax ifStatement);
        protected abstract bool IsEmptyStatementRange((SyntaxNode first, SyntaxNode last) range);

        protected abstract (SyntaxNode first, SyntaxNode last) GetIfBodyStatementRange(TIfStatementSyntax ifStatement);
        protected abstract IEnumerable<(SyntaxNode first, SyntaxNode last)> GetSubsequentStatementRanges(TIfStatementSyntax ifStatement);

        protected abstract TextSpan GetHeaderSpan(TIfStatementSyntax ifStatement);
        protected abstract string GetTitle();

        protected abstract SyntaxNode GetRootWithInvertIfStatement(
            Document document,
            SemanticModel semanticModel,
            TIfStatementSyntax ifStatement,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            SyntaxNode negatedExpression,
            CancellationToken cancellationToken);

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
