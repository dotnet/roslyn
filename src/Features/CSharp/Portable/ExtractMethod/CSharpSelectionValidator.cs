// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal sealed partial class CSharpSelectionValidator(
        SemanticDocument document,
        TextSpan textSpan,
        bool localFunction) : SelectionValidator(document, textSpan)
    {
        private readonly bool _localFunction = localFunction;

        protected override InitialSelectionInfo GetInitialSelectionInfo(CancellationToken cancellationToken)
        {
            var root = this.SemanticDocument.Root;

            var adjustedSpan = GetAdjustedSpan(OriginalSpan);

            var firstTokenInSelection = root.FindTokenOnRightOfPosition(adjustedSpan.Start, includeSkipped: false);
            var lastTokenInSelection = root.FindTokenOnLeftOfPosition(adjustedSpan.End, includeSkipped: false);

            if (firstTokenInSelection.Kind() == SyntaxKind.None || lastTokenInSelection.Kind() == SyntaxKind.None)
                return InitialSelectionInfo.Failure(FeaturesResources.Invalid_selection);

            var commonRoot = firstTokenInSelection.GetCommonRoot(lastTokenInSelection);
            var selectionInExpression = commonRoot is ExpressionSyntax;

            var statusReason = CheckSpan();
            if (statusReason is not null)
                return InitialSelectionInfo.Failure(statusReason);

            return CreateInitialSelectionInfo(
                selectionInExpression, firstTokenInSelection, lastTokenInSelection, cancellationToken);

            string? CheckSpan()
            {
                if (firstTokenInSelection.SpanStart > lastTokenInSelection.Span.End)
                    return FeaturesResources.Selection_does_not_contain_a_valid_token;

                if (!UnderValidContext(firstTokenInSelection) || !UnderValidContext(lastTokenInSelection))
                    return FeaturesResources.No_valid_selection_to_perform_extraction;

                if (commonRoot == null)
                    return FeaturesResources.No_common_root_node_for_extraction;

                if (!commonRoot.ContainedInValidType())
                    return FeaturesResources.Selection_not_contained_inside_a_type;

                if (!selectionInExpression && !commonRoot.UnderValidContext())
                    return FeaturesResources.No_valid_selection_to_perform_extraction;

                return null;
            }
        }

        protected override FinalSelectionInfo UpdateSelectionInfo(
            InitialSelectionInfo initialSelectionInfo,
            CancellationToken cancellationToken)
        {
            var root = SemanticDocument.Root;
            var model = SemanticDocument.SemanticModel;

            // go through pipe line and calculate information about the user selection
            var selectionInfo = AssignInitialFinalTokens(initialSelectionInfo);
            selectionInfo = AdjustFinalTokensBasedOnContext(selectionInfo, model, cancellationToken);
            selectionInfo = AssignFinalSpan(initialSelectionInfo, selectionInfo);
            selectionInfo = ApplySpecialCases(initialSelectionInfo, selectionInfo, SemanticDocument.SyntaxTree.Options, _localFunction);
            selectionInfo = CheckErrorCasesAndAppendDescriptions(selectionInfo, root);

            return selectionInfo;
        }

        protected override async Task<SelectionResult> CreateSelectionResultAsync(
            FinalSelectionInfo selectionInfo, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(ContainsValidSelection);
            Contract.ThrowIfFalse(selectionInfo.Status.Succeeded);

            return await CSharpSelectionResult.CreateAsync(
                SemanticDocument, selectionInfo, cancellationToken).ConfigureAwait(false);
        }

        private FinalSelectionInfo ApplySpecialCases(
            InitialSelectionInfo initialSelectionInfo, FinalSelectionInfo finalSelectionInfo, ParseOptions options, bool localFunction)
        {
            if (finalSelectionInfo.Status.Failed)
                return finalSelectionInfo;

            // If we're under a global statement (and not inside an inner lambda/local-function) then there are restrictions
            // on if we can extract a method vs a local function.
            if (IsCodeInGlobalLevel())
            {
                // Cannot extract a method from a top-level statement in normal code
                if (!localFunction && options is { Kind: SourceCodeKind.Regular })
                    return finalSelectionInfo with { Status = finalSelectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_include_top_level_statements) };

                // Cannot extract a local function from a global statement in script code
                if (localFunction && options is { Kind: SourceCodeKind.Script })
                    return finalSelectionInfo with { Status = finalSelectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_include_global_statements) };
            }

            if (_localFunction)
            {
                foreach (var ancestor in initialSelectionInfo.CommonRoot.AncestorsAndSelf())
                {
                    if (ancestor.Kind() is SyntaxKind.BaseConstructorInitializer or SyntaxKind.ThisConstructorInitializer)
                        return finalSelectionInfo with { Status = finalSelectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Selection_cannot_be_in_constructor_initializer) };

                    if (ancestor is AnonymousFunctionExpressionSyntax)
                        break;
                }
            }

            if (!finalSelectionInfo.SelectionInExpression)
                return finalSelectionInfo;

            var expressionNode = finalSelectionInfo.FirstTokenInFinalSpan.GetCommonRoot(finalSelectionInfo.LastTokenInFinalSpan);
            if (expressionNode is not AssignmentExpressionSyntax assign)
                return finalSelectionInfo;

            // make sure there is a visible token at right side expression
            if (assign.Right.GetLastToken().Kind() == SyntaxKind.None)
                return finalSelectionInfo;

            return AssignFinalSpan(initialSelectionInfo, finalSelectionInfo with
            {
                FirstTokenInFinalSpan = assign.Right.GetFirstToken(includeZeroWidth: true),
                LastTokenInFinalSpan = assign.Right.GetLastToken(includeZeroWidth: true),
            });

            bool IsCodeInGlobalLevel()
            {
                for (var current = initialSelectionInfo.CommonRoot; current != null; current = current.Parent)
                {
                    if (current is CompilationUnitSyntax)
                        return true;

                    if (current is GlobalStatementSyntax)
                        return true;

                    if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax)
                        return false;
                }

                throw ExceptionUtilities.Unreachable();
            }
        }

        private static FinalSelectionInfo AdjustFinalTokensBasedOnContext(
            FinalSelectionInfo selectionInfo,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            // don't need to adjust anything if it is multi-statements case
            if (selectionInfo.GetSelectionType() == SelectionType.MultipleStatements)
                return selectionInfo;

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
                return selectionInfo with
                {
                    Status = new(succeeded: false, CSharpFeaturesResources.Selection_does_not_contain_a_valid_node),
                    FirstTokenInFinalSpan = default,
                    LastTokenInFinalSpan = default,
                };
            }

            firstValidNode = (firstValidNode.Parent is ExpressionStatementSyntax) ? firstValidNode.Parent : firstValidNode;

            return selectionInfo with
            {
                SelectionInExpression = firstValidNode is ExpressionSyntax,
                FirstTokenInFinalSpan = firstValidNode.GetFirstToken(includeZeroWidth: true),
                LastTokenInFinalSpan = firstValidNode.GetLastToken(includeZeroWidth: true),
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

                case GlobalStatementSyntax:
                    return true;

                case ConstructorInitializerSyntax constructorInitializer:
                    return constructorInitializer.ContainsInArgument(span);

                case PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType:
                    return primaryConstructorBaseType.ArgumentList.Arguments.FullSpan.Contains(span);
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

        private static FinalSelectionInfo CheckErrorCasesAndAppendDescriptions(
            FinalSelectionInfo selectionInfo,
            SyntaxNode root)
        {
            if (selectionInfo.Status.Failed)
                return selectionInfo;

            if (selectionInfo.FirstTokenInFinalSpan.IsMissing || selectionInfo.LastTokenInFinalSpan.IsMissing)
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Contains_invalid_selection)
                };
            }

            // get the node that covers the selection
            var commonNode = selectionInfo.FirstTokenInFinalSpan.GetCommonRoot(selectionInfo.LastTokenInFinalSpan);

            if (selectionInfo.GetSelectionType() != SelectionType.MultipleStatements && commonNode.HasDiagnostics())
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.The_selection_contains_syntactic_errors),
                };
            }

            var tokens = root.DescendantTokens(selectionInfo.FinalSpan);
            if (tokens.ContainPreprocessorCrossOver(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_cross_over_preprocessor_directives),
                };
            }

            // TODO : check whether this can be handled by control flow analysis engine
            if (tokens.Any(t => t.Kind() == SyntaxKind.YieldKeyword))
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_contain_a_yield_statement),
                };
            }

            // TODO : check behavior of control flow analysis engine around exception and exception handling.
            if (tokens.ContainArgumentlessThrowWithoutEnclosingCatch(selectionInfo.FinalSpan))
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: true, CSharpFeaturesResources.Selection_can_not_contain_throw_statement),
                };
            }

            if (selectionInfo.SelectionInExpression && commonNode.PartOfConstantInitializerExpression())
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Selection_can_not_be_part_of_constant_initializer_expression),
                };
            }

            if (commonNode.IsUnsafeContext())
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(selectionInfo.Status.Succeeded, CSharpFeaturesResources.The_selected_code_is_inside_an_unsafe_context),
                };
            }

            // For now patterns are being blanket disabled for extract method.  This issue covers designing extractions for them
            // and re-enabling this. 
            // https://github.com/dotnet/roslyn/issues/9244
            if (commonNode.Kind() == SyntaxKind.IsPatternExpression)
            {
                selectionInfo = selectionInfo with
                {
                    Status = selectionInfo.Status.With(succeeded: false, CSharpFeaturesResources.Selection_can_not_contain_a_pattern_expression),
                };
            }

            return selectionInfo;
        }

        private static FinalSelectionInfo AssignInitialFinalTokens(
            InitialSelectionInfo selectionInfo)
        {
            if (selectionInfo.SelectionInExpression)
            {
                // simple expression case
                return new()
                {
                    Status = selectionInfo.Status,
                    SelectionInExpression = true,
                    FirstTokenInFinalSpan = selectionInfo.CommonRoot.GetFirstToken(includeZeroWidth: true),
                    LastTokenInFinalSpan = selectionInfo.CommonRoot.GetLastToken(includeZeroWidth: true),
                };
            }

            var (firstStatement, lastStatement) = (selectionInfo.FirstStatement, selectionInfo.LastStatement);
            Contract.ThrowIfNull(firstStatement);
            Contract.ThrowIfNull(lastStatement);
            if (firstStatement == lastStatement)
            {
                // check one more time to see whether it is an expression case
                var expression = selectionInfo.CommonRoot.GetAncestor<ExpressionSyntax>();
                if (expression != null && firstStatement.Span.Contains(expression.Span))
                {
                    return new()
                    {
                        Status = selectionInfo.Status,
                        SelectionInExpression = true,
                        FirstTokenInFinalSpan = expression.GetFirstToken(includeZeroWidth: true),
                        LastTokenInFinalSpan = expression.GetLastToken(includeZeroWidth: true),
                    };
                }

                // single statement case
                return new()
                {
                    Status = selectionInfo.Status,
                    FirstTokenInFinalSpan = firstStatement.GetFirstToken(includeZeroWidth: true),
                    LastTokenInFinalSpan = firstStatement.GetLastToken(includeZeroWidth: true),
                };
            }

            // move only statements inside of the block
            return new()
            {
                Status = selectionInfo.Status,
                FirstTokenInFinalSpan = firstStatement.GetFirstToken(includeZeroWidth: true),
                LastTokenInFinalSpan = lastStatement.GetLastToken(includeZeroWidth: true),
            };
        }

        protected override TextSpan GetAdjustedSpan(TextSpan textSpan)
        {
            var text = this.SemanticDocument.Text;

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
