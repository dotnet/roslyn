' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue

    Friend NotInheritable Class StatementSyntaxComparer
        Inherits SyntaxComparer

        Friend Shared ReadOnly [Default] As StatementSyntaxComparer = New StatementSyntaxComparer()

        Private ReadOnly _oldRoot As SyntaxNode
        Private ReadOnly _newRoot As SyntaxNode
        Private ReadOnly _oldRootChildren As IEnumerable(Of SyntaxNode)
        Private ReadOnly _newRootChildren As IEnumerable(Of SyntaxNode)
        Private ReadOnly _matchingLambdas As Boolean

        Private Sub New()
        End Sub

        Friend Sub New(oldRoot As SyntaxNode,
                       oldRootChildren As IEnumerable(Of SyntaxNode),
                       newRoot As SyntaxNode,
                       newRootChildren As IEnumerable(Of SyntaxNode),
                       matchingLambdas As Boolean)
            _oldRoot = oldRoot
            _newRoot = newRoot
            _oldRootChildren = oldRootChildren
            _newRootChildren = newRootChildren
            _matchingLambdas = matchingLambdas
        End Sub

#Region "Tree Traversal"
        Protected Overrides Function TryGetParent(node As SyntaxNode, ByRef parent As SyntaxNode) As Boolean
            parent = node.Parent
            While parent IsNot Nothing AndAlso Not HasLabel(parent)
                parent = parent.Parent
            End While

            Return parent IsNot Nothing
        End Function

        Protected Overrides Function GetChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Debug.Assert(HasLabel(node))

            If node Is _oldRoot OrElse node Is _newRoot Then
                Return EnumerateRootChildren(node)
            End If

            Return If(IsLeaf(node), Nothing, EnumerateNonRootChildren(node))
        End Function

        Private Iterator Function EnumerateRootChildren(root As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Debug.Assert(_oldRoot IsNot Nothing AndAlso _newRoot IsNot Nothing)

            Dim rootChildren = If(root Is _oldRoot, _oldRootChildren, _newRootChildren)

            For Each rootChild In rootChildren
                If HasLabel(rootChild) Then
                    Yield rootChild
                Else
                    For Each descendant In rootChild.DescendantNodes(AddressOf DescendIntoChildren)
                        If HasLabel(descendant) Then
                            Yield descendant
                        End If
                    Next
                End If
            Next
        End Function

        Private Iterator Function EnumerateNonRootChildren(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Debug.Assert(HasLabel(node))

            For Each child In node.ChildNodes()

                If LambdaUtilities.IsLambdaBodyStatementOrExpression(child) Then
                    Continue For
                End If

                If HasLabel(child) Then
                    Yield child
                Else
                    For Each descendant In child.DescendantNodes(AddressOf DescendIntoChildren)
                        If HasLabel(descendant) Then
                            Yield descendant
                        End If
                    Next
                End If
            Next
        End Function

        Protected Overrides Iterator Function GetDescendants(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            If node Is _oldRoot OrElse node Is _newRoot Then
                Debug.Assert(_oldRoot IsNot Nothing AndAlso _newRoot IsNot Nothing)

                Dim rootChildren = If(node Is _oldRoot, _oldRootChildren, _newRootChildren)

                For Each rootChild In rootChildren
                    If HasLabel(rootChild) Then
                        Yield rootChild
                    End If

                    ' TODO: avoid allocation of closure
                    For Each descendant In rootChild.DescendantNodes(descendIntoChildren:=Function(c) Not IsLeaf(c) AndAlso (c Is rootChild OrElse Not LambdaUtilities.IsLambdaBodyStatementOrExpression(c)))
                        If Not LambdaUtilities.IsLambdaBodyStatementOrExpression(descendant) AndAlso HasLabel(descendant) Then
                            Yield descendant
                        End If
                    Next
                Next
            Else
                ' TODO: avoid allocation of closure
                Dim descendants = node.DescendantNodes(descendIntoChildren:=Function(c) Not IsLeaf(c) AndAlso (c Is node OrElse Not LambdaUtilities.IsLambdaBodyStatementOrExpression(c)))
                For Each descendant In descendants
                    If Not LambdaUtilities.IsLambdaBodyStatementOrExpression(descendant) AndAlso HasLabel(descendant) Then
                        Yield descendant
                    End If
                Next
            End If
        End Function

        Private Function DescendIntoChildren(node As SyntaxNode) As Boolean
            Return Not LambdaUtilities.IsLambdaBodyStatementOrExpression(node) AndAlso Not HasLabel(node)
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

            LambdaRoot

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

            LocalDeclarationStatement           ' tied to parent 
            LocalVariableDeclarator             ' tied to parent 

            ' TODO: AwaitExpression

            Lambda
            LambdaBodyBegin                     ' tied to parent

            QueryExpression
            AggregateClause                     ' tied to parent 
            JoinClause                          ' tied to parent
            FromClause                          ' tied to parent
            WhereClauseLambda                   ' tied to parent 
            LetClause                           ' tied to parent
            SelectClauseLambda                  ' tied to parent
            PartitionWhileLambda                ' tied to parent
            PartitionClause                     ' tied to parent
            GroupByClause                       ' tied to parent
            OrderByClause                       ' tied to parent

            CollectionRangeVariable             ' tied to parent (FromClause, JoinClause, AggregateClause)
            ExpressionRangeVariable             ' tied to parent (LetClause, SelectClause, GroupByClause keys)
            ExpressionRangeVariableItems        ' tied to parent (GroupByClause items)
            FunctionAggregationLambda           ' tied to parent (JoinClause, GroupByClause, AggregateClause)

            OrderingLambda                      ' tied to parent (OrderByClause)
            JoinConditionLambda                 ' tied to parent (JoinClause)

            LocalVariableName                   ' tied to parent 

            BodyEnd                             ' tied to parent

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
                     Label.AggregateClause,
                     Label.JoinClause,
                     Label.FromClause,
                     Label.WhereClauseLambda,
                     Label.LetClause,
                     Label.SelectClauseLambda,
                     Label.PartitionWhileLambda,
                     Label.PartitionClause,
                     Label.GroupByClause,
                     Label.OrderByClause,
                     Label.CollectionRangeVariable,
                     Label.ExpressionRangeVariable,
                     Label.ExpressionRangeVariableItems,
                     Label.FunctionAggregationLambda,
                     Label.OrderingLambda,
                     Label.JoinConditionLambda,
                     Label.LocalDeclarationStatement,
                     Label.LocalVariableDeclarator,
                     Label.LocalVariableName
                    Return 1

                Case Else
                    Return 0
            End Select
        End Function

        Protected Shared Function IsLeaf(node As SyntaxNode) As Boolean
            Classify(node.Kind, node, IsLeaf)
        End Function

        Friend Shared Function Classify(kind As SyntaxKind, nodeOpt As SyntaxNode, <Out> ByRef isLeaf As Boolean) As Label
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
                    ' Header is a leaf so that we don't descent into lambda parameters.
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
                    Return Label.Lambda

                Case SyntaxKind.FunctionLambdaHeader,
                     SyntaxKind.SubLambdaHeader
                    ' TODO: Parameters are not mapped?
                    isLeaf = True
                    Return Label.Ignored

                Case SyntaxKind.QueryExpression
                    Return Label.QueryExpression

                Case SyntaxKind.WhereClause
                    Return Label.WhereClauseLambda

                Case SyntaxKind.LetClause
                    Return Label.LetClause

                Case SyntaxKind.SkipClause,
                     SyntaxKind.TakeClause
                    Return Label.PartitionClause

                Case SyntaxKind.TakeWhileClause,
                     SyntaxKind.SkipWhileClause
                    Return Label.PartitionWhileLambda

                Case SyntaxKind.AscendingOrdering,
                     SyntaxKind.DescendingOrdering
                    Return Label.OrderingLambda

                Case SyntaxKind.FunctionAggregation
                    Return Label.FunctionAggregationLambda

                Case SyntaxKind.SelectClause
                    Return Label.SelectClauseLambda

                Case SyntaxKind.GroupByClause
                    Return Label.GroupByClause

                Case SyntaxKind.OrderByClause
                    Return Label.OrderByClause

                Case SyntaxKind.SimpleJoinClause,
                     SyntaxKind.GroupJoinClause
                    Return Label.JoinClause

                Case SyntaxKind.AggregateClause
                    Return Label.AggregateClause

                Case SyntaxKind.FromClause
                    Return Label.FromClause

                Case SyntaxKind.ExpressionRangeVariable
                    ' Select, Let, GroupBy
                    '
                    ' All ERVs need to be included in the map,
                    ' so that we are able to map range variable declarations.
                    ' 
                    ' Since we don't distinguish between ERVs that represent a lambda and non-lambda ERVs,
                    ' we need to be handle cases when one maps to the other (and vice versa).
                    ' This is handled in GetCorresondingLambdaBody.
                    '
                    ' On the other hand we don't want map across lambda body boundaries.
                    ' Hence we use a label for ERVs in Items distinct from one for Keys of a GroupBy clause.
                    '
                    ' Node that the nodeOpt is Nothing only when comparing nodes for value equality.
                    ' In that case it doesn't matter what label the node has as long as it has some.
                    If nodeOpt IsNot Nothing AndAlso
                       nodeOpt.Parent.IsKind(SyntaxKind.GroupByClause) AndAlso
                       nodeOpt.SpanStart < DirectCast(nodeOpt.Parent, GroupByClauseSyntax).ByKeyword.SpanStart Then
                        Return Label.ExpressionRangeVariableItems
                    Else
                        Return Label.ExpressionRangeVariable
                    End If

                Case SyntaxKind.CollectionRangeVariable
                    ' From, Aggregate
                    '
                    ' All CRVs need to be included in the map,
                    ' so that we are able to map range variable declarations.

                    Return Label.CollectionRangeVariable

                Case SyntaxKind.JoinCondition
                    ' TODO:
                    Return Label.JoinConditionLambda

                ' TODO:
                'Case SyntaxKind.AwaitExpression
                '    Return Label.AwaitExpression

                Case SyntaxKind.GenericName
                    ' optimization - no need to dig into type instantiations
                    isLeaf = True
                    Return Label.Ignored

                Case Else
                    Return Label.Ignored
            End Select
        End Function

        Protected Overrides Function GetLabel(node As SyntaxNode) As Integer
            If _matchingLambdas AndAlso (node Is _newRoot OrElse node Is _oldRoot) Then
                Return Label.LambdaRoot
            End If

            Return GetLabelImpl(node)
        End Function

        Friend Shared Function GetLabelImpl(node As SyntaxNode) As Label
            Dim isLeaf As Boolean
            Return Classify(node.Kind, node, isLeaf)
        End Function

        Friend Shared Function HasLabel(node As SyntaxNode) As Boolean
            Return GetLabelImpl(node) <> Label.Ignored
        End Function

        Protected Overrides ReadOnly Property LabelCount As Integer
            Get
                Return Label.Count
            End Get
        End Property

        Protected Overrides Function TiedToAncestor(label As Integer) As Integer
            Return TiedToAncestor(CType(label, Label))
        End Function

#End Region

#Region "Comparisons"

        Friend Shared Function IgnoreLabeledChild(kind As SyntaxKind) As Boolean
            Dim isLeaf = False
            Return Classify(kind, Nothing, isLeaf) <> Label.Ignored
        End Function

        Public Overrides Function ValuesEqual(left As SyntaxNode, right As SyntaxNode) As Boolean
            Dim ignoreChildFunction As Func(Of SyntaxKind, Boolean)

            ' When comparing the value of a node with its partner we are deciding whether to add an Update edit for the pair.
            ' If the actual change is under a descendant labeled node we don't want to attribute it to the node being compared,
            ' so we skip all labeled children when recursively checking for equivalence.
            If IsLeaf(left) Then
                ignoreChildFunction = Nothing
            Else
                ignoreChildFunction = AddressOf IgnoreLabeledChild
            End If

            Return SyntaxFactory.AreEquivalent(left, right, ignoreChildFunction)
        End Function

        Protected Overrides Function TryComputeWeightedDistance(leftNode As SyntaxNode, rightNode As SyntaxNode, ByRef distance As Double) As Boolean
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
