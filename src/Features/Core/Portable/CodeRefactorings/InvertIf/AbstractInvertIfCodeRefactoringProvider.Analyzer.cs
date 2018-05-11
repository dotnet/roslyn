// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider
    {
        protected enum InvertIfStyle
        {
            None,
            // swap if and else
            Normal,
            // swap subsequent statements and if body
            SwapIfBodyWithSubsequence,
            // move subsequent statements to if body
            MoveSubsequenceToElseBody,
            // invert and generete else
            WithElseClause,
            // invert and generate else, keep if-body empty
            MoveIfBodyToElseClause,
            // invert and copy the exit point statement
            WithSubsequenceExitPoint,
            // invert and generate return, break, continue
            WithNearmostJump,
            // just invert the condition
            WithNegatedCondition,
        }

        protected abstract class Analyzer<TIfStatementSyntax> : IAnalyzer
            where TIfStatementSyntax : SyntaxNode
        {
            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context, SyntaxNode ifNode)
            {
                var ifStatement = (TIfStatementSyntax)ifNode;

                var textSpan = context.Span;
                var document = context.Document;
                var cancellationToken = context.CancellationToken;

                var headerSpan = GetHeaderSpan(ifStatement);
                if (!headerSpan.IntersectsWith(textSpan))
                {
                    return;
                }

                if (ifStatement.OverlapsHiddenPosition(cancellationToken))
                {
                    return;
                }

                int? generatedJumpStatementRawKindOpt = null;
                SyntaxNode subsequenceSingleExitPointOpt = null;

                var invertIfStyle = GetInvertIfStyle(
                    ifStatement,
                    await document.GetSemanticModelAsync().ConfigureAwait(false),
                    ref subsequenceSingleExitPointOpt,
                    ref generatedJumpStatementRawKindOpt);

                if (invertIfStyle == InvertIfStyle.None)
                {
                    return;
                }

                context.RegisterRefactoring(
                    new MyCodeAction(
                        GetTitle(),
                        c => InvertIfAsync(document, ifStatement, invertIfStyle, generatedJumpStatementRawKindOpt, subsequenceSingleExitPointOpt, c)));
            }

            private InvertIfStyle GetInvertIfStyle(
                TIfStatementSyntax ifStatement,
                SemanticModel semanticModel,
                ref SyntaxNode subsequenceSingleExitPointOpt,
                ref int? generatedJumpStatementRawKindOpt)
            {
                switch (IsElselessIfStatement(ifStatement))
                {
                    case null:
                        return InvertIfStyle.None;
                    case false:
                        return InvertIfStyle.Normal;
                }

                AnalyzeIfBody(semanticModel, ifStatement,
                    out var ifBodyStatementCount,
                    out var ifBodyEndPointIsReachable,
                    out var ifBodySingleExitPointOpt);

                if (ifBodyStatementCount == 0)
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

                AnalyzeSubsequence(semanticModel, ifStatement,
                    out var subsequenceStatementCount,
                    out var subsequenceEndPointIsReachable,
                    out var subsequenceIsInSameBlock,
                    out subsequenceSingleExitPointOpt,
                    out generatedJumpStatementRawKindOpt);

                if (subsequenceStatementCount == 0)
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
                    return InvertIfStyle.WithNearmostJump;
                }

                if (subsequenceEndPointIsReachable)
                {
                    if (ifBodyEndPointIsReachable)
                    {
                        return InvertIfStyle.MoveIfBodyToElseClause;
                    }
                    else
                    {
                        if (ifBodyStatementCount == 1 && subsequenceIsInSameBlock &&
                            ifBodySingleExitPointOpt?.RawKind == generatedJumpStatementRawKindOpt)
                        {
                            return InvertIfStyle.MoveSubsequenceToElseBody;
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
                        if (subsequenceSingleExitPointOpt != null &&
                            subsequenceStatementCount == 1)
                        {
                            return InvertIfStyle.WithSubsequenceExitPoint;
                        }
                        else
                        {
                            return InvertIfStyle.MoveIfBodyToElseClause;
                        }
                    }
                    else
                    {
                        if (subsequenceIsInSameBlock)
                        {
                            return InvertIfStyle.SwapIfBodyWithSubsequence;
                        }
                        else
                        {
                            return InvertIfStyle.MoveIfBodyToElseClause;
                        }
                    }
                }
            }

            private void AnalyzeIfBody(
                SemanticModel semanticModel,
                TIfStatementSyntax ifStatement,
                out int ifBodyStatementCount,
                out bool ifBodyEndPointIsReachable,
                out SyntaxNode ifBodySingleExitPointOpt)
            {
                ifBodyStatementCount = GetIfBodyStatementCount(ifStatement);

                if (ifBodyStatementCount == 0)
                {
                    ifBodySingleExitPointOpt = null;
                    ifBodyEndPointIsReachable = true;
                    return;
                }

                var controlFlow = AnalyzeIfBodyControlFlow(semanticModel, ifStatement);
                if (controlFlow.EndPointIsReachable)
                {
                    ifBodySingleExitPointOpt = null;
                    ifBodyEndPointIsReachable = true;
                }
                else
                {
                    var exitPoints = controlFlow.ExitPoints;
                    ifBodySingleExitPointOpt = exitPoints.Length == 1 ? exitPoints[0] : null;
                    ifBodyEndPointIsReachable = false;
                }
            }

            private async Task<Document> InvertIfAsync(
                Document document,
                TIfStatementSyntax ifStatement,
                InvertIfStyle invertIfStyle,
                int? generatedJumpStatementRawKindOpt,
                SyntaxNode subsequenceSingleExitPointOpt,
                CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                return document.WithSyntaxRoot(
                    // this returns the root because VB requires changing context around if statement
                    GetRootWithInvertIfStatement(
                        document, semanticModel, ifStatement, invertIfStyle, generatedJumpStatementRawKindOpt, subsequenceSingleExitPointOpt, cancellationToken));
            }

            protected abstract SyntaxNode GetRootWithInvertIfStatement(
                Document document,
                SemanticModel semanticModel,
                TIfStatementSyntax ifStatement,
                InvertIfStyle invertIfStyle,
                int? generatedJumpStatementRawKindOpt,
                SyntaxNode subsequenceSingleExitPointOpt,
                CancellationToken cancellationToken);

            protected abstract void AnalyzeSubsequence(
                SemanticModel semanticModel,
                TIfStatementSyntax ifStatement,
                out int subsequenceStatementCount,
                out bool subsequenceEndPontIsReachable,
                out bool subsequenceIsInSameBlock,
                out SyntaxNode subsequenceSingleExitPointOpt,
                out int? jumpStatementRawKindOpt);

            protected abstract ControlFlowAnalysis AnalyzeIfBodyControlFlow(
                SemanticModel semanticModel,
                TIfStatementSyntax ifStatement);

            protected abstract int GetIfBodyStatementCount(TIfStatementSyntax ifStatement);
            protected abstract bool? IsElselessIfStatement(TIfStatementSyntax ifStatement);
            protected abstract TextSpan GetHeaderSpan(TIfStatementSyntax ifStatement);
            protected abstract string GetTitle();
        }
    }
}
