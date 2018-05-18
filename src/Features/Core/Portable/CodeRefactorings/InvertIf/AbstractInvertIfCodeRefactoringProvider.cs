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
using System.Collections.Immutable;
using System.Diagnostics;

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

        protected readonly struct StatementRange
        {
            public readonly SyntaxNode FirstStatement;
            public readonly SyntaxNode LastStatement;

            public static readonly StatementRange Empty = new StatementRange();

            public StatementRange(SyntaxNode firstStatement, SyntaxNode lastStatement)
            {
                Debug.Assert(firstStatement != null);
                Debug.Assert(lastStatement != null);
                Debug.Assert(firstStatement.Parent != null);
                Debug.Assert(firstStatement.Parent == lastStatement.Parent);
                Debug.Assert(firstStatement.SpanStart <= lastStatement.SpanStart);
                FirstStatement = firstStatement;
                LastStatement = lastStatement;
            }

            public bool IsSingleStatement => FirstStatement == LastStatement;
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

            if (!CanInvert(ifNode))
            {
                return;
            }

            // Keep the subsequent exit-point to be used in case (5) below.
            SyntaxNode subsequentSingleExitPointOpt = null;

            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var invertIfStyle = IsElseless(ifNode)
                ? GetInvertIfStyle(
                    ifNode,
                    semanticModel,
                    ref subsequentSingleExitPointOpt)
                : InvertIfStyle.Normal;

            context.RegisterRefactoring(new MyCodeAction(GetTitle(),
                c => InvertIfAsync(root, document, semanticModel, ifNode, invertIfStyle, subsequentSingleExitPointOpt, c)));
        }

        private InvertIfStyle GetInvertIfStyle(
            TIfStatementSyntax ifNode,
            SemanticModel semanticModel,
            ref SyntaxNode subsequentSingleExitPointOpt)
        {
            var ifBodyStatementRange = GetIfBodyStatementRange(ifNode);
            if (IsEmptyStatementRange(ifBodyStatementRange))
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

            var subsequentStatementRanges = GetSubsequentStatementRanges(ifNode).ToImmutableArray();
            if (subsequentStatementRanges.All(IsEmptyStatementRange))
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

            AnalyzeControlFlow(
                semanticModel, ifBodyStatementRange,
                out var ifBodyEndPointIsReachable,
                out var ifBodySingleExitPointOpt);

            AnalyzeSubsequentControlFlow(
                semanticModel, subsequentStatementRanges,
                out var subsequentEndPointIsReachable,
                out subsequentSingleExitPointOpt);

            if (subsequentEndPointIsReachable)
            {
                if (!ifBodyEndPointIsReachable)
                {
                    if (ifBodyStatementRange.IsSingleStatement &&
                        SubsequentStatementsAreInTheSameBlock(ifNode, subsequentStatementRanges) &&
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
                    SingleSubsequentStatement(subsequentStatementRanges))
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
            else if (SubsequentStatementsAreInTheSameBlock(ifNode, subsequentStatementRanges))
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

        private static bool SingleSubsequentStatement(ImmutableArray<StatementRange> subsequentStatementRanges)
        {
            return subsequentStatementRanges.Length == 1 && subsequentStatementRanges[0].IsSingleStatement;
        }

        private Task<Document> InvertIfAsync(
            SyntaxNode root,
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
                        root,
                        ifNode,
                        invertIfStyle,
                        subsequentSingleExitPointOpt,
                        negatedExpression: Negate(
                            GetCondition(ifNode),
                            document.GetLanguageService<SyntaxGenerator>(),
                            document.GetLanguageService<ISyntaxFactsService>(),
                            semanticModel,
                            cancellationToken))));
        }

        private static void AnalyzeSubsequentControlFlow(
            SemanticModel semanticModel,
            ImmutableArray<StatementRange> subsequentStatementRanges,
            out bool subsequentEndPointIsReachable,
            out SyntaxNode subsequentSingleExitPointOpt)
        {
            subsequentEndPointIsReachable = true;
            subsequentSingleExitPointOpt = null;

            foreach (var statementRange in subsequentStatementRanges)
            {
                AnalyzeControlFlow(
                    semanticModel,
                    statementRange,
                    out subsequentEndPointIsReachable,
                    out subsequentSingleExitPointOpt);
                if (!subsequentEndPointIsReachable)
                {
                    return;
                }
            }
        }

        private static void AnalyzeControlFlow(
            SemanticModel semanticModel,
            StatementRange statementRange,
            out bool endPointIsReachable,
            out SyntaxNode singleExitPointOpt)
        {
            var flow = semanticModel.AnalyzeControlFlow(statementRange.FirstStatement, statementRange.LastStatement);
            endPointIsReachable = flow.EndPointIsReachable;
            singleExitPointOpt = flow.ExitPoints.Length == 1 ? flow.ExitPoints[0] : null;
        }

        private static bool SubsequentStatementsAreInTheSameBlock(
            TIfStatementSyntax ifNode,
            ImmutableArray<StatementRange> subsequentStatementRanges)
        {
            Debug.Assert(subsequentStatementRanges.Length > 0);
            return ifNode.Parent == subsequentStatementRanges[0].FirstStatement.Parent;
        }

        protected abstract bool CanInvert(TIfStatementSyntax ifNode);
        protected abstract bool IsElseless(TIfStatementSyntax ifNode);
        protected abstract SyntaxNode GetCondition(TIfStatementSyntax ifNode);
        protected abstract TextSpan GetHeaderSpan(TIfStatementSyntax ifNode);
        protected abstract string GetTitle();

        protected abstract int GetNearmostParentJumpStatementRawKind(TIfStatementSyntax ifNode);
        protected abstract bool IsEmptyStatementRange(StatementRange statementRange);

        protected abstract StatementRange GetIfBodyStatementRange(TIfStatementSyntax ifNode);
        protected abstract IEnumerable<StatementRange> GetSubsequentStatementRanges(TIfStatementSyntax ifNode);

        protected abstract SyntaxNode GetRootWithInvertIfStatement(
            SyntaxNode root,
            TIfStatementSyntax ifNode,
            InvertIfStyle invertIfStyle,
            SyntaxNode subsequentSingleExitPointOpt,
            SyntaxNode negatedExpression);

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
