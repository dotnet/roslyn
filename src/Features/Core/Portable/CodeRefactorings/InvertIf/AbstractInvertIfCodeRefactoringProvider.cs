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
            MoveSubsequentStatementsToIfBody,
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
            if (!AnyIfBodyStatements(ifNode))
            {
                // (1) An empty if-statement: just negate the condition
                //  
                //  if (condition) { }
                //
                // ->
                //
                //  if (!condition) { }
                //
                return InvertIfStyle.WithNegatedCondition;
            }

            if (!AnySubsequentStatements(ifNode))
            {
                // (2) No statements after if-statement, invert with the nearmost parent jump-statement
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
                if (!ifBodyEndPointIsReachable)
                {
                    if (SingleIfBodyStatement(ifNode) &&
                        SubsequentStatementsAreInTheSameBlock(ifNode) &&
                        ifBodySingleExitPointOpt?.RawKind == GetNearmostParentJumpStatementRawKind(ifNode))
                    {
                        // (3) Invese of the case (2). Safe to move all subsequent statements to if-body.
                        // 
                        //  while (condition) {
                        //    if (condition) {
                        //      continue;
                        //    }
                        //    f();
                        //  }
                        //
                        // ->
                        //
                        //  while (condition) {
                        //    if (!condition) {
                        //      f();
                        //    }
                        //  }
                        //
                        return InvertIfStyle.MoveSubsequentStatementsToIfBody;
                    }
                    else
                    {
                        // (4) Otherwise, we generate the else and swap blocks to keep flow intact.
                        // 
                        //  while (condition) {
                        //    if (condition) {
                        //      return;
                        //    }
                        //    f();
                        //  }
                        //
                        // ->
                        //
                        //  while (condition) {
                        //    if (!condition) {
                        //      f();
                        //    } else {
                        //      return;
                        //    }
                        //  }
                        //
                        return InvertIfStyle.WithElseClause;
                    }
                }
            }
            else if (ifBodyEndPointIsReachable)
            {
                if (subsequentSingleExitPointOpt != null &&
                    SingleSubsequentStatement(ifNode))
                {
                    // (5) if-body end-point is reachable but the next statement is a only jump-statement.
                    //     This usually happens in a switch-statement. We invert and use that jump-statement.
                    // 
                    //  case constant:
                    //    if (condition) {
                    //      f();
                    //    }
                    //    break;
                    //
                    // ->
                    //
                    //  case constant:
                    //    if (!condition) {
                    //      break;
                    //    }
                    //    f();
                    //    break; // we always keep this so that we don't end up with invalid code.
                    //
                    return InvertIfStyle.WithSubsequentExitPointStatement;
                }
            }
            else if (SubsequentStatementsAreInTheSameBlock(ifNode))
            {
                // (6) If both if-body and subsequent statements have an unreachable end-point,
                //     it would be safe to just swap the two.
                //
                //    if (condition) {
                //      return;
                //    }
                //    break;
                //
                // ->
                //
                //  case constant:
                //    if (!condition) {
                //      break;
                //    }
                //    return;
                //
                return InvertIfStyle.SwapIfBodyWithSubsequentStatements;
            }

            // (7) If none of the above worked, as the last resort we invert and generate an empty if-body.
            // 
            //  {
            //    if (condition) {
            //      f();
            //    }
            //    f();
            //  }
            //
            // ->
            //
            //  {
            //    if (!condition) {
            //    } else {
            //      f();
            //    }
            //    f();
            //  }
            //  
            return InvertIfStyle.MoveIfBodyToElseClause;
        }

        private bool AnySubsequentStatements(TIfStatementSyntax ifNode)
        {
            foreach (var statementRange in GetSubsequentStatementRanges(ifNode))
            {
                if (!IsEmptyStatementRange(statementRange))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AnyIfBodyStatements(TIfStatementSyntax ifNode)
        {
            return !IsEmptyStatementRange(GetIfBodyStatementRange(ifNode));
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
