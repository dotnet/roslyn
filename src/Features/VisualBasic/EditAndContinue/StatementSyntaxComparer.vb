' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue

    Partial Friend MustInherit Class StatementSyntaxComparer
        Inherits SyntaxComparer

#Region "Tree Traversal"
        Protected NotOverridable Overrides Function TryGetParent(node As SyntaxNode, ByRef parent As SyntaxNode) As Boolean
            parent = node.Parent
            While parent IsNot Nothing AndAlso Not HasLabel(parent.Kind)
                parent = parent.Parent
            End While

            Return parent IsNot Nothing
        End Function

        Protected Shared Iterator Function EnumerateChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            For Each child In node.ChildNodesAndTokens()
                Dim childNode = child.AsNode()
                If childNode IsNot Nothing Then
                    If GetLabelImpl(childNode) <> Label.Ignored Then
                        Yield childNode
                    Else
                        For Each descendant In childNode.DescendantNodesAndTokens(AddressOf SyntaxUtilities.IsNotLambda)
                            If SyntaxUtilities.IsLambda(descendant.Kind) Then
                                Yield descendant.AsNode()
                            End If
                        Next
                    End If
                End If
            Next
        End Function
#End Region

#Region "Labels"
        ' Assumptions:
        ' - Each listed label corresponds to one or more syntax kinds.
        ' - Nodes with same labels might produce Update edits, nodes with different labels don't. 
        ' - If IsTiedToParent(label) is true for a label then all its possible parent labels must precede the label.
        '   (i.e. both MethodDeclaration and TypeDeclaration must precede TypeParameter label).
        Friend Enum Label
            BodyBlock

            ' We need to represent sub/function/ctor/accessor/operator declaration 
            ' begin And End statements since they may be active statements.
            BodyBegin                       ' tied to parent

            TryBlock
            TryStatement                    ' tied to parent
            CatchBlock                      ' tied to parent 
            CatchStatement                  ' tied to parent
            FinallyBlock                    ' tied to parent 
            FinallyStatement                ' tied to parent
            CatchFilterClause               ' tied to parent 
            EndTryStatement                 ' tied to parent 

            ForBlock
            ForEachBlock
            ForEachStatement                ' tied to parent
            ForStatement                    ' tied to parent
            ForStepClause                   ' tied to parent
            NextStatement                   ' tied to parent

            UsingBlock
            UsingStatement                  ' tied to parent
            EndUsingStatement               ' tied to parent

            SyncLockBlock
            SyncLockStatement               ' tied to parent 
            EndSyncLockStatement            ' tied to parent

            WithBlock
            WithStatement                   ' tied to parent 
            EndWithStatement                ' tied to parent

            DoWhileBlock
            DoWhileStatement                ' tied to parent
            EndLoop                         ' tied to parent
            WhileOrUntilClause                ' tied to parent

            IfBlock
            IfStatement                     ' tied to parent 
            ElseIfBlock                     ' tied to parent 
            ElseIfStatement                 ' tied to parent 
            ElseBlock                       ' tied to parent
            ElseStatement                   ' tied to parent
            EndIfStatement                  ' tied to parent

            SelectBlock
            SelectStatement                 ' tied to parent
            CaseBlock                       ' tied to parent
            CaseStatement                   ' tied to parent
            CaseClause                      ' tied to parent
            EndSelectStatement              ' tied to parent

            ReDimStatement
            ReDimClause                     ' tied to parent 

            ' Exit Sub|Function|Operator|Property|For|While|Do|Select|Try
            ExitStatement

            ' Continue While|Do|For
            ContinueStatement

            ' Throw, Throw expr
            ThrowStatement
            ErrorStatement

            ' Return, Return expr
            ReturnStatement

            OnErrorStatement
            ResumeStatement

            ' GoTo, Stop, End
            GoToStatement

            LabelStatement
            EraseStatement
            ExpressionStatement
            AssignmentStatement
            EventHandlerStatement

            YieldStatement

            LocalDeclarationStatement        ' tied to parent 
            LocalVariableDeclarator          ' tied to parent 
            LocalVariableName                ' tied to parent 

            Lambda
            LambdaBodyBegin                  ' tied to parent
            WhereClauseLambda
            CollectionVariableLambda
            FunctionAggregationLambda
            RangeVariableLambda
            PartitionWhileLambda
            OrderingLambda
            JoinConditionLambda

            BodyEnd                          ' tied to parent

            ' helpers
            Count
            Ignored = IgnoredNode
        End Enum

        Private Overloads Shared Function TiedToAncestor(label As Label) As Integer
            Select Case label
                Case Label.BodyBegin,
                     Label.LambdaBodyBegin,
                     Label.BodyEnd,
                     Label.TryStatement,
                     Label.CatchBlock,
                     Label.CatchStatement,
                     Label.FinallyBlock,
                     Label.FinallyStatement,
                     Label.CatchFilterClause,
                     Label.EndTryStatement,
                     Label.ForEachStatement,
                     Label.ForStatement,
                     Label.ForStepClause,
                     Label.NextStatement,
                     Label.UsingStatement,
                     Label.EndUsingStatement,
                     Label.SyncLockStatement,
                     Label.EndSyncLockStatement,
                     Label.WithStatement,
                     Label.EndWithStatement,
                     Label.DoWhileStatement,
                     Label.WhileOrUntilClause,
                     Label.EndLoop,
                     Label.IfStatement,
                     Label.ElseIfBlock,
                     Label.ElseIfStatement,
                     Label.ElseBlock,
                     Label.ElseStatement,
                     Label.EndIfStatement,
                     Label.SelectStatement,
                     Label.CaseBlock,
                     Label.CaseStatement,
                     Label.CaseClause,
                     Label.EndSelectStatement,
                     Label.ReDimClause,
                     Label.LocalDeclarationStatement,
                     Label.LocalVariableDeclarator,
                     Label.LocalVariableName
                    Return 1

                Case Else
                    Return 0
            End Select
        End Function

        Protected Shared Function NonRootHasChildren(node As SyntaxNode) As Boolean
            ' Leaves are labeled statements that don't have a labeled child.
            ' A non-labeled statement may not be leave since it may contain a lambda.
            Dim isLeaf As Boolean
            Classify(node.Kind, isLeaf)
            Return Not isLeaf
        End Function

        Friend Shared Function Classify(kind As SyntaxKind, ByRef isLeaf As Boolean) As Label
            isLeaf = False

            Select Case kind
                Case SyntaxKind.SubBlock,
                     SyntaxKind.ConstructorBlock,
                     SyntaxKind.FunctionBlock,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.GetAccessorBlock,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RaiseEventAccessorBlock
                    Return Label.BodyBlock

                Case SyntaxKind.SubStatement,
                     SyntaxKind.SubNewStatement,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.OperatorStatement,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorStatement
                    isLeaf = True
                    Return Label.BodyBegin

                Case SyntaxKind.SubLambdaHeader,
                     SyntaxKind.FunctionLambdaHeader
                    isLeaf = True
                    Return Label.LambdaBodyBegin

                Case SyntaxKind.EndSubStatement,
                     SyntaxKind.EndFunctionStatement,
                     SyntaxKind.EndOperatorStatement,
                     SyntaxKind.EndGetStatement,
                     SyntaxKind.EndSetStatement,
                     SyntaxKind.EndAddHandlerStatement,
                     SyntaxKind.EndRemoveHandlerStatement,
                     SyntaxKind.EndRaiseEventStatement
                    isLeaf = True
                    Return Label.BodyEnd


                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.WhileBlock
                    Return Label.DoWhileBlock

                Case SyntaxKind.SimpleDoStatement, SyntaxKind.DoWhileStatement, SyntaxKind.DoUntilStatement,
                     SyntaxKind.WhileStatement
                    Return Label.DoWhileStatement

                Case SyntaxKind.WhileClause,
                     SyntaxKind.UntilClause
                    Return Label.WhileOrUntilClause

                Case SyntaxKind.SimpleLoopStatement, SyntaxKind.LoopWhileStatement, SyntaxKind.LoopUntilStatement,
                     SyntaxKind.EndWhileStatement
                    Return Label.EndLoop


                Case SyntaxKind.ForBlock
                    Return Label.ForBlock

                Case SyntaxKind.ForEachBlock
                    Return Label.ForEachBlock

                Case SyntaxKind.ForStatement
                    Return Label.ForStatement

                Case SyntaxKind.ForEachStatement
                    Return Label.ForEachStatement

                Case SyntaxKind.ForStepClause
                    Return Label.ForStepClause

                Case SyntaxKind.NextStatement
                    isLeaf = True
                    Return Label.NextStatement


                Case SyntaxKind.UsingBlock
                    Return Label.UsingBlock

                Case SyntaxKind.UsingStatement
                    Return Label.UsingStatement

                Case SyntaxKind.EndUsingStatement
                    isLeaf = True
                    Return Label.EndUsingStatement


                Case SyntaxKind.SyncLockBlock
                    Return Label.SyncLockBlock

                Case SyntaxKind.SyncLockStatement
                    Return Label.SyncLockStatement

                Case SyntaxKind.EndSyncLockStatement
                    isLeaf = True
                    Return Label.EndSyncLockStatement


                Case SyntaxKind.WithBlock
                    Return Label.WithBlock

                Case SyntaxKind.WithStatement
                    Return Label.WithStatement

                Case SyntaxKind.EndWithStatement
                    isLeaf = True
                    Return Label.EndWithStatement


                Case SyntaxKind.LocalDeclarationStatement
                    Return Label.LocalDeclarationStatement

                Case SyntaxKind.VariableDeclarator
                    Return Label.LocalVariableDeclarator

                Case SyntaxKind.ModifiedIdentifier
                    Return Label.LocalVariableName


                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement
                    Return Label.IfBlock

                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement
                    Return Label.IfBlock

                Case SyntaxKind.IfStatement
                    Return Label.IfStatement

                Case SyntaxKind.ElseIfBlock
                    Return Label.ElseIfBlock

                Case SyntaxKind.ElseIfStatement
                    Return Label.ElseIfStatement

                Case SyntaxKind.ElseBlock,
                     SyntaxKind.SingleLineElseClause
                    Return Label.ElseBlock

                Case SyntaxKind.ElseStatement
                    isLeaf = True
                    Return Label.ElseStatement

                Case SyntaxKind.EndIfStatement
                    isLeaf = True
                    Return Label.EndIfStatement


                Case SyntaxKind.TryBlock
                    Return Label.TryBlock

                Case SyntaxKind.TryBlock
                    Return Label.TryBlock

                Case SyntaxKind.TryStatement
                    Return Label.TryStatement

                Case SyntaxKind.CatchBlock
                    Return Label.CatchBlock

                Case SyntaxKind.CatchStatement
                    Return Label.CatchStatement

                Case SyntaxKind.FinallyBlock
                    Return Label.FinallyBlock

                Case SyntaxKind.FinallyStatement
                    Return Label.FinallyStatement

                Case SyntaxKind.CatchFilterClause
                    Return Label.CatchFilterClause

                Case SyntaxKind.EndTryStatement
                    isLeaf = True
                    Return Label.EndTryStatement


                Case SyntaxKind.ErrorStatement
                    isLeaf = True
                    Return Label.ErrorStatement

                Case SyntaxKind.ThrowStatement
                    Return Label.ThrowStatement

                Case SyntaxKind.OnErrorGoToZeroStatement,
                     SyntaxKind.OnErrorGoToMinusOneStatement,
                     SyntaxKind.OnErrorGoToLabelStatement,
                     SyntaxKind.OnErrorResumeNextStatement
                    Return Label.OnErrorStatement

                Case SyntaxKind.ResumeStatement,
                     SyntaxKind.ResumeLabelStatement,
                     SyntaxKind.ResumeNextStatement
                    Return Label.ResumeStatement

                Case SyntaxKind.SelectBlock
                    Return Label.SelectBlock

                Case SyntaxKind.SelectStatement
                    Return Label.SelectStatement

                Case SyntaxKind.CaseBlock,
                     SyntaxKind.CaseElseBlock
                    Return Label.CaseBlock

                Case SyntaxKind.CaseStatement,
                     SyntaxKind.CaseElseStatement
                    Return Label.CaseStatement

                Case SyntaxKind.ElseCaseClause,
                     SyntaxKind.SimpleCaseClause,
                     SyntaxKind.RangeCaseClause,
                     SyntaxKind.CaseEqualsClause,
                     SyntaxKind.CaseNotEqualsClause,
                     SyntaxKind.CaseLessThanClause,
                     SyntaxKind.CaseLessThanOrEqualClause,
                     SyntaxKind.CaseGreaterThanOrEqualClause,
                     SyntaxKind.CaseGreaterThanClause
                    Return Label.CaseClause

                Case SyntaxKind.EndSelectStatement
                    isLeaf = True
                    Return Label.EndSelectStatement


                Case SyntaxKind.ExitForStatement,
                     SyntaxKind.ExitDoStatement,
                     SyntaxKind.ExitWhileStatement,
                     SyntaxKind.ExitSelectStatement,
                     SyntaxKind.ExitTryStatement,
                     SyntaxKind.ExitSubStatement,
                     SyntaxKind.ExitFunctionStatement,
                     SyntaxKind.ExitOperatorStatement,
                     SyntaxKind.ExitPropertyStatement
                    isLeaf = True
                    Return Label.ExitStatement

                Case SyntaxKind.ContinueWhileStatement,
                     SyntaxKind.ContinueDoStatement,
                     SyntaxKind.ContinueForStatement
                    isLeaf = True
                    Return Label.ContinueStatement

                Case SyntaxKind.ReturnStatement
                    Return Label.ReturnStatement


                Case SyntaxKind.GoToStatement,
                     SyntaxKind.StopStatement,
                     SyntaxKind.EndStatement
                    isLeaf = True
                    Return Label.GoToStatement

                Case SyntaxKind.LabelStatement
                    isLeaf = True
                    Return Label.LabelStatement

                Case SyntaxKind.EraseStatement
                    isLeaf = True
                    Return Label.EraseStatement

                Case SyntaxKind.ExpressionStatement,
                     SyntaxKind.CallStatement
                    Return Label.ExpressionStatement

                Case SyntaxKind.MidAssignmentStatement,
                     SyntaxKind.SimpleAssignmentStatement,
                     SyntaxKind.AddAssignmentStatement,
                     SyntaxKind.SubtractAssignmentStatement,
                     SyntaxKind.MultiplyAssignmentStatement,
                     SyntaxKind.DivideAssignmentStatement,
                     SyntaxKind.IntegerDivideAssignmentStatement,
                     SyntaxKind.ExponentiateAssignmentStatement,
                     SyntaxKind.LeftShiftAssignmentStatement,
                     SyntaxKind.RightShiftAssignmentStatement,
                     SyntaxKind.ConcatenateAssignmentStatement
                    Return Label.AssignmentStatement

                Case SyntaxKind.AddHandlerStatement,
                     SyntaxKind.RemoveHandlerStatement,
                     SyntaxKind.RaiseEventStatement
                    Return Label.EventHandlerStatement

                Case SyntaxKind.ReDimStatement,
                     SyntaxKind.ReDimPreserveStatement
                    Return Label.ReDimStatement

                Case SyntaxKind.RedimClause
                    Return Label.ReDimClause

                Case SyntaxKind.YieldStatement
                    Return Label.YieldStatement


                Case SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression
                    isLeaf = True
                    Return Label.Lambda

                Case SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    isLeaf = True
                    Return Label.Ignored

                Case SyntaxKind.WhereClause
                    isLeaf = True
                    Return Label.WhereClauseLambda

                Case SyntaxKind.CollectionRangeVariable
                    isLeaf = True
                    Return Label.CollectionVariableLambda

                Case SyntaxKind.FunctionAggregation
                    isLeaf = True
                    Return Label.FunctionAggregationLambda

                Case SyntaxKind.ExpressionRangeVariable
                    isLeaf = True
                    Return Label.RangeVariableLambda

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    isLeaf = True
                    Return Label.PartitionWhileLambda

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    isLeaf = True
                    Return Label.OrderingLambda

                Case SyntaxKind.JoinCondition
                    isLeaf = True
                    Return Label.JoinConditionLambda

                Case Else
                    Return Label.Ignored
            End Select
        End Function

        Protected NotOverridable Overrides Function GetLabel(node As SyntaxNode) As Integer
            Return GetLabelImpl(node)
        End Function

        Friend Shared Function GetLabelImpl(node As SyntaxNode) As Label
            Dim isLeaf As Boolean
            Return Classify(node.Kind, isLeaf)
        End Function

        Friend Shared Function HasLabel(kind As SyntaxKind) As Boolean
            Dim isLeaf As Boolean
            Return Classify(kind, isLeaf) <> Label.Ignored
        End Function

        Protected NotOverridable Overrides ReadOnly Property LabelCount As Integer
            Get
                Return Label.Count
            End Get
        End Property

        Protected NotOverridable Overrides Function TiedToAncestor(label As Integer) As Integer
            Return TiedToAncestor(CType(label, Label))
        End Function

#End Region

#Region "Comparisons"

        Public NotOverridable Overrides Function ValuesEqual(left As SyntaxNode, right As SyntaxNode) As Boolean
            Dim ignoreChildFunction As Func(Of SyntaxKind, Boolean)
            Select Case left.Kind
                Case Else
                    If NonRootHasChildren(left) Then
                        ignoreChildFunction = AddressOf HasLabel
                    Else
                        ignoreChildFunction = Nothing
                    End If
            End Select

            Return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction)
        End Function

        Protected NotOverridable Overrides Function TryComputeWeightedDistance(leftNode As SyntaxNode, rightNode As SyntaxNode, ByRef distance As Double) As Boolean
            Select Case leftNode.Kind
                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock,
                     SyntaxKind.WhileBlock

                    Dim getParts = Function(node As SyntaxNode)
                                       Select Case node.Kind
                                           Case SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock
                                               Dim block = DirectCast(node, DoLoopBlockSyntax)
                                               Return New With {.Begin = CType(block.LoopStatement, StatementSyntax), block.Statements}

                                           Case SyntaxKind.WhileBlock
                                               Dim block = DirectCast(node, WhileBlockSyntax)
                                               Return New With {.Begin = CType(block.WhileStatement, StatementSyntax), block.Statements}

                                           Case Else
                                               Dim block = DirectCast(node, DoLoopBlockSyntax)
                                               Return New With {.Begin = CType(block.DoStatement, StatementSyntax), block.Statements}
                                       End Select
                                   End Function

                    Dim leftParts = getParts(leftNode)
                    Dim rightParts = getParts(rightNode)

                    distance = ComputeWeightedDistance(leftParts.Begin, leftParts.Statements, rightParts.Begin, rightParts.Statements)
                    Return True

                Case SyntaxKind.ForBlock
                    Dim leftFor = DirectCast(leftNode, ForOrForEachBlockSyntax)
                    Dim rightFor = DirectCast(rightNode, ForOrForEachBlockSyntax)
                    Dim leftStatement = DirectCast(leftFor.ForOrForEachStatement, ForStatementSyntax)
                    Dim rightStatement = DirectCast(rightFor.ForOrForEachStatement, ForStatementSyntax)

                    distance = ComputeWeightedDistance(leftStatement.ControlVariable,
                                                       leftFor.ForOrForEachStatement,
                                                       leftFor.Statements,
                                                       rightStatement.ControlVariable,
                                                       rightFor.ForOrForEachStatement,
                                                       rightFor.Statements)
                    Return True

                Case SyntaxKind.ForEachBlock
                    Dim leftFor = DirectCast(leftNode, ForOrForEachBlockSyntax)
                    Dim rightFor = DirectCast(rightNode, ForOrForEachBlockSyntax)
                    Dim leftStatement = DirectCast(leftFor.ForOrForEachStatement, ForEachStatementSyntax)
                    Dim rightStatement = DirectCast(rightFor.ForOrForEachStatement, ForEachStatementSyntax)

                    distance = ComputeWeightedDistance(leftStatement.ControlVariable,
                                                       leftFor.ForOrForEachStatement,
                                                       leftFor.Statements,
                                                       rightStatement.ControlVariable,
                                                       rightFor.ForOrForEachStatement,
                                                       rightFor.Statements)
                    Return True

                Case SyntaxKind.UsingBlock
                    Dim leftUsing = DirectCast(leftNode, UsingBlockSyntax)
                    Dim rightUsing = DirectCast(rightNode, UsingBlockSyntax)
                    distance = ComputeWeightedDistance(leftUsing.UsingStatement.Expression, leftUsing.Statements, rightUsing.UsingStatement.Expression, rightUsing.Statements)
                    Return True

                Case SyntaxKind.WithBlock
                    Dim leftWith = DirectCast(leftNode, WithBlockSyntax)
                    Dim rightWith = DirectCast(rightNode, WithBlockSyntax)
                    distance = ComputeWeightedDistance(leftWith.WithStatement.Expression, leftWith.Statements, rightWith.WithStatement.Expression, rightWith.Statements)
                    Return True

                Case SyntaxKind.SyncLockBlock
                    Dim leftLock = DirectCast(leftNode, SyncLockBlockSyntax)
                    Dim rightLock = DirectCast(rightNode, SyncLockBlockSyntax)
                    distance = ComputeWeightedDistance(leftLock.SyncLockStatement.Expression, leftLock.Statements, rightLock.SyncLockStatement.Expression, rightLock.Statements)
                    Return True

                Case SyntaxKind.VariableDeclarator
                    distance = ComputeDistance(DirectCast(leftNode, VariableDeclaratorSyntax).Names, DirectCast(rightNode, VariableDeclaratorSyntax).Names)
                    Return True

                Case SyntaxKind.MultiLineIfBlock,
                     SyntaxKind.SingleLineIfStatement

                    Dim ifParts = Function(node As SyntaxNode)
                                      If node.IsKind(SyntaxKind.MultiLineIfBlock) Then
                                          Dim part = DirectCast(node, MultiLineIfBlockSyntax)
                                          Return New With {part.IfStatement.Condition, part.Statements}
                                      Else
                                          Dim part = DirectCast(node, SingleLineIfStatementSyntax)
                                          Return New With {part.Condition, part.Statements}
                                      End If
                                  End Function

                    Dim leftIf = ifParts(leftNode)
                    Dim rightIf = ifParts(rightNode)
                    distance = ComputeWeightedDistance(leftIf.Condition, leftIf.Statements, rightIf.Condition, rightIf.Statements)
                    Return True

                Case SyntaxKind.ElseIfBlock
                    Dim leftElseIf = DirectCast(leftNode, ElseIfBlockSyntax)
                    Dim rightElseIf = DirectCast(rightNode, ElseIfBlockSyntax)
                    distance = ComputeWeightedDistance(leftElseIf.ElseIfStatement.Condition, leftElseIf.Statements, rightElseIf.ElseIfStatement.Condition, rightElseIf.Statements)
                    Return True

                Case SyntaxKind.ElseBlock,
                     SyntaxKind.SingleLineElseClause

                    Dim elseStatements = Function(node As SyntaxNode)
                                             If node.IsKind(SyntaxKind.ElseBlock) Then
                                                 Return DirectCast(node, ElseBlockSyntax).Statements
                                             Else
                                                 Return DirectCast(node, SingleLineElseClauseSyntax).Statements
                                             End If
                                         End Function

                    Dim leftStatements = elseStatements(leftNode)
                    Dim rightStatements = elseStatements(rightNode)
                    distance = ComputeWeightedDistance(Nothing, leftStatements, Nothing, rightStatements)
                    Return True

                Case SyntaxKind.CatchBlock
                    Dim leftCatch = DirectCast(leftNode, CatchBlockSyntax)
                    Dim rightCatch = DirectCast(rightNode, CatchBlockSyntax)
                    distance = ComputeWeightedDistance(leftCatch.CatchStatement, leftCatch.Statements, rightCatch.CatchStatement, rightCatch.Statements)
                    Return True

                Case SyntaxKind.SingleLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression

                    Dim getParts = Function(node As SyntaxNode)
                                       Select Case node.Kind
                                           Case SyntaxKind.SingleLineSubLambdaExpression,
                                                SyntaxKind.SingleLineFunctionLambdaExpression
                                               Dim lambda = DirectCast(node, SingleLineLambdaExpressionSyntax)
                                               Return New With {lambda.SubOrFunctionHeader, .Body = lambda.Body.DescendantTokens()}

                                           Case Else
                                               Dim lambda = DirectCast(node, MultiLineLambdaExpressionSyntax)
                                               Return New With {lambda.SubOrFunctionHeader, .Body = GetDescendantTokensIgnoringSeparators(lambda.Statements)}
                                       End Select
                                   End Function

                    Dim leftParts = getParts(leftNode)
                    Dim rightParts = getParts(rightNode)
                    distance = ComputeWeightedDistanceOfLambdas(leftParts.SubOrFunctionHeader, rightParts.SubOrFunctionHeader, leftParts.Body, rightParts.Body)
                    Return True

                Case Else
                    distance = 0
                    Return False
            End Select
        End Function

        Private Shared Function ComputeWeightedDistanceOfLambdas(leftHeader As LambdaHeaderSyntax,
                                                                 rightHeader As LambdaHeaderSyntax,
                                                                 leftBody As IEnumerable(Of SyntaxToken),
                                                                 rightBody As IEnumerable(Of SyntaxToken)) As Double

            If leftHeader.Modifiers.Any(SyntaxKind.AsyncKeyword) <> rightHeader.Modifiers.Any(SyntaxKind.AsyncKeyword) Then
                Return 1.0
            End If

            Dim parameterDistance = ComputeDistance(leftHeader.ParameterList, rightHeader.ParameterList)

            If leftHeader.AsClause IsNot Nothing OrElse rightHeader.AsClause IsNot Nothing Then
                Dim asClauseDistance As Double = ComputeDistance(leftHeader.AsClause, rightHeader.AsClause)
                parameterDistance = parameterDistance * 0.8 + asClauseDistance * 0.2
            End If

            Dim bodyDistance = ComputeDistance(leftBody, rightBody)

            Return parameterDistance * 0.6 + bodyDistance * 0.4
        End Function

        Private Shared Function ComputeWeightedDistance(leftHeader As SyntaxNode,
                                                        leftStatements As SyntaxList(Of StatementSyntax),
                                                        rightHeader As SyntaxNode,
                                                        rightStatements As SyntaxList(Of StatementSyntax)) As Double
            Return ComputeWeightedDistance(Nothing, leftHeader, leftStatements, Nothing, rightHeader, rightStatements)
        End Function

        Private Shared Function ComputeWeightedDistance(leftControlVariable As SyntaxNode,
                                                        leftHeader As SyntaxNode,
                                                        leftStatements As SyntaxList(Of StatementSyntax),
                                                        rightControlVariable As SyntaxNode,
                                                        rightHeader As SyntaxNode,
                                                        rightStatements As SyntaxList(Of StatementSyntax)) As Double

            Debug.Assert((leftControlVariable Is Nothing) = (rightControlVariable Is Nothing))

            Dim headerDistance As Double = ComputeDistance(leftHeader, rightHeader)
            If leftControlVariable IsNot Nothing Then
                Dim controlVariableDistance = ComputeDistance(leftControlVariable, rightControlVariable)
                headerDistance = controlVariableDistance * 0.9 + headerDistance * 0.1
            End If

            Dim statementDistance As Double = ComputeDistance(leftStatements, rightStatements)

            Dim distance As Double = headerDistance * 0.6 + statementDistance * 0.4

            Dim localsDistance As Double
            If TryComputeLocalsDistance(leftStatements, rightStatements, localsDistance) Then
                distance = localsDistance * 0.5 + distance * 0.5
            End If

            Return distance
        End Function

        Private Shared Function TryComputeLocalsDistance(left As SyntaxList(Of StatementSyntax),
                                                         right As SyntaxList(Of StatementSyntax),
                                                         <Out> ByRef distance As Double) As Boolean

            Dim leftLocals As List(Of SyntaxToken) = Nothing
            Dim rightLocals As List(Of SyntaxToken) = Nothing

            GetLocalNames(left, leftLocals)
            GetLocalNames(right, rightLocals)

            If leftLocals Is Nothing OrElse rightLocals Is Nothing Then
                distance = 0
                Return False
            End If

            distance = ComputeDistance(leftLocals, rightLocals)
            Return True
        End Function

        Private Shared Sub GetLocalNames(statements As SyntaxList(Of StatementSyntax), <Out> ByRef result As List(Of SyntaxToken))
            For Each statement In statements
                If statement.IsKind(SyntaxKind.LocalDeclarationStatement) Then
                    For Each declarator In DirectCast(statement, LocalDeclarationStatementSyntax).Declarators
                        GetLocalNames(declarator, result)
                    Next
                End If
            Next
        End Sub

        Private Shared Sub GetLocalNames(localDecl As VariableDeclaratorSyntax, <Out> ByRef result As List(Of SyntaxToken))
            For Each local In localDecl.Names
                If result Is Nothing Then
                    result = New List(Of SyntaxToken)()
                End If

                result.Add(local.Identifier)
            Next
        End Sub
#End Region
    End Class
End Namespace
