// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionValidator : SelectionValidator
    {
        public CSharpSelectionValidator(
            SemanticDocument document,
            TextSpan textSpan,
            OptionSet options) :
            base(document, textSpan, options)
        {
        }

        public override async Task<SelectionResult> GetValidSelectionAsync(CancellationToken cancellationToken)
        {
            if (!this.ContainsValidSelection)
            {
                return NullSelection;
            }

            var text = this.SemanticDocument.Text;
            var root = this.SemanticDocument.Root;
            var model = this.SemanticDocument.SemanticModel;

            // go through pipe line and calculate information about the user selection
            var selectionInfo = GetInitialSelectionInfo(root, text, cancellationToken);
            selectionInfo = AssignInitialFinalTokens(selectionInfo, root, cancellationToken);
            selectionInfo = AdjustFinalTokensBasedOnContext(selectionInfo, model, cancellationToken);
            selectionInfo = AssignFinalSpan(selectionInfo, text, cancellationToken);
            selectionInfo = ApplySpecialCases(selectionInfo, text, cancellationToken);
            selectionInfo = CheckErrorCasesAndAppendDescriptions(selectionInfo, root, cancellationToken);

            // there was a fatal error that we couldn't even do negative preview, return error result
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion())
            {
                return new ErrorSelectionResult(selectionInfo.Status);
            }

            var controlFlowSpan = GetControlFlowSpan(selectionInfo);
            if (!selectionInfo.SelectionInExpression)
            {
                var statementRange = GetStatementRangeContainedInSpan<StatementSyntax>(root, controlFlowSpan, cancellationToken);
                if (statementRange == null)
                {
                    selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.CantDetermineValidRangeOfStatements));
                    return new ErrorSelectionResult(selectionInfo.Status);
                }

                var isFinalSpanSemanticallyValid = IsFinalSpanSemanticallyValidSpan(model, controlFlowSpan, statementRange, cancellationToken);
                if (!isFinalSpanSemanticallyValid)
                {
                    // check control flow only if we are extracting statement level, not expression
                    // level. you can not have goto that moves control out of scope in expression level
                    // (even in lambda)
                    selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.BestEffort, CSharpFeaturesResources.NotAllCodePathReturns));
                }
            }

            return await CSharpSelectionResult.CreateAsync(
                selectionInfo.Status,
                selectionInfo.OriginalSpan,
                selectionInfo.FinalSpan,
                this.Options,
                selectionInfo.SelectionInExpression,
                this.SemanticDocument,
                selectionInfo.FirstTokenInFinalSpan,
                selectionInfo.LastTokenInFinalSpan,
                cancellationToken).ConfigureAwait(false);
        }

        private SelectionInfo ApplySpecialCases(SelectionInfo selectionInfo, SourceText text, CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion() || !selectionInfo.SelectionInExpression)
            {
                return selectionInfo;
            }

            var expressionNode = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);
            if (!expressionNode.IsAnyAssignExpression())
            {
                return selectionInfo;
            }

            var assign = (AssignmentExpressionSyntax)expressionNode;

            // make sure there is a visible token at right side expression
            if (assign.Right.GetLastToken().Kind() == SyntaxKind.None)
            {
                return selectionInfo;
            }

            return AssignFinalSpan(selectionInfo.With(s => s.FirstTokenInFinalSpan = assign.Right.GetFirstToken(includeZeroWidth: true))
                                                .With(s => s.LastTokenInFinalSpan = assign.Right.GetLastToken(includeZeroWidth: true)),
                                   text, cancellationToken);
        }

        private TextSpan GetControlFlowSpan(SelectionInfo selectionInfo)
        {
            return TextSpan.FromBounds(selectionInfo.FirstTokenInFinalSpan.SpanStart, selectionInfo.LastTokenInFinalSpan.Span.End);
        }

        private SelectionInfo AdjustFinalTokensBasedOnContext(
            SelectionInfo selectionInfo,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion())
            {
                return selectionInfo;
            }

            // don't need to adjust anything if it is multi-statements case
            if (!selectionInfo.SelectionInExpression && !selectionInfo.SelectionInSingleStatement)
            {
                return selectionInfo;
            }

            // get the node that covers the selection
            var node = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);

            var validNode = Check(semanticModel, node, cancellationToken);
            if (validNode)
            {
                return selectionInfo;
            }

            var firstValidNode = node.GetAncestors<SyntaxNode>().FirstOrDefault(n => Check(semanticModel, n, cancellationToken));
            if (firstValidNode == null)
            {
                // couldn't find any valid node
                return selectionInfo.WithStatus(s => new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.SelectionDoesNotContainAValidNode))
                                    .With(s => s.FirstTokenInFinalSpan = default(SyntaxToken))
                                    .With(s => s.LastTokenInFinalSpan = default(SyntaxToken));
            }

            firstValidNode = (firstValidNode.Parent is ExpressionStatementSyntax) ? firstValidNode.Parent : firstValidNode;

            return selectionInfo.With(s => s.SelectionInExpression = firstValidNode is ExpressionSyntax)
                                .With(s => s.SelectionInSingleStatement = firstValidNode is StatementSyntax)
                                .With(s => s.FirstTokenInFinalSpan = firstValidNode.GetFirstToken(includeZeroWidth: true))
                                .With(s => s.LastTokenInFinalSpan = firstValidNode.GetLastToken(includeZeroWidth: true));
        }

        private SelectionInfo GetInitialSelectionInfo(SyntaxNode root, SourceText text, CancellationToken cancellationToken)
        {
            var adjustedSpan = GetAdjustedSpan(text, this.OriginalSpan);

            var firstTokenInSelection = root.FindTokenOnRightOfPosition(adjustedSpan.Start, includeSkipped: false);
            var lastTokenInSelection = root.FindTokenOnLeftOfPosition(adjustedSpan.End, includeSkipped: false);

            if (firstTokenInSelection.Kind() == SyntaxKind.None || lastTokenInSelection.Kind() == SyntaxKind.None)
            {
                return new SelectionInfo { Status = new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.InvalidSelection), OriginalSpan = adjustedSpan };
            }

            if (!adjustedSpan.Contains(firstTokenInSelection.Span) && !adjustedSpan.Contains(lastTokenInSelection.Span))
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.SelectionDoesNotContainAValidToken),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            if (!firstTokenInSelection.UnderValidContext() || !lastTokenInSelection.UnderValidContext())
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.NoValidSelectionToPerformExtraction),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            var commonRoot = firstTokenInSelection.GetCommonRoot(lastTokenInSelection);
            if (commonRoot == null)
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.NoCommonRootNodeForExtraction),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            var selectionInExpression = commonRoot is ExpressionSyntax;
            if (!selectionInExpression && !commonRoot.UnderValidContext())
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(OperationStatusFlag.None, CSharpFeaturesResources.NoValidSelectionToPerformExtraction),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            return new SelectionInfo
            {
                Status = OperationStatus.Succeeded,
                OriginalSpan = adjustedSpan,
                CommonRootFromOriginalSpan = commonRoot,
                SelectionInExpression = selectionInExpression,
                FirstTokenInOriginalSpan = firstTokenInSelection,
                LastTokenInOriginalSpan = lastTokenInSelection
            };
        }

        private SelectionInfo CheckErrorCasesAndAppendDescriptions(SelectionInfo selectionInfo, SyntaxNode root, CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion())
            {
                return selectionInfo;
            }

            if (selectionInfo.FirstTokenInFinalSpan.IsMissing || selectionInfo.LastTokenInFinalSpan.IsMissing)
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.ContainsInvalidSelection));
            }

            // get the node that covers the selection
            var commonNode = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);

            if ((selectionInfo.SelectionInExpression || selectionInfo.SelectionInSingleStatement) && commonNode.HasDiagnostics())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.TheSelectionContainsSyntacticErrors));
            }

            var tokens = root.DescendantTokens(selectionInfo.FinalSpan);
            if (tokens.ContainPreprocessorCrossOver(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.BestEffort, CSharpFeaturesResources.SelectionCanNotCrossOverPreprocessorDirectives));
            }

            // TODO : check whether this can be handled by control flow analysis engine
            if (tokens.Any(t => t.Kind() == SyntaxKind.YieldKeyword))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.BestEffort, CSharpFeaturesResources.SelectionCanNotContainAYieldStatement));
            }

            // TODO : check behavior of control flow analysis engine around exception and exception handling.
            if (tokens.ContainArgumentlessThrowWithoutEnclosingCatch(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.BestEffort, CSharpFeaturesResources.SelectionCanNotContainThrowStatement));
            }

            if (selectionInfo.SelectionInExpression && commonNode.PartOfConstantInitializerExpression())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.SelectionCanNotBePartOfConstInitializerExpr));
            }

            if (commonNode.IsUnsafeContext())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(s.Flag, CSharpFeaturesResources.TheSelectedCodeIsInsideAnUnsafeContext));
            }

            // For now patterns are being blanket disabled for extract method.  This issue covers designing extractions for them
            // and re-enabling this. 
            // https://github.com/dotnet/roslyn/issues/9244
            if (commonNode.Kind() == SyntaxKind.IsPatternExpression)
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.SelectionCanNotContainPattern));
            }

            var selectionChanged = selectionInfo.FirstTokenInOriginalSpan != selectionInfo.FirstTokenInFinalSpan || selectionInfo.LastTokenInOriginalSpan != selectionInfo.LastTokenInFinalSpan;
            if (selectionChanged)
            {
                selectionInfo = selectionInfo.WithStatus(s => s.MarkSuggestion());
            }

            return selectionInfo;
        }

        private SelectionInfo AssignInitialFinalTokens(SelectionInfo selectionInfo, SyntaxNode root, CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion())
            {
                return selectionInfo;
            }

            if (selectionInfo.SelectionInExpression)
            {
                // simple expression case
                return selectionInfo.With(s => s.FirstTokenInFinalSpan = s.CommonRootFromOriginalSpan.GetFirstToken(includeZeroWidth: true))
                                    .With(s => s.LastTokenInFinalSpan = s.CommonRootFromOriginalSpan.GetLastToken(includeZeroWidth: true));
            }

            var range = GetStatementRangeContainingSpan<StatementSyntax>(
                root, TextSpan.FromBounds(selectionInfo.FirstTokenInOriginalSpan.SpanStart, selectionInfo.LastTokenInOriginalSpan.Span.End),
                cancellationToken);

            if (range == null)
            {
                return selectionInfo.WithStatus(s => s.With(OperationStatusFlag.None, CSharpFeaturesResources.NoValidStatementRangeToExtractOut));
            }

            var statement1 = (StatementSyntax)range.Item1;
            var statement2 = (StatementSyntax)range.Item2;

            if (statement1 == statement2)
            {
                // check one more time to see whether it is an expression case
                var expression = selectionInfo.CommonRootFromOriginalSpan.GetAncestor<ExpressionSyntax>();
                if (expression != null && statement1.Span.Contains(expression.Span))
                {
                    return selectionInfo.With(s => s.SelectionInExpression = true)
                                        .With(s => s.FirstTokenInFinalSpan = expression.GetFirstToken(includeZeroWidth: true))
                                        .With(s => s.LastTokenInFinalSpan = expression.GetLastToken(includeZeroWidth: true));
                }

                // single statement case
                return selectionInfo.With(s => s.SelectionInSingleStatement = true)
                                    .With(s => s.FirstTokenInFinalSpan = statement1.GetFirstToken(includeZeroWidth: true))
                                    .With(s => s.LastTokenInFinalSpan = statement1.GetLastToken(includeZeroWidth: true));
            }

            // move only statements inside of the block
            return selectionInfo.With(s => s.FirstTokenInFinalSpan = statement1.GetFirstToken(includeZeroWidth: true))
                                .With(s => s.LastTokenInFinalSpan = statement2.GetLastToken(includeZeroWidth: true));
        }

        private SelectionInfo AssignFinalSpan(SelectionInfo selectionInfo, SourceText text, CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.FailedWithNoBestEffortSuggestion())
            {
                return selectionInfo;
            }

            // set final span
            var start = (selectionInfo.FirstTokenInOriginalSpan == selectionInfo.FirstTokenInFinalSpan) ?
                            Math.Min(selectionInfo.FirstTokenInOriginalSpan.SpanStart, selectionInfo.OriginalSpan.Start) :
                            selectionInfo.FirstTokenInFinalSpan.FullSpan.Start;

            var end = (selectionInfo.LastTokenInOriginalSpan == selectionInfo.LastTokenInFinalSpan) ?
                            Math.Max(selectionInfo.LastTokenInOriginalSpan.Span.End, selectionInfo.OriginalSpan.End) :
                            selectionInfo.LastTokenInFinalSpan.FullSpan.End;

            return selectionInfo.With(s => s.FinalSpan = GetAdjustedSpan(text, TextSpan.FromBounds(start, end)));
        }

        public override bool ContainsNonReturnExitPointsStatements(IEnumerable<SyntaxNode> jumpsOutOfRegion)
        {
            return jumpsOutOfRegion.Where(n => !(n is ReturnStatementSyntax)).Any();
        }

        public override IEnumerable<SyntaxNode> GetOuterReturnStatements(SyntaxNode commonRoot, IEnumerable<SyntaxNode> jumpsOutOfRegion)
        {
            var returnStatements = jumpsOutOfRegion.Where(s => s is ReturnStatementSyntax);

            var container = commonRoot.GetAncestorsOrThis<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault();
            if (container == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            var returnableConstructPairs = returnStatements.Select(r => Tuple.Create(r, r.GetAncestors<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault()))
                                                           .Where(p => p.Item2 != null);

            // now filter return statements to only include the one under outmost container
            return returnableConstructPairs.Where(p => p.Item2 == container).Select(p => p.Item1);
        }

        public override bool IsFinalSpanSemanticallyValidSpan(
            SyntaxNode root, TextSpan textSpan,
            IEnumerable<SyntaxNode> returnStatements, CancellationToken cancellationToken)
        {
            // return statement shouldn't contain any return value
            if (returnStatements.Cast<ReturnStatementSyntax>().Any(r => r.Expression != null))
            {
                return false;
            }

            var lastToken = root.FindToken(textSpan.End);
            if (lastToken.Kind() == SyntaxKind.None)
            {
                return false;
            }

            var container = lastToken.GetAncestors<SyntaxNode>().FirstOrDefault(n => n.IsReturnableConstruct());
            if (container == null)
            {
                return false;
            }

            var body = container.GetBlockBody();
            if (body == null)
            {
                return false;
            }

            // make sure that next token of the last token in the selection is the close braces of containing block
            if (body.CloseBraceToken != lastToken.GetNextToken(includeZeroWidth: true))
            {
                return false;
            }

            // alright, for these constructs, it must be okay to be extracted
            switch (container.Kind())
            {
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return true;
            }

            // now, only method is okay to be extracted out
            var method = body.Parent as MethodDeclarationSyntax;
            if (method == null)
            {
                return false;
            }

            // make sure this method doesn't have return type.
            return method.ReturnType.TypeSwitch((PredefinedTypeSyntax p) => p.Keyword.Kind() == SyntaxKind.VoidKeyword);
        }

        private static TextSpan GetAdjustedSpan(SourceText text, TextSpan textSpan)
        {
            // beginning of a file
            if (textSpan.IsEmpty || textSpan.End == 0)
            {
                return textSpan;
            }

            // if it is a start of new line, make it belong to previous line
            var line = text.Lines.GetLineFromPosition(textSpan.End);
            if (line.Start != textSpan.End)
            {
                return textSpan;
            }

            // get previous line
            Contract.ThrowIfFalse(line.LineNumber > 0);
            var previousLine = text.Lines[line.LineNumber - 1];
            return TextSpan.FromBounds(textSpan.Start, previousLine.End);
        }

        private class SelectionInfo
        {
            public OperationStatus Status { get; set; }

            public TextSpan OriginalSpan { get; set; }
            public TextSpan FinalSpan { get; set; }

            public SyntaxNode CommonRootFromOriginalSpan { get; set; }

            public SyntaxToken FirstTokenInOriginalSpan { get; set; }
            public SyntaxToken LastTokenInOriginalSpan { get; set; }

            public SyntaxToken FirstTokenInFinalSpan { get; set; }
            public SyntaxToken LastTokenInFinalSpan { get; set; }

            public bool SelectionInExpression { get; set; }
            public bool SelectionInSingleStatement { get; set; }

            public SelectionInfo WithStatus(Func<OperationStatus, OperationStatus> statusGetter)
            {
                return With(s => s.Status = statusGetter(s.Status));
            }

            public SelectionInfo With(Action<SelectionInfo> valueSetter)
            {
                var newInfo = this.Clone();
                valueSetter(newInfo);
                return newInfo;
            }

            public SelectionInfo Clone()
            {
                return (SelectionInfo)this.MemberwiseClone();
            }
        }
    }
}
