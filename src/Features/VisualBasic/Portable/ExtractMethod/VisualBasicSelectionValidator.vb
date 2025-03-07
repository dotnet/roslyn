' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend NotInheritable Class VisualBasicExtractMethodService
        Friend NotInheritable Class VisualBasicSelectionValidator
            Inherits SelectionValidator

            Public Sub New(document As SemanticDocument, textSpan As TextSpan)
                MyBase.New(document, textSpan)
            End Sub

            Protected Overrides Function GetInitialSelectionInfo(cancellationToken As CancellationToken) As InitialSelectionInfo
                Dim root = Me.SemanticDocument.Root
                Dim adjustedSpan = GetAdjustedSpan(Me.OriginalSpan)
                Dim firstTokenInSelection = root.FindTokenOnRightOfPosition(adjustedSpan.Start, includeSkipped:=False)
                Dim lastTokenInSelection = root.FindTokenOnLeftOfPosition(adjustedSpan.End, includeSkipped:=False)

                If firstTokenInSelection.Kind = SyntaxKind.None OrElse lastTokenInSelection.Kind = SyntaxKind.None Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.Invalid_selection)
                End If

                If firstTokenInSelection <> lastTokenInSelection AndAlso
                   firstTokenInSelection.Span.End > lastTokenInSelection.SpanStart Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.Invalid_selection)
                End If

                If (Not adjustedSpan.Contains(firstTokenInSelection.Span)) AndAlso (Not adjustedSpan.Contains(lastTokenInSelection.Span)) Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.Selection_does_not_contain_a_valid_token)
                End If

                If (Not firstTokenInSelection.UnderValidContext()) OrElse (Not lastTokenInSelection.UnderValidContext()) Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.No_valid_selection_to_perform_extraction)
                End If

                Dim commonRoot = GetCommonRoot(firstTokenInSelection, lastTokenInSelection)
                If commonRoot Is Nothing Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.No_common_root_node_for_extraction)
                End If

                If Not commonRoot.ContainedInValidType() Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.Selection_not_contained_inside_a_type)
                End If

                Dim selectionInExpression = TypeOf commonRoot Is ExpressionSyntax AndAlso
                                            commonRoot.GetFirstToken(includeZeroWidth:=True) = firstTokenInSelection AndAlso
                                            commonRoot.GetLastToken(includeZeroWidth:=True) = lastTokenInSelection

                If (Not selectionInExpression) AndAlso (Not commonRoot.UnderValidContext()) Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.No_valid_selection_to_perform_extraction)
                End If

                ' make sure type block enclosing the selection exist
                If commonRoot.GetAncestor(Of TypeBlockSyntax)() Is Nothing Then
                    Return InitialSelectionInfo.Failure(FeaturesResources.No_valid_selection_to_perform_extraction)
                End If

                Return CreateInitialSelectionInfo(
                    selectionInExpression, firstTokenInSelection, lastTokenInSelection, cancellationToken)
            End Function

            Protected Overrides Function UpdateSelectionInfo(initialSelectionInfo As InitialSelectionInfo, cancellationToken As CancellationToken) As FinalSelectionInfo
                Dim model = Me.SemanticDocument.SemanticModel

                Dim selectionInfo = AssignInitialFinalTokens(initialSelectionInfo)
                selectionInfo = AdjustFinalTokensBasedOnContext(selectionInfo, model, cancellationToken)
                selectionInfo = AdjustFinalTokensIfNextStatement(selectionInfo, model, cancellationToken)
                selectionInfo = AssignFinalSpan(initialSelectionInfo, selectionInfo)
                selectionInfo = CheckErrorCasesAndAppendDescriptions(selectionInfo, model, cancellationToken)

                Return selectionInfo
            End Function

            Protected Overrides Async Function CreateSelectionResultAsync(
                    finalSelectionInfo As FinalSelectionInfo,
                    cancellationToken As CancellationToken) As Task(Of SelectionResult)

                Contract.ThrowIfFalse(ContainsValidSelection)
                Contract.ThrowIfFalse(finalSelectionInfo.Status.Succeeded)

                Return Await VisualBasicSelectionResult.CreateResultAsync(
                    Me.SemanticDocument, finalSelectionInfo, cancellationToken).ConfigureAwait(False)
            End Function

            Private Shared Function CheckErrorCasesAndAppendDescriptions(
                    selectionInfo As FinalSelectionInfo,
                    semanticModel As SemanticModel,
                    cancellationToken As CancellationToken) As FinalSelectionInfo
                If selectionInfo.Status.Failed() Then
                    Return selectionInfo
                End If

                Dim clone = selectionInfo

                If selectionInfo.FirstTokenInFinalSpan.IsMissing OrElse selectionInfo.LastTokenInFinalSpan.IsMissing Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=False, VBFeaturesResources.contains_invalid_selection))
                End If

                ' get the node that covers the selection
                Dim commonNode = GetFinalTokenCommonRoot(selectionInfo)

                If selectionInfo.GetSelectionType() <> SelectionType.MultipleStatements AndAlso commonNode.HasDiagnostics() Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=False, VBFeaturesResources.the_selection_contains_syntactic_errors))
                End If

                Dim root = semanticModel.SyntaxTree.GetRoot(cancellationToken)
                Dim tokens = root.DescendantTokens(selectionInfo.FinalSpan)
                If tokens.ContainPreprocessorCrossOver(selectionInfo.FinalSpan) Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=True, VBFeaturesResources.Selection_can_t_be_crossed_over_preprocessors))
                End If

                ' TODO : check behavior of control flow analysis engine around exception and exception handling.
                If tokens.ContainArgumentlessThrowWithoutEnclosingCatch(selectionInfo.FinalSpan) Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=True, VBFeaturesResources.Selection_can_t_contain_throw_without_enclosing_catch_block))
                End If

                If selectionInfo.SelectionInExpression AndAlso commonNode.PartOfConstantInitializerExpression() Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=False, VBFeaturesResources.Selection_can_t_be_parts_of_constant_initializer_expression))
                End If

                If selectionInfo.SelectionInExpression AndAlso commonNode.IsArgumentForByRefParameter(semanticModel, cancellationToken) Then
                    clone = clone.With(
                        status:=clone.Status.With(succeeded:=True, VBFeaturesResources.Argument_used_for_ByRef_parameter_can_t_be_extracted_out))
                End If

                ' if it is multiple statement case.
                If selectionInfo.GetSelectionType() = SelectionType.MultipleStatements Then
                    If commonNode.GetAncestorOrThis(Of WithBlockSyntax)() IsNot Nothing Then
                        If commonNode.GetImplicitMemberAccessExpressions(selectionInfo.FinalSpan).Any() Then
                            clone = clone.With(
                                status:=clone.Status.With(succeeded:=True, VBFeaturesResources.Implicit_member_access_can_t_be_included_in_the_selection_without_containing_statement))
                        End If
                    End If

                    If selectionInfo.FirstTokenInFinalSpan.GetAncestor(Of ExecutableStatementSyntax)() Is Nothing OrElse
                        selectionInfo.LastTokenInFinalSpan.GetAncestor(Of ExecutableStatementSyntax)() Is Nothing Then
                        clone = clone.With(
                            status:=clone.Status.With(succeeded:=False, VBFeaturesResources.Selection_must_be_part_of_executable_statements))
                    End If
                End If

                Return clone
            End Function

            Private Shared Function GetFinalTokenCommonRoot(selection As FinalSelectionInfo) As SyntaxNode
                Return GetCommonRoot(selection.FirstTokenInFinalSpan, selection.LastTokenInFinalSpan)
            End Function

            Private Shared Function GetCommonRoot(token1 As SyntaxToken, token2 As SyntaxToken) As SyntaxNode
                Return token1.GetCommonRoot(token2)
            End Function

            Private Shared Function AdjustFinalTokensIfNextStatement(
                    selectionInfo As FinalSelectionInfo,
                    semanticModel As SemanticModel,
                    cancellationToken As CancellationToken) As FinalSelectionInfo
                If selectionInfo.Status.Failed() Then
                    Return selectionInfo
                End If

                ' if last statement is next statement, make sure its corresponding loop statement is
                ' included
                Dim nextStatement = selectionInfo.LastTokenInFinalSpan.GetAncestor(Of NextStatementSyntax)()
                If nextStatement Is Nothing OrElse nextStatement.ControlVariables.Count < 2 Then
                    Return selectionInfo
                End If

                Dim outmostControlVariable = nextStatement.ControlVariables.Last

                Dim symbolInfo = semanticModel.GetSymbolInfo(outmostControlVariable, cancellationToken)
                Dim symbol = symbolInfo.GetBestOrAllSymbols().FirstOrDefault()

                ' can't find symbol for the control variable. don't provide extract method
                If symbol Is Nothing OrElse
                   symbol.Locations.Length <> 1 OrElse
                   Not symbol.Locations.First().IsInSource OrElse
                   symbol.Locations.First().SourceTree IsNot semanticModel.SyntaxTree Then
                    Return selectionInfo.With(
                        status:=selectionInfo.Status.With(succeeded:=False, VBFeaturesResources.next_statement_control_variable_doesn_t_have_matching_declaration_statement))
                End If

                Dim startPosition = symbol.Locations.First().SourceSpan.Start
                Dim root = semanticModel.SyntaxTree.GetRoot(cancellationToken)
                Dim forBlock = root.FindToken(startPosition).GetAncestor(Of ForOrForEachBlockSyntax)()
                If forBlock Is Nothing Then
                    Return selectionInfo.With(
                        status:=selectionInfo.Status.With(succeeded:=False, VBFeaturesResources.next_statement_control_variable_doesn_t_have_matching_declaration_statement))
                End If

                Dim firstStatement = forBlock.ForOrForEachStatement
                Return selectionInfo.With(
                    firstTokenInFinalSpan:=firstStatement.GetFirstToken(includeZeroWidth:=True),
                    lastTokenInFinalSpan:=nextStatement.GetLastToken(includeZeroWidth:=True))
            End Function

            Private Shared Function AdjustFinalTokensBasedOnContext(
                    selectionInfo As FinalSelectionInfo,
                    semanticModel As SemanticModel,
                    cancellationToken As CancellationToken) As FinalSelectionInfo
                If selectionInfo.Status.Failed() Then
                    Return selectionInfo
                End If

                ' don't need to adjust anything if it is multi-statements case
                If selectionInfo.GetSelectionType() = SelectionType.MultipleStatements Then
                    Return selectionInfo
                End If

                ' get the node that covers the selection
                Dim node = GetFinalTokenCommonRoot(selectionInfo)

                Dim validNode = Check(semanticModel, node, cancellationToken)
                If validNode Then
                    Return selectionInfo
                End If

                Dim firstValidNode = node.GetAncestors(Of SyntaxNode)().FirstOrDefault(
                    Function(n) Check(semanticModel, n, cancellationToken))

                If firstValidNode Is Nothing Then
                    ' couldn't find any valid node
                    Return selectionInfo.With(
                        status:=New OperationStatus(succeeded:=False, VBFeaturesResources.Selection_doesn_t_contain_any_valid_node),
                        firstTokenInFinalSpan:=Nothing,
                        lastTokenInFinalSpan:=Nothing)
                End If

                Return selectionInfo.With(
                    selectionInExpression:=TypeOf firstValidNode Is ExpressionSyntax,
                    firstTokenInFinalSpan:=firstValidNode.GetFirstToken(includeZeroWidth:=True),
                    lastTokenInFinalSpan:=firstValidNode.GetLastToken(includeZeroWidth:=True))
            End Function

            Private Shared Function AssignInitialFinalTokens(
                    selectionInfo As InitialSelectionInfo) As FinalSelectionInfo

                If selectionInfo.SelectionInExpression Then
                    ' prefer outer statement or expression if two has same span
                    Dim outerNode = selectionInfo.CommonRoot.GetOutermostNodeWithSameSpan(Function(n) TypeOf n Is ExecutableStatementSyntax OrElse TypeOf n Is ExpressionSyntax)

                    ' simple expression case
                    Return New FinalSelectionInfo With {
                        .Status = selectionInfo.Status,
                        .SelectionInExpression = TypeOf outerNode Is ExpressionSyntax,
                        .FirstTokenInFinalSpan = outerNode.GetFirstToken(includeZeroWidth:=True),
                        .LastTokenInFinalSpan = outerNode.GetLastToken(includeZeroWidth:=True)
                        }
                End If

                Dim statement1 = selectionInfo.FirstStatement
                Dim statement2 = selectionInfo.LastStatement

                If statement1 Is statement2 Then
                    ' check one more time to see whether it is an expression case
                    Dim expression = selectionInfo.CommonRoot.GetAncestor(Of ExpressionSyntax)()
                    If expression IsNot Nothing AndAlso statement1.Span.Contains(expression.Span) Then
                        Return New FinalSelectionInfo With {
                            .Status = selectionInfo.Status,
                            .SelectionInExpression = True,
                            .FirstTokenInFinalSpan = expression.GetFirstToken(includeZeroWidth:=True),
                            .LastTokenInFinalSpan = expression.GetLastToken(includeZeroWidth:=True)
                            }
                    End If

                    ' single statement case
                    ' current way to find out a statement that can be extracted out
                    Dim singleStatement = statement1.GetAncestorsOrThis(Of ExecutableStatementSyntax)().FirstOrDefault(
                        Function(s) s.Parent IsNot Nothing AndAlso s.Parent.IsStatementContainerNode() AndAlso s.Parent.ContainStatement(s))

                    If singleStatement Is Nothing Then
                        Return New FinalSelectionInfo With {
                            .Status = selectionInfo.Status.With(succeeded:=False, FeaturesResources.No_valid_statement_range_to_extract)
                            }
                    End If

                    Return New FinalSelectionInfo With {
                        .Status = selectionInfo.Status,
                        .FirstTokenInFinalSpan = singleStatement.GetFirstToken(includeZeroWidth:=True),
                        .LastTokenInFinalSpan = singleStatement.GetLastToken(includeZeroWidth:=True)
                        }
                End If

                ' Special check for vb
                ' either statement1 or statement2 is pointing to header and end of a block node
                ' return the block instead of each node
                If statement1.Parent.IsStatementContainerNode() Then
                    Dim contain1 = statement1.Parent.ContainStatement(statement1)
                    Dim contain2 = statement2.Parent.ContainStatement(statement2)

                    If Not contain1 OrElse Not contain2 Then
                        Dim parent = statement1.Parent _
                                               .GetAncestorsOrThis(Of SyntaxNode)() _
                                               .Where(Function(n) TypeOf n Is ExpressionSyntax OrElse TypeOf n Is ExecutableStatementSyntax) _
                                               .First()

                        ' single statement case
                        Return New FinalSelectionInfo With {
                            .Status = selectionInfo.Status,
                            .SelectionInExpression = TypeOf parent Is ExpressionSyntax,
                            .FirstTokenInFinalSpan = parent.GetFirstToken(),
                            .LastTokenInFinalSpan = parent.GetLastToken()
                            }
                    End If
                End If

                Return New FinalSelectionInfo With {
                    .Status = selectionInfo.Status,
                    .FirstTokenInFinalSpan = statement1.GetFirstToken(includeZeroWidth:=True),
                    .LastTokenInFinalSpan = statement2.GetLastToken(includeZeroWidth:=True)
                    }
            End Function

            Protected Overrides Function GetAdjustedSpan(textSpan As TextSpan) As TextSpan
                Dim root = Me.SemanticDocument.Root
                Dim text = Me.SemanticDocument.Text

                ' quick exit
                If textSpan.IsEmpty OrElse textSpan.End = 0 Then
                    Return textSpan
                End If

                ' regular column 0 check
                Dim line = text.Lines.GetLineFromPosition(textSpan.End)
                If line.Start <> textSpan.End Then
                    Return textSpan
                End If

                ' previous line
                Contract.ThrowIfFalse(line.LineNumber > 0)
                Dim previousLine = text.Lines(line.LineNumber - 1)

                ' check whether end of previous line is last token of a statement. if it is, don't do anything
                If root.FindTokenOnLeftOfPosition(previousLine.End).IsLastTokenOfStatement() Then
                    Return textSpan
                End If

                ' move end position of the selection
                Return textSpan.FromBounds(textSpan.Start, previousLine.End)
            End Function
        End Class
    End Class
End Namespace
