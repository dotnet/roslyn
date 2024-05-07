// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionValidator(
        SemanticDocument document,
        TextSpan textSpan,
        ExtractMethodOptions options,
        bool localFunction) : SelectionValidator<CSharpSelectionResult, StatementSyntax>(document, textSpan, options)
    {
        private readonly bool _localFunction = localFunction;

        public override async Task<(CSharpSelectionResult, OperationStatus)> GetValidSelectionAsync(CancellationToken cancellationToken)
        {
            if (!ContainsValidSelection)
                return (null, OperationStatus.FailedWithUnknownReason);

            var text = SemanticDocument.Text;
            var root = SemanticDocument.Root;
            var model = SemanticDocument.SemanticModel;
            var doc = SemanticDocument;

            // go through pipe line and calculate information about the user selection
            var selectionInfo = GetInitialSelectionInfo(root, text);
            selectionInfo = AssignInitialFinalTokens(selectionInfo, root, cancellationToken);
            selectionInfo = AdjustFinalTokensBasedOnContext(selectionInfo, model, cancellationToken);
            selectionInfo = AssignFinalSpan(selectionInfo, text);
            selectionInfo = ApplySpecialCases(selectionInfo, text, SemanticDocument.SyntaxTree.Options, _localFunction);
            selectionInfo = CheckErrorCasesAndAppendDescriptions(selectionInfo, root);

            // there was a fatal error that we couldn't even do negative preview, return error result
            if (selectionInfo.Status.Failed)
                return (null, selectionInfo.Status);

            var controlFlowSpan = GetControlFlowSpan(selectionInfo);
            if (!selectionInfo.SelectionInExpression)
            {
                var statementRange = GetStatementRangeContainedInSpan<StatementSyntax>(root, controlFlowSpan, cancellationToken);
                if (statementRange == null)
                {
                    selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Cannot_determine_valid_range_of_statements_to_extract));
                    return (null, selectionInfo.Status);
                }

                var isFinalSpanSemanticallyValid = IsFinalSpanSemanticallyValidSpan(model, controlFlowSpan, statementRange.Value, cancellationToken);
                if (!isFinalSpanSemanticallyValid)
                {
                    // check control flow only if we are extracting statement level, not expression
                    // level. you can not have goto that moves control out of scope in expression level
                    // (even in lambda)
                    selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: true, CSharpFeaturesResources.Not_all_code_paths_return));
                }
            }

            var selectionChanged = selectionInfo.FirstTokenInOriginalSpan != selectionInfo.FirstTokenInFinalSpan || selectionInfo.LastTokenInOriginalSpan != selectionInfo.LastTokenInFinalSpan;

            var result = await CSharpSelectionResult.CreateAsync(
                selectionInfo.OriginalSpan,
                selectionInfo.FinalSpan,
                Options,
                selectionInfo.SelectionInExpression,
                doc,
                selectionInfo.FirstTokenInFinalSpan,
                selectionInfo.LastTokenInFinalSpan,
                selectionChanged,
                cancellationToken).ConfigureAwait(false);
            return (result, selectionInfo.Status);
        }

        private SelectionInfo ApplySpecialCases(SelectionInfo selectionInfo, SourceText text, ParseOptions options, bool localFunction)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            if (selectionInfo.CommonRootFromOriginalSpan.IsKind(SyntaxKind.CompilationUnit)
                || selectionInfo.CommonRootFromOriginalSpan.IsParentKind(SyntaxKind.GlobalStatement))
            {
                // Cannot extract a local function from a global statement in script code
                if (localFunction && options is { Kind: SourceCodeKind.Script })
                {
                    return selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_include_global_statements));
                }

                // Cannot extract a method from a top-level statement in normal code
                if (!localFunction && options is { Kind: SourceCodeKind.Regular })
                {
                    return selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_include_top_level_statements));
                }
            }

            if (_localFunction)
            {
                foreach (var ancestor in selectionInfo.CommonRootFromOriginalSpan.AncestorsAndSelf())
                {
                    if (ancestor.Kind() is SyntaxKind.BaseConstructorInitializer or SyntaxKind.ThisConstructorInitializer)
                    {
                        return selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_be_in_constructor_initializer));
                    }

                    if (ancestor is AnonymousFunctionExpressionSyntax)
                    {
                        break;
                    }
                }
            }

            if (!selectionInfo.SelectionInExpression)
            {
                return selectionInfo;
            }

            var expressionNode = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);
            if (expressionNode is not AssignmentExpressionSyntax assign)
                return selectionInfo;

            // make sure there is a visible token at right side expression
            if (assign.Right.GetLastToken().Kind() == SyntaxKind.None)
            {
                return selectionInfo;
            }

            return AssignFinalSpan(selectionInfo.With(s => s.FirstTokenInFinalSpan = assign.Right.GetFirstToken(includeZeroWidth: true))
                                                .With(s => s.LastTokenInFinalSpan = assign.Right.GetLastToken(includeZeroWidth: true)),
                                   text);
        }

        private static TextSpan GetControlFlowSpan(SelectionInfo selectionInfo)
            => TextSpan.FromBounds(selectionInfo.FirstTokenInFinalSpan.SpanStart, selectionInfo.LastTokenInFinalSpan.Span.End);

        private static SelectionInfo AdjustFinalTokensBasedOnContext(
            SelectionInfo selectionInfo,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

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
                return selectionInfo.WithStatus(s => new OperationStatus(succeeded: false, CSharpFeaturesResources.Selection_does_not_contain_a_valid_node))
                                    .With(s => s.FirstTokenInFinalSpan = default)
                                    .With(s => s.LastTokenInFinalSpan = default);
            }

            firstValidNode = (firstValidNode.Parent is ExpressionStatementSyntax) ? firstValidNode.Parent : firstValidNode;

            return selectionInfo.With(s => s.SelectionInExpression = firstValidNode is ExpressionSyntax)
                                .With(s => s.SelectionInSingleStatement = firstValidNode is StatementSyntax)
                                .With(s => s.FirstTokenInFinalSpan = firstValidNode.GetFirstToken(includeZeroWidth: true))
                                .With(s => s.LastTokenInFinalSpan = firstValidNode.GetLastToken(includeZeroWidth: true));
        }

        private SelectionInfo GetInitialSelectionInfo(SyntaxNode root, SourceText text)
        {
            var adjustedSpan = GetAdjustedSpan(text, OriginalSpan);

            var firstTokenInSelection = root.FindTokenOnRightOfPosition(adjustedSpan.Start, includeSkipped: false);
            var lastTokenInSelection = root.FindTokenOnLeftOfPosition(adjustedSpan.End, includeSkipped: false);

            if (firstTokenInSelection.Kind() == SyntaxKind.None || lastTokenInSelection.Kind() == SyntaxKind.None)
            {
                return new SelectionInfo { Status = new OperationStatus(succeeded: false, FeaturesResources.Invalid_selection), OriginalSpan = adjustedSpan };
            }

            if (!adjustedSpan.Contains(firstTokenInSelection.Span) && !adjustedSpan.Contains(lastTokenInSelection.Span))
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(succeeded: false, FeaturesResources.Selection_does_not_contain_a_valid_token),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            if (!UnderValidContext(firstTokenInSelection) || !UnderValidContext(lastTokenInSelection))
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(succeeded: false, FeaturesResources.No_valid_selection_to_perform_extraction),
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
                    Status = new OperationStatus(succeeded: false, FeaturesResources.No_common_root_node_for_extraction),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            if (!commonRoot.ContainedInValidType())
            {
                return new SelectionInfo
                {
                    Status = new OperationStatus(succeeded: false, FeaturesResources.Selection_not_contained_inside_a_type),
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
                    Status = new OperationStatus(succeeded: false, FeaturesResources.No_valid_selection_to_perform_extraction),
                    OriginalSpan = adjustedSpan,
                    FirstTokenInOriginalSpan = firstTokenInSelection,
                    LastTokenInOriginalSpan = lastTokenInSelection
                };
            }

            return new SelectionInfo
            {
                Status = OperationStatus.SucceededStatus,
                OriginalSpan = adjustedSpan,
                CommonRootFromOriginalSpan = commonRoot,
                SelectionInExpression = selectionInExpression,
                FirstTokenInOriginalSpan = firstTokenInSelection,
                LastTokenInOriginalSpan = lastTokenInSelection
            };
        }

        private static bool UnderValidContext(SyntaxToken token)
            => token.GetAncestors<SyntaxNode>().Any(n => CheckTopLevel(n, token.Span));

        private static bool CheckTopLevel(SyntaxNode node, TextSpan span)
        {
            switch (node)
            {
                case BlockSyntax block:
                    return ContainsInBlockBody(block, span);
                case ArrowExpressionClauseSyntax expressionBodiedMember:
                    return ContainsInExpressionBodiedMemberBody(expressionBodiedMember, span);
                case FieldDeclarationSyntax field:
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            if (variable.Initializer != null && variable.Initializer.Span.Contains(span))
                            {
                                return true;
                            }
                        }

                        break;
                    }

                case GlobalStatementSyntax _:
                    return true;
                case ConstructorInitializerSyntax constructorInitializer:
                    return constructorInitializer.ContainsInArgument(span);
            }

            return false;
        }

        private static bool ContainsInBlockBody(BlockSyntax block, TextSpan textSpan)
        {
            if (block == null)
            {
                return false;
            }

            var blockSpan = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart);
            return blockSpan.Contains(textSpan);
        }

        private static bool ContainsInExpressionBodiedMemberBody(ArrowExpressionClauseSyntax expressionBodiedMember, TextSpan textSpan)
        {
            if (expressionBodiedMember == null)
            {
                return false;
            }

            var expressionBodiedMemberBody = TextSpan.FromBounds(expressionBodiedMember.Expression.SpanStart, expressionBodiedMember.Expression.Span.End);
            return expressionBodiedMemberBody.Contains(textSpan);
        }

        private static SelectionInfo CheckErrorCasesAndAppendDescriptions(
            SelectionInfo selectionInfo,
            SyntaxNode root)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            if (selectionInfo.FirstTokenInFinalSpan.IsMissing || selectionInfo.LastTokenInFinalSpan.IsMissing)
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Contains_invalid_selection));
            }

            // get the node that covers the selection
            var commonNode = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);

            if ((selectionInfo.SelectionInExpression || selectionInfo.SelectionInSingleStatement) && commonNode.HasDiagnostics())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.The_selection_contains_syntactic_errors));
            }

            var tokens = root.DescendantTokens(selectionInfo.FinalSpan);
            if (tokens.ContainPreprocessorCrossOver(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_cross_over_preprocessor_directives));
            }

            // TODO : check whether this can be handled by control flow analysis engine
            if (tokens.Any(t => t.Kind() == SyntaxKind.YieldKeyword))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_contain_a_yield_statement));
            }

            // TODO : check behavior of control flow analysis engine around exception and exception handling.
            if (tokens.ContainArgumentlessThrowWithoutEnclosingCatch(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_contain_throw_statement));
            }

            if (selectionInfo.SelectionInExpression && commonNode.PartOfConstantInitializerExpression())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Selection_can_not_be_part_of_constant_initializer_expression));
            }

            if (commonNode.IsUnsafeContext())
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(s.Succeeded, CSharpFeaturesResources.The_selected_code_is_inside_an_unsafe_context));
            }

            // For now patterns are being blanket disabled for extract method.  This issue covers designing extractions for them
            // and re-enabling this. 
            // https://github.com/dotnet/roslyn/issues/9244
            if (commonNode.Kind() == SyntaxKind.IsPatternExpression)
            {
                selectionInfo = selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.Selection_can_not_contain_a_pattern_expression));
            }

            return selectionInfo;
        }

        private static SelectionInfo AssignInitialFinalTokens(SelectionInfo selectionInfo, SyntaxNode root, CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            if (selectionInfo.SelectionInExpression)
            {
                // simple expression case
                return selectionInfo.With(s => s.FirstTokenInFinalSpan = s.CommonRootFromOriginalSpan.GetFirstToken(includeZeroWidth: true))
                                    .With(s => s.LastTokenInFinalSpan = s.CommonRootFromOriginalSpan.GetLastToken(includeZeroWidth: true));
            }

            var range = GetStatementRangeContainingSpan<StatementSyntax>(
                CSharpSyntaxFacts.Instance,
                root, TextSpan.FromBounds(selectionInfo.FirstTokenInOriginalSpan.SpanStart, selectionInfo.LastTokenInOriginalSpan.Span.End),
                cancellationToken);

            if (range == null)
            {
                return selectionInfo.WithStatus(s => s.With(succeeded: false, CSharpFeaturesResources.No_valid_statement_range_to_extract));
            }

            var statement1 = range.Value.Item1;
            var statement2 = range.Value.Item2;

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

        private static SelectionInfo AssignFinalSpan(SelectionInfo selectionInfo, SourceText text)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            // set final span
            var start = (selectionInfo.FirstTokenInOriginalSpan == selectionInfo.FirstTokenInFinalSpan)
                            ? Math.Min(selectionInfo.FirstTokenInOriginalSpan.SpanStart, selectionInfo.OriginalSpan.Start)
                            : selectionInfo.FirstTokenInFinalSpan.FullSpan.Start;

            var end = (selectionInfo.LastTokenInOriginalSpan == selectionInfo.LastTokenInFinalSpan)
                            ? Math.Max(selectionInfo.LastTokenInOriginalSpan.Span.End, selectionInfo.OriginalSpan.End)
                            : selectionInfo.LastTokenInFinalSpan.FullSpan.End;

            return selectionInfo.With(s => s.FinalSpan = GetAdjustedSpan(text, TextSpan.FromBounds(start, end)));
        }

        public override bool ContainsNonReturnExitPointsStatements(IEnumerable<SyntaxNode> jumpsOutOfRegion)
            => jumpsOutOfRegion.Where(n => n is not ReturnStatementSyntax).Any();

        public override IEnumerable<SyntaxNode> GetOuterReturnStatements(SyntaxNode commonRoot, IEnumerable<SyntaxNode> jumpsOutOfRegion)
        {
            var returnStatements = jumpsOutOfRegion.Where(s => s is ReturnStatementSyntax);

            var container = commonRoot.GetAncestorsOrThis<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault();
            if (container == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            var returnableConstructPairs = returnStatements.Select(r => (r, r.GetAncestors<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault()))
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
            if (body.Parent is not MethodDeclarationSyntax method)
            {
                return false;
            }

            // make sure this method doesn't have return type.
            return method.ReturnType is PredefinedTypeSyntax p &&
                p.Keyword.Kind() == SyntaxKind.VoidKeyword;
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

            // if the span is past the end of the line (ie, in whitespace) then
            // return to the end of the line including whitespace
            if (textSpan.Start > previousLine.End)
            {
                return TextSpan.FromBounds(textSpan.Start, previousLine.EndIncludingLineBreak);
            }

            return TextSpan.FromBounds(textSpan.Start, previousLine.End);
        }
    }
}
