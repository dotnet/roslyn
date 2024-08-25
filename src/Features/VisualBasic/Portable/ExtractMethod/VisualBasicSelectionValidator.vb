' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Friend Class VisualBasicSelectionValidator
        Inherits SelectionValidator(Of VisualBasicSelectionResult, ExecutableStatementSyntax)

        Public Sub New(document As SemanticDocument,
                       textSpan As TextSpan)
            MyBase.New(document, textSpan)
        End Sub

        Public Overrides Async Function GetValidSelectionAsync(cancellationToken As CancellationToken) As Task(Of (VisualBasicSelectionResult, OperationStatus))
            If Not ContainsValidSelection Then
                Return (Nothing, OperationStatus.FailedWithUnknownReason)
            End If

            Dim text = Me.SemanticDocument.Text
            Dim root = SemanticDocument.Root
            Dim model = Me.SemanticDocument.SemanticModel

            Dim selectionInfo = GetInitialSelectionInfo(root)
            selectionInfo = AssignInitialFinalTokens(selectionInfo, root, cancellationToken)
            selectionInfo = AdjustFinalTokensBasedOnContext(selectionInfo, model, cancellationToken)
            selectionInfo = AdjustFinalTokensIfNextStatement(selectionInfo, model, cancellationToken)
            selectionInfo = FixUpFinalTokensAndAssignFinalSpan(selectionInfo, root, cancellationToken)
            selectionInfo = CheckErrorCasesAndAppendDescriptions(selectionInfo, model, cancellationToken)

            If selectionInfo.Status.Failed() Then
                Return (Nothing, selectionInfo.Status)
            End If

            Dim controlFlowSpan = GetControlFlowSpan(selectionInfo)
            If Not selectionInfo.SelectionInExpression Then
                Dim statementRange = GetStatementRangeContainedInSpan(Of StatementSyntax)(root, controlFlowSpan, cancellationToken)
                If statementRange Is Nothing Then
                    With selectionInfo
                        .Status = .Status.With(succeeded:=False, VBFeaturesResources.can_t_determine_valid_range_of_statements_to_extract_out)
                    End With

                    Return (Nothing, selectionInfo.Status)
                End If

                Dim isFinalSpanSemanticallyValid = IsFinalSpanSemanticallyValidSpan(model, controlFlowSpan, statementRange.Value, cancellationToken)
                If Not isFinalSpanSemanticallyValid Then
                    ' check control flow only if we are extracting statement level, not expression level.
                    ' you can't have goto that moves control out of scope in expression level (even in lambda)
                    With selectionInfo
                        .Status = .Status.With(succeeded:=True, VBFeaturesResources.Not_all_code_paths_return)
                    End With
                End If
            End If

            Dim result = Await VisualBasicSelectionResult.CreateResultAsync(
                selectionInfo.OriginalSpan,
                selectionInfo.FinalSpan,
                selectionInfo.SelectionInExpression,
                Me.SemanticDocument,
                selectionInfo.FirstTokenInFinalSpan,
                selectionInfo.LastTokenInFinalSpan,
                SelectionChanged(selectionInfo),
                cancellationToken).ConfigureAwait(False)
            Return (result, selectionInfo.Status)
        End Function

        Private Shared Function GetControlFlowSpan(selectionInfo As SelectionInfo) As TextSpan
            Return TextSpan.FromBounds(selectionInfo.FirstTokenInFinalSpan.SpanStart, selectionInfo.LastTokenInFinalSpan.Span.End)
        End Function

        Private Shared Function CheckErrorCasesAndAppendDescriptions(selectionInfo As SelectionInfo, semanticModel As SemanticModel, cancellationToken As CancellationToken) As SelectionInfo
            If selectionInfo.Status.Failed() Then
                Return selectionInfo
            End If

            Dim clone = selectionInfo.Clone()

            If selectionInfo.FirstTokenInFinalSpan.IsMissing OrElse selectionInfo.LastTokenInFinalSpan.IsMissing Then
                With clone
                    .Status = .Status.With(succeeded:=False, VBFeaturesResources.contains_invalid_selection)
                End With
            End If

            ' get the node that covers the selection
            Dim commonNode = GetFinalTokenCommonRoot(selectionInfo)

            If (selectionInfo.SelectionInExpression OrElse selectionInfo.SelectionInSingleStatement) AndAlso commonNode.HasDiagnostics() Then
                With clone
                    .Status = .Status.With(succeeded:=False, VBFeaturesResources.the_selection_contains_syntactic_errors)
                End With
            End If

            Dim root = semanticModel.SyntaxTree.GetRoot(cancellationToken)
            Dim tokens = root.DescendantTokens(selectionInfo.FinalSpan)
            If tokens.ContainPreprocessorCrossOver(selectionInfo.FinalSpan) Then
                With clone
                    .Status = .Status.With(succeeded:=True, VBFeaturesResources.Selection_can_t_be_crossed_over_preprocessors)
                End With
            End If

            ' TODO : check behavior of control flow analysis engine around exception and exception handling.
            If tokens.ContainArgumentlessThrowWithoutEnclosingCatch(selectionInfo.FinalSpan) Then
                With clone
                    .Status = .Status.With(succeeded:=True, VBFeaturesResources.Selection_can_t_contain_throw_without_enclosing_catch_block)
                End With
            End If

            If selectionInfo.SelectionInExpression AndAlso commonNode.PartOfConstantInitializerExpression() Then
                With clone
                    .Status = .Status.With(succeeded:=False, VBFeaturesResources.Selection_can_t_be_parts_of_constant_initializer_expression)
                End With
            End If

            If selectionInfo.SelectionInExpression AndAlso commonNode.IsArgumentForByRefParameter(semanticModel, cancellationToken) Then
                With clone
                    .Status = .Status.With(succeeded:=True, VBFeaturesResources.Argument_used_for_ByRef_parameter_can_t_be_extracted_out)
                End With
            End If

            Dim containsAllStaticLocals = ContainsAllStaticLocalUsagesDefinedInSelectionIfExist(selectionInfo, semanticModel, cancellationToken)
            If Not containsAllStaticLocals Then
                With clone
                    .Status = .Status.With(succeeded:=True, VBFeaturesResources.all_static_local_usages_defined_in_the_selection_must_be_included_in_the_selection)
                End With
            End If

            ' if it is multiple statement case.
            If Not selectionInfo.SelectionInExpression AndAlso Not selectionInfo.SelectionInSingleStatement Then
                If commonNode.GetAncestorOrThis(Of WithBlockSyntax)() IsNot Nothing Then
                    If commonNode.GetImplicitMemberAccessExpressions(selectionInfo.FinalSpan).Any() Then
                        With clone
                            .Status = .Status.With(succeeded:=True, VBFeaturesResources.Implicit_member_access_can_t_be_included_in_the_selection_without_containing_statement)
                        End With
                    End If
                End If
            End If

            If Not selectionInfo.SelectionInExpression AndAlso Not selectionInfo.SelectionInSingleStatement Then
                If selectionInfo.FirstTokenInFinalSpan.GetAncestor(Of ExecutableStatementSyntax)() Is Nothing OrElse
                    selectionInfo.LastTokenInFinalSpan.GetAncestor(Of ExecutableStatementSyntax)() Is Nothing Then
                    With clone
                        .Status = .Status.With(succeeded:=False, VBFeaturesResources.Selection_must_be_part_of_executable_statements)
                    End With
                End If
            End If

            Return clone
        End Function

        Private Shared Function SelectionChanged(selectionInfo As SelectionInfo) As Boolean
            ' get final token that doesn't pointing to empty token
            Dim finalFirstToken = If(selectionInfo.FirstTokenInFinalSpan.Width = 0,
                                     selectionInfo.FirstTokenInFinalSpan.GetNextToken(),
                                     selectionInfo.FirstTokenInFinalSpan)

            Dim finalLastToken = If(selectionInfo.LastTokenInFinalSpan.Width = 0,
                                     selectionInfo.LastTokenInFinalSpan.GetPreviousToken(),
                                     selectionInfo.LastTokenInFinalSpan)

            ' adjust original tokens to point to statement terminator token if needed
            Dim originalFirstToken = selectionInfo.FirstTokenInOriginalSpan

            Dim originalLastToken = selectionInfo.LastTokenInOriginalSpan

            Return originalFirstToken <> finalFirstToken OrElse originalLastToken <> finalLastToken
        End Function

        Private Shared Function ContainsAllStaticLocalUsagesDefinedInSelectionIfExist(selectionInfo As SelectionInfo,
                                                                               semanticModel As SemanticModel,
                                                                               cancellationToken As CancellationToken) As Boolean
            If selectionInfo.FirstTokenInFinalSpan.GetAncestor(Of FieldDeclarationSyntax)() IsNot Nothing OrElse
               selectionInfo.FirstTokenInFinalSpan.GetAncestor(Of PropertyStatementSyntax)() IsNot Nothing Then
                ' static local can't exist in field initializer
                Return True
            End If

            Dim result As DataFlowAnalysis

            If selectionInfo.SelectionInExpression Then
                Dim expression = GetFinalTokenCommonRoot(selectionInfo).GetAncestorOrThis(Of ExpressionSyntax)()
                result = semanticModel.AnalyzeDataFlow(expression)
            Else
                Dim range = GetStatementRangeContainedInSpan(Of StatementSyntax)(
                    semanticModel.SyntaxTree.GetRoot(cancellationToken), GetControlFlowSpan(selectionInfo), cancellationToken)

                ' we can't determine valid range of statements, don't bother to do the analysis
                If range Is Nothing Then
                    Return True
                End If

                result = semanticModel.AnalyzeDataFlow(range.Value.Item1, range.Value.Item2)
            End If

            For Each symbol In result.VariablesDeclared
                Dim local = TryCast(symbol, ILocalSymbol)
                If local Is Nothing Then
                    Continue For
                End If

                If Not local.IsStatic Then
                    Continue For
                End If

                If result.WrittenOutside().Any(Function(s) Equals(s, local)) OrElse
result.ReadOutside().Any(Function(s) Equals(s, local)) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function GetFinalTokenCommonRoot(selection As SelectionInfo) As SyntaxNode
            Return GetCommonRoot(selection.FirstTokenInFinalSpan, selection.LastTokenInFinalSpan)
        End Function

        Private Shared Function GetCommonRoot(token1 As SyntaxToken, token2 As SyntaxToken) As SyntaxNode
            Return token1.GetCommonRoot(token2)
        End Function

        Private Shared Function FixUpFinalTokensAndAssignFinalSpan(selectionInfo As SelectionInfo,
                                                            root As SyntaxNode,
                                                            cancellationToken As CancellationToken) As SelectionInfo
            If selectionInfo.Status.Failed() Then
                Return selectionInfo
            End If

            Dim clone = selectionInfo.Clone()

            ' make sure we include statement terminator token if selection contains them
            Dim firstToken = selectionInfo.FirstTokenInFinalSpan
            Dim lastToken = selectionInfo.LastTokenInFinalSpan

            ' set final span
            Dim start = If(selectionInfo.OriginalSpan.Start <= firstToken.SpanStart, selectionInfo.OriginalSpan.Start, firstToken.FullSpan.Start)
            Dim [end] = If(lastToken.Span.End <= selectionInfo.OriginalSpan.End, selectionInfo.OriginalSpan.End, lastToken.Span.End)

            With clone
                .FinalSpan = GetAdjustedSpan(root, TextSpan.FromBounds(start, [end]))
                .FirstTokenInFinalSpan = firstToken
                .LastTokenInFinalSpan = lastToken
            End With

            Return clone
        End Function

        Private Shared Function AdjustFinalTokensIfNextStatement(selectionInfo As SelectionInfo,
                                                          semanticModel As SemanticModel,
                                                          cancellationToken As CancellationToken) As SelectionInfo
            If selectionInfo.Status.Failed() Then
                Return selectionInfo
            End If

            ' if last statement is next statement, make sure its corresponding loop statement is
            ' included
            Dim nextStatement = selectionInfo.LastTokenInFinalSpan.GetAncestor(Of NextStatementSyntax)()
            If nextStatement Is Nothing OrElse nextStatement.ControlVariables.Count < 2 Then
                Return selectionInfo
            End If

            Dim clone = selectionInfo.Clone()
            Dim outmostControlVariable = nextStatement.ControlVariables.Last

            Dim symbolInfo = semanticModel.GetSymbolInfo(outmostControlVariable, cancellationToken)
            Dim symbol = symbolInfo.GetBestOrAllSymbols().FirstOrDefault()

            ' can't find symbol for the control variable. don't provide extract method
            If symbol Is Nothing OrElse
               symbol.Locations.Length <> 1 OrElse
               Not symbol.Locations.First().IsInSource OrElse
               symbol.Locations.First().SourceTree IsNot semanticModel.SyntaxTree Then
                With clone
                    .Status = .Status.With(succeeded:=False, VBFeaturesResources.next_statement_control_variable_doesn_t_have_matching_declaration_statement)
                End With

                Return clone
            End If

            Dim startPosition = symbol.Locations.First().SourceSpan.Start
            Dim root = semanticModel.SyntaxTree.GetRoot(cancellationToken)
            Dim forBlock = root.FindToken(startPosition).GetAncestor(Of ForOrForEachBlockSyntax)()
            If forBlock Is Nothing Then
                With clone
                    .Status = .Status.With(succeeded:=False, VBFeaturesResources.next_statement_control_variable_doesn_t_have_matching_declaration_statement)
                End With

                Return clone
            End If

            Dim firstStatement = forBlock.ForOrForEachStatement
            With clone
                .SelectionInExpression = False
                .SelectionInSingleStatement = forBlock.Span.Contains(nextStatement.Span)
                .FirstTokenInFinalSpan = firstStatement.GetFirstToken(includeZeroWidth:=True)
                .LastTokenInFinalSpan = nextStatement.GetLastToken(includeZeroWidth:=True)
            End With

            Return clone
        End Function

        Private Shared Function AdjustFinalTokensBasedOnContext(selectionInfo As SelectionInfo,
                                                         semanticModel As SemanticModel,
                                                         cancellationToken As CancellationToken) As SelectionInfo
            If selectionInfo.Status.Failed() Then
                Return selectionInfo
            End If

            ' don't need to adjust anything if it is multi-statements case
            If (Not selectionInfo.SelectionInExpression) AndAlso (Not selectionInfo.SelectionInSingleStatement) Then
                Return selectionInfo
            End If

            Dim clone = selectionInfo.Clone()

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
                With clone
                    .Status = New OperationStatus(succeeded:=False, VBFeaturesResources.Selection_doesn_t_contain_any_valid_node)
                    .FirstTokenInFinalSpan = Nothing
                    .LastTokenInFinalSpan = Nothing
                End With

                Return clone
            End If

            With clone
                .SelectionInExpression = TypeOf firstValidNode Is ExpressionSyntax
                .SelectionInSingleStatement = TypeOf firstValidNode Is StatementSyntax
                .FirstTokenInFinalSpan = firstValidNode.GetFirstToken(includeZeroWidth:=True)
                .LastTokenInFinalSpan = firstValidNode.GetLastToken(includeZeroWidth:=True)
            End With

            Return clone
        End Function

        Private Shared Function AssignInitialFinalTokens(selectionInfo As SelectionInfo, root As SyntaxNode, cancellationToken As CancellationToken) As SelectionInfo
            If selectionInfo.Status.Failed() Then
                Return selectionInfo
            End If

            Dim clone = selectionInfo.Clone()

            If selectionInfo.SelectionInExpression Then
                ' prefer outer statement or expression if two has same span
                Dim outerNode = selectionInfo.CommonRootFromOriginalSpan.GetOutermostNodeWithSameSpan(Function(n) TypeOf n Is StatementSyntax OrElse TypeOf n Is ExpressionSyntax)

                ' simple expression case
                With clone
                    .SelectionInExpression = TypeOf outerNode Is ExpressionSyntax
                    .SelectionInSingleStatement = TypeOf outerNode Is StatementSyntax
                    .FirstTokenInFinalSpan = outerNode.GetFirstToken(includeZeroWidth:=True)
                    .LastTokenInFinalSpan = outerNode.GetLastToken(includeZeroWidth:=True)
                End With

                Return clone
            End If

            Dim range = GetStatementRangeContainingSpan(Of StatementSyntax)(
                VisualBasicSyntaxFacts.Instance,
                root, TextSpan.FromBounds(selectionInfo.FirstTokenInOriginalSpan.SpanStart, selectionInfo.LastTokenInOriginalSpan.Span.End),
                cancellationToken)

            If range Is Nothing Then
                With clone
                    .Status = clone.Status.With(succeeded:=False, VBFeaturesResources.no_valid_statement_range_to_extract_out)
                End With

                Return clone
            End If

            Dim statement1 = DirectCast(range.Value.Item1, StatementSyntax)
            Dim statement2 = DirectCast(range.Value.Item2, StatementSyntax)

            If statement1 Is statement2 Then
                ' check one more time to see whether it is an expression case
                Dim expression = selectionInfo.CommonRootFromOriginalSpan.GetAncestor(Of ExpressionSyntax)()
                If expression IsNot Nothing AndAlso statement1.Span.Contains(expression.Span) Then
                    With clone
                        .SelectionInExpression = True
                        .FirstTokenInFinalSpan = expression.GetFirstToken(includeZeroWidth:=True)
                        .LastTokenInFinalSpan = expression.GetLastToken(includeZeroWidth:=True)
                    End With

                    Return clone
                End If

                ' single statement case
                ' current way to find out a statement that can be extracted out
                Dim singleStatement = statement1.GetAncestorsOrThis(Of StatementSyntax)().FirstOrDefault(
                    Function(s) s.Parent IsNot Nothing AndAlso s.Parent.IsStatementContainerNode() AndAlso s.Parent.ContainStatement(s))

                If singleStatement Is Nothing Then
                    With clone
                        .Status = clone.Status.With(succeeded:=False, VBFeaturesResources.no_valid_statement_range_to_extract_out)
                    End With

                    Return clone
                End If

                With clone
                    .SelectionInSingleStatement = True
                    .FirstTokenInFinalSpan = singleStatement.GetFirstToken(includeZeroWidth:=True)
                    .LastTokenInFinalSpan = singleStatement.GetLastToken(includeZeroWidth:=True)
                End With

                Return clone
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
                                           .Where(Function(n) TypeOf n Is ExpressionSyntax OrElse TypeOf n Is StatementSyntax) _
                                           .First()

                    ' single statement case
                    With clone
                        .SelectionInExpression = TypeOf parent Is ExpressionSyntax
                        .SelectionInSingleStatement = TypeOf parent Is StatementSyntax
                        .FirstTokenInFinalSpan = parent.GetFirstToken()
                        .LastTokenInFinalSpan = parent.GetLastToken()
                    End With

                    Return clone
                End If
            End If

            With clone
                .FirstTokenInFinalSpan = statement1.GetFirstToken(includeZeroWidth:=True)
                .LastTokenInFinalSpan = statement2.GetLastToken(includeZeroWidth:=True)
            End With

            Return clone
        End Function

        Private Function GetInitialSelectionInfo(root As SyntaxNode) As SelectionInfo
            Dim adjustedSpan = GetAdjustedSpan(root, Me.OriginalSpan)
            Dim firstTokenInSelection = root.FindTokenOnRightOfPosition(adjustedSpan.Start, includeSkipped:=False)
            Dim lastTokenInSelection = root.FindTokenOnLeftOfPosition(adjustedSpan.End, includeSkipped:=False)

            If firstTokenInSelection.Kind = SyntaxKind.None OrElse lastTokenInSelection.Kind = SyntaxKind.None Then
                Return New SelectionInfo With {.Status = New OperationStatus(succeeded:=False, FeaturesResources.Invalid_selection), .OriginalSpan = adjustedSpan}
            End If

            If firstTokenInSelection <> lastTokenInSelection AndAlso
               firstTokenInSelection.Span.End > lastTokenInSelection.SpanStart Then
                Return New SelectionInfo With {.Status = New OperationStatus(succeeded:=False, FeaturesResources.Invalid_selection), .OriginalSpan = adjustedSpan}
            End If

            If (Not adjustedSpan.Contains(firstTokenInSelection.Span)) AndAlso (Not adjustedSpan.Contains(lastTokenInSelection.Span)) Then
                Return New SelectionInfo With
                       {
                           .Status = New OperationStatus(succeeded:=False, FeaturesResources.Selection_does_not_contain_a_valid_token),
                           .OriginalSpan = adjustedSpan,
                           .FirstTokenInOriginalSpan = firstTokenInSelection,
                           .LastTokenInOriginalSpan = lastTokenInSelection
                       }
            End If

            If (Not firstTokenInSelection.UnderValidContext()) OrElse (Not lastTokenInSelection.UnderValidContext()) Then
                Return New SelectionInfo With
                       {
                           .Status = New OperationStatus(succeeded:=False, FeaturesResources.No_valid_selection_to_perform_extraction),
                           .OriginalSpan = adjustedSpan,
                           .FirstTokenInOriginalSpan = firstTokenInSelection,
                           .LastTokenInOriginalSpan = lastTokenInSelection
                       }
            End If

            Dim commonRoot = GetCommonRoot(firstTokenInSelection, lastTokenInSelection)
            If commonRoot Is Nothing Then
                Return New SelectionInfo With
                       {
                           .Status = New OperationStatus(succeeded:=False, FeaturesResources.No_common_root_node_for_extraction),
                           .OriginalSpan = adjustedSpan,
                           .FirstTokenInOriginalSpan = firstTokenInSelection,
                           .LastTokenInOriginalSpan = lastTokenInSelection
                       }
            End If

            If Not commonRoot.ContainedInValidType() Then
                Return New SelectionInfo With
                    {
                        .Status = New OperationStatus(succeeded:=False, FeaturesResources.Selection_not_contained_inside_a_type),
                        .OriginalSpan = adjustedSpan,
                        .FirstTokenInOriginalSpan = firstTokenInSelection,
                        .LastTokenInOriginalSpan = lastTokenInSelection
                    }
            End If

            Dim selectionInExpression = TypeOf commonRoot Is ExpressionSyntax AndAlso
                                        commonRoot.GetFirstToken(includeZeroWidth:=True) = firstTokenInSelection AndAlso
                                        commonRoot.GetLastToken(includeZeroWidth:=True) = lastTokenInSelection

            If (Not selectionInExpression) AndAlso (Not commonRoot.UnderValidContext()) Then
                Return New SelectionInfo With
                       {
                           .Status = New OperationStatus(succeeded:=False, FeaturesResources.No_valid_selection_to_perform_extraction),
                           .OriginalSpan = adjustedSpan,
                           .FirstTokenInOriginalSpan = firstTokenInSelection,
                           .LastTokenInOriginalSpan = lastTokenInSelection
                       }
            End If

            ' make sure type block enclosing the selection exist
            If commonRoot.GetAncestor(Of TypeBlockSyntax)() Is Nothing Then
                Return New SelectionInfo With
                       {
                           .Status = New OperationStatus(succeeded:=False, FeaturesResources.No_valid_selection_to_perform_extraction),
                           .OriginalSpan = adjustedSpan,
                           .FirstTokenInOriginalSpan = firstTokenInSelection,
                           .LastTokenInOriginalSpan = lastTokenInSelection
                       }
            End If

            Return New SelectionInfo With
                   {
                       .Status = OperationStatus.SucceededStatus,
                       .OriginalSpan = adjustedSpan,
                       .CommonRootFromOriginalSpan = commonRoot,
                       .SelectionInExpression = selectionInExpression,
                       .FirstTokenInOriginalSpan = firstTokenInSelection,
                       .LastTokenInOriginalSpan = lastTokenInSelection
                   }
        End Function

        Public Overrides Function ContainsNonReturnExitPointsStatements(jumpsOutOfRegion As IEnumerable(Of SyntaxNode)) As Boolean
            Dim returnStatement = False
            Dim exitStatement = False

            For Each statement In jumpsOutOfRegion
                If TypeOf statement Is ReturnStatementSyntax Then
                    returnStatement = True
                ElseIf TypeOf statement Is ExitStatementSyntax Then
                    exitStatement = True
                Else
                    Return True
                End If
            Next

            If exitStatement Then
                Return Not returnStatement
            End If

            Return False
        End Function

        Public Overrides Function GetOuterReturnStatements(commonRoot As SyntaxNode, jumpsOutOfRegionStatements As IEnumerable(Of SyntaxNode)) As IEnumerable(Of SyntaxNode)
            Dim returnStatements = jumpsOutOfRegionStatements.Where(Function(n) TypeOf n Is ReturnStatementSyntax OrElse TypeOf n Is ExitStatementSyntax)

            Dim container = commonRoot.GetAncestorsOrThis(Of SyntaxNode)().Where(Function(a) a.IsReturnableConstruct()).FirstOrDefault()
            If container Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
            End If

            Dim returnableConstructPairs = returnStatements.
                                                Select(Function(r) (r, r.GetAncestors(Of SyntaxNode)().Where(Function(a) a.IsReturnableConstruct()).FirstOrDefault())).
                                                Where(Function(p) p.Item2 IsNot Nothing)

            ' now filter return statements to only include the one under outmost container
            Return returnableConstructPairs.Where(Function(p) p.Item2 Is container).Select(Function(p) p.Item1)
        End Function

        Public Overrides Function IsFinalSpanSemanticallyValidSpan(root As SyntaxNode,
                                                                   textSpan As TextSpan,
                                                                   returnStatements As IEnumerable(Of SyntaxNode),
                                                                   cancellationToken As CancellationToken) As Boolean

            ' do quick check to make sure we are under sub (no return value) container. otherwise, there is no point to anymore checks.
            If returnStatements.Any(Function(s)
                                        Return s.TypeSwitch(
                                            Function(e As ExitStatementSyntax) e.BlockKeyword.Kind <> SyntaxKind.SubKeyword,
                                            Function(r As ReturnStatementSyntax) r.Expression IsNot Nothing,
                                            Function(n As SyntaxNode) True)
                                    End Function) Then
                Return False
            End If

            ' check whether selection reaches the end of the container
            Dim lastToken = root.FindToken(textSpan.End)
            If lastToken.Kind = SyntaxKind.None Then
                Return False
            End If

            Dim nextToken = lastToken.GetNextToken(includeZeroWidth:=True)

            Dim container = nextToken.GetAncestors(Of SyntaxNode).Where(Function(n) n.IsReturnableConstruct()).FirstOrDefault()
            If container Is Nothing Then
                Return False
            End If

            Dim match = If(TryCast(container, MethodBlockBaseSyntax)?.EndBlockStatement.EndKeyword = nextToken, False) OrElse
                        If(TryCast(container, MultiLineLambdaExpressionSyntax)?.EndSubOrFunctionStatement.EndKeyword = nextToken, False)

            If Not match Then
                Return False
            End If

            If TryCast(container, MethodBlockBaseSyntax)?.BlockStatement.Kind = SyntaxKind.SubStatement Then
                Return True
            ElseIf TryCast(container, MultiLineLambdaExpressionSyntax)?.SubOrFunctionHeader.Kind = SyntaxKind.SubLambdaHeader Then
                Return True
            Else
                Return False
            End If
        End Function

        Private Shared Function GetAdjustedSpan(root As SyntaxNode, textSpan As TextSpan) As TextSpan
            ' quick exit
            If textSpan.IsEmpty OrElse textSpan.End = 0 Then
                Return textSpan
            End If

            ' regular column 0 check
            Dim line = root.GetText().Lines.GetLineFromPosition(textSpan.End)
            If line.Start <> textSpan.End Then
                Return textSpan
            End If

            ' previous line
            Contract.ThrowIfFalse(line.LineNumber > 0)
            Dim previousLine = root.GetText().Lines(line.LineNumber - 1)

            ' check whether end of previous line is last token of a statement. if it is, don't do anything
            If root.FindTokenOnLeftOfPosition(previousLine.End).IsLastTokenOfStatement() Then
                Return textSpan
            End If

            ' move end position of the selection
            Return TextSpan.FromBounds(textSpan.Start, previousLine.End)
        End Function
    End Class
End Namespace
