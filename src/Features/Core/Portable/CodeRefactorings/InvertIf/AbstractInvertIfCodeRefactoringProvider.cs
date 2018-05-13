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

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var textSpan = context.Span;
            if (!textSpan.IsEmpty)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            var ifNode = token.GetAncestor<TIfStatementSyntax>();
            if (ifNode == null)
            {
                return;
            }

            if (ifNode.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            var headerSpan = GetHeaderSpan(ifNode);
            if (!headerSpan.IntersectsWith(textSpan))
            {
                return;
            }

            if (ifNode.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            if (!CanInvert(ifNode))
            {
                return;
            }

            SyntaxNode subsequentSingleExitPointOpt = null;
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var invertIfStyle = IsElseless(ifNode)
                ? GetInvertIfStyle(
                    ifNode,
                    semanticModel,
                    ref subsequentSingleExitPointOpt)
                : InvertIfStyle.Normal;

            context.RegisterRefactoring(new MyCodeAction(GetTitle(),
                c => InvertIfAsync(document, semanticModel, ifNode, invertIfStyle, subsequentSingleExitPointOpt, c)));
        }

        private InvertIfStyle GetInvertIfStyle(
            TIfStatementSyntax ifNode,
            SemanticModel semanticModel,
            ref SyntaxNode subsequentSingleExitPointOpt)
        {
            if (NoIfBodyStatements(ifNode))
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

            if (NoSubsequentStatements(ifNode))
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
                semanticModel, ifNode,
                out var ifBodyEndPointIsReachable,
                out var ifBodySingleExitPointOpt);

            AnalyzeSubsequentControlFlow(
                semanticModel, ifNode,
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
                    if (SingleIfBodyStatement(ifNode) &&
                        SubsequentStatementsAreInTheSameBlock(ifNode) &&
                        ifBodySingleExitPointOpt?.RawKind == GetNearmostParentJumpStatementRawKind(ifNode))
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
                        SingleSubsequentStatement(ifNode))
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
                    if (SubsequentStatementsAreInTheSameBlock(ifNode))
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

        private bool NoSubsequentStatements(TIfStatementSyntax ifNode)
        {
            foreach (var statementRange in GetSubsequentStatementRanges(ifNode))
            {
                if (!IsEmptyStatementRange(statementRange))
                {
                    return false;
                }
            }

            return true;
        }

        private bool NoIfBodyStatements(TIfStatementSyntax ifNode)
        {
            return IsEmptyStatementRange(GetIfBodyStatementRange(ifNode));
        }

        private bool SingleIfBodyStatement(TIfStatementSyntax ifNode)
        {
            var statementRange = GetIfBodyStatementRange(ifNode);
            return statementRange.first == statementRange.last;
        }

        private bool SingleSubsequentStatement(TIfStatementSyntax ifNode)
        {
            using (var e = GetSubsequentStatementRanges(ifNode).GetEnumerator())
            {
                return e.MoveNext() && e.Current.first == e.Current.last && !e.MoveNext();
            }
        }

        private Task<Document> InvertIfAsync(
            Document document,
            SemanticModel semanticModel,
            TIfStatementSyntax ifNode,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                document.WithSyntaxRoot(
                    GetRootWithInvertIfStatement(
                        document,
                        semanticModel,
                        ifNode,
                        invertIfStyle,
                        subsequentSingleExitPointOpt,
                        negatedExpression: Negate(
                            GetCondition(ifNode),
                            document.GetLanguageService<SyntaxGenerator>(),
                            document.GetLanguageService<ISyntaxFactsService>(),
                            semanticModel,
                            cancellationToken),
                        cancellationToken)));
        }

        private void AnalyzeSubsequentControlFlow(
            SemanticModel semanticModel,
            TIfStatementSyntax ifNode,
            out bool subsequentEndPontIsReachable,
            out SyntaxNode subsequentSingleExitPointOpt)
        {
            subsequentEndPontIsReachable = true;
            subsequentSingleExitPointOpt = null;

            foreach (var statementRange in GetSubsequentStatementRanges(ifNode))
            {
                AnalyzeControlFlow(
                    semanticModel,
                    statementRange,
                    out subsequentEndPontIsReachable,
                    out subsequentSingleExitPointOpt);
                if (!subsequentEndPontIsReachable)
                {
                    return;
                }
            }
        }

        private void AnalyzeIfBodyControlFlow(
            SemanticModel semanticModel,
            TIfStatementSyntax ifNode,
            out bool ifBodyEndPointIsReachable,
            out SyntaxNode ifBodySingleExitPointOpt)
        {
            AnalyzeControlFlow(
                semanticModel,
                GetIfBodyStatementRange(ifNode),
                out ifBodyEndPointIsReachable,
                out ifBodySingleExitPointOpt);
        }

        private static void AnalyzeControlFlow(
            SemanticModel semanticModel,
            (SyntaxNode first, SyntaxNode last) statementRange,
            out bool endPointIsReachable,
            out SyntaxNode singleExitPointOpt)
        {
            var flow = semanticModel.AnalyzeControlFlow(statementRange.first, statementRange.last);
            endPointIsReachable = flow.EndPointIsReachable;
            singleExitPointOpt = flow.ExitPoints.Length == 1 ? flow.ExitPoints[0] : null;
        }

        private bool SubsequentStatementsAreInTheSameBlock(TIfStatementSyntax ifNode)
        {
            var (firstStatement, _) = GetSubsequentStatementRanges(ifNode).First();
            return ifNode.Parent == firstStatement.Parent;
        }

        protected abstract bool CanInvert(TIfStatementSyntax ifNode);
        protected abstract bool IsElseless(TIfStatementSyntax ifNode);
        protected abstract SyntaxNode GetCondition(TIfStatementSyntax ifNode);
        protected abstract TextSpan GetHeaderSpan(TIfStatementSyntax ifNode);
        protected abstract string GetTitle();

        protected abstract int GetNearmostParentJumpStatementRawKind(TIfStatementSyntax ifNode);
        protected abstract bool IsEmptyStatementRange((SyntaxNode first, SyntaxNode last) statementRange);

        protected abstract (SyntaxNode first, SyntaxNode last) GetIfBodyStatementRange(TIfStatementSyntax ifNode);
        protected abstract IEnumerable<(SyntaxNode first, SyntaxNode last)> GetSubsequentStatementRanges(TIfStatementSyntax ifNode);

        protected abstract SyntaxNode GetRootWithInvertIfStatement(
            Document document,
            SemanticModel semanticModel,
            TIfStatementSyntax ifNode,
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
