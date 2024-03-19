' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        ''' <summary>
        ''' Rewrites the tree to account for destructive nature of stack local reads.
        ''' 
        ''' Typically, last read stays as-is and local is destroyed by the read.
        ''' Intermediate reads are rewritten as Dups -
        ''' 
        '''       NotLastUse(X_stackLocal) ===> NotLastUse(Dup)
        '''       LastUse(X_stackLocal) ===> LastUse(X_stackLocal)
        ''' 
        ''' </summary>
        Private NotInheritable Class Rewriter
            Inherits BoundTreeRewriterWithStackGuard

            Private _nodeCounter As Integer = 0
            Private ReadOnly _info As Dictionary(Of LocalSymbol, LocalDefUseInfo) = Nothing

            Private Sub New(info As Dictionary(Of LocalSymbol, LocalDefUseInfo))
                Me._info = info
            End Sub

            Public Shared Function Rewrite(src As BoundStatement, info As Dictionary(Of LocalSymbol, LocalDefUseInfo)) As BoundStatement
                Dim scheduler As New Rewriter(info)
                Return DirectCast(scheduler.Visit(src), BoundStatement)
            End Function

            Public Overrides Function Visit(node As BoundNode) As BoundNode
                Dim result As BoundNode = Nothing

                ' rewriting constants may undo constant folding and make thing worse.
                ' so we will not go into constant nodes. 
                ' CodeGen will not do that either.
                Dim asExpression = TryCast(node, BoundExpression)
                If asExpression IsNot Nothing AndAlso asExpression.ConstantValueOpt IsNot Nothing Then
                    result = node
                Else
                    result = MyBase.Visit(node)
                End If

                Me._nodeCounter += 1

                Return result
            End Function

            Public Overrides Function VisitBinaryOperator(node As BoundBinaryOperator) As BoundNode
                ' Do not blow the stack due to a deep recursion on the left. 

                Dim child As BoundExpression = node.Left

                If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                    Return VisitBinaryOperatorSimple(node)
                End If

                Dim stack = ArrayBuilder(Of BoundBinaryOperator).GetInstance()
                stack.Push(node)

                Dim binary As BoundBinaryOperator = DirectCast(child, BoundBinaryOperator)

                Do
                    stack.Push(binary)
                    child = binary.Left

                    If child.Kind <> BoundKind.BinaryOperator OrElse child.ConstantValueOpt IsNot Nothing Then
                        Exit Do
                    End If

                    binary = DirectCast(child, BoundBinaryOperator)
                Loop

                Dim left = DirectCast(Me.Visit(child), BoundExpression)

                Do
                    binary = stack.Pop()

                    Dim right = DirectCast(Me.Visit(binary.Right), BoundExpression)
                    Dim type As TypeSymbol = Me.VisitType(binary.Type)
                    left = binary.Update(binary.OperatorKind, left, right, binary.Checked, binary.ConstantValueOpt, type)

                    If stack.Count = 0 Then
                        Exit Do
                    End If

                    Me._nodeCounter += 1
                Loop

                Debug.Assert(binary Is node)
                stack.Free()

                Return left
            End Function

            Private Function VisitBinaryOperatorSimple(node As BoundBinaryOperator) As BoundNode
                Return MyBase.VisitBinaryOperator(node)
            End Function

            Private Shared Function IsLastAccess(locInfo As LocalDefUseInfo, counter As Integer) As Boolean
                Return locInfo.localDefs.Any(Function(d) counter = d.Start AndAlso counter = d.End)
            End Function

            Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
                Dim locInfo As LocalDefUseInfo = Nothing
                If Not _info.TryGetValue(node.LocalSymbol, locInfo) Then
                    Return MyBase.VisitLocal(node)
                End If

                ' not the last access, emit Dup.
                If Not IsLastAccess(locInfo, _nodeCounter) Then
                    Return New BoundDup(node.Syntax, node.LocalSymbol.IsByRef, node.Type)
                End If

                ' last access - leave the node as is. Emit will do nothing expecting the node on the stack
                Return MyBase.VisitLocal(node)
            End Function

            ' TODO: Why??
            'Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode
            '    Dim arguments As ImmutableArray(Of BoundExpression) = Me.VisitList(node.Arguments)
            '    Debug.Assert(node.InitializerOpt Is Nothing)
            '    Dim type As TypeSymbol = Me.VisitType(node.Type)
            '    Return node.Update(node.ConstructorOpt, arguments, node.InitializerOpt, type)
            'End Function

            Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
                Dim locInfo As LocalDefUseInfo = Nothing
                Dim left = DirectCast(node.ByRefLocal, BoundLocal)

                ' store to something that is not special. (operands still could be rewritten) 
                If Not _info.TryGetValue(left.LocalSymbol, locInfo) Then
                    Return MyBase.VisitReferenceAssignment(node)
                End If

                ' we do not need to visit lhs, just update the counter to be in sync
                Me._nodeCounter += 1

                ' Visit the expression being assigned 
                Dim right = DirectCast(Me.Visit(node.LValue), BoundExpression)

                ' this should not be the last store, why would be created such a variable after all???
                Debug.Assert(locInfo.localDefs.Any(Function(d) _nodeCounter = d.Start AndAlso _nodeCounter <= d.End))

                If IsLastAccess(locInfo, _nodeCounter) Then
                    ' assigned local is not used later => just emit the Right 
                    Return right
                End If

                ' assigned local used later - keep assignment. 
                ' codegen will keep value on stack when sees assignment "stackLocal = expr"
                Return node.Update(left, right, node.IsLValue, node.Type)
            End Function

            ' default visitor for AssignmentOperator that ignores LeftOnTheRightOpt
            Private Function VisitAssignmentOperatorDefault(node As BoundAssignmentOperator) As BoundNode
                Dim left As BoundExpression = DirectCast(Me.Visit(node.Left), BoundExpression)
                Dim right As BoundExpression = DirectCast(Me.Visit(node.Right), BoundExpression)

                Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

                Return node.Update(left, Nothing, right, node.SuppressObjectClone, node.Type)
            End Function

            Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode
                Dim locInfo As LocalDefUseInfo = Nothing
                Dim left = TryCast(node.Left, BoundLocal)

                ' store to something that is not special. (operands still could be rewritten) 
                If left Is Nothing OrElse Not _info.TryGetValue(left.LocalSymbol, locInfo) Then
                    Return VisitAssignmentOperatorDefault(node)
                End If

                ' indirect local store is not special. (operands still could be rewritten) 
                ' NOTE: if Lhs is a stack local, it will be handled as a read and possibly duped.
                Dim isIndirectLocalStore = left.LocalSymbol.IsByRef
                If isIndirectLocalStore Then
                    Return VisitAssignmentOperatorDefault(node)
                End If

                '==  here we have a regular write to a stack local
                '
                ' we do not need to visit lhs, because we do not read the local,
                ' just update the counter to be in sync.
                ' 
                ' if this is the last store, we just push the rhs
                ' otherwise record a store.

                ' fake visiting of left
                Me._nodeCounter += 1

                ' Left on the right should be Nothing by this time
                Debug.Assert(node.LeftOnTheRightOpt Is Nothing)

                ' do not visit left-on-the-right
                ' Me.nodeCounter += 1

                ' visit right
                Dim right = DirectCast(Me.Visit(node.Right), BoundExpression)

                ' do actual assignment
                Debug.Assert(locInfo.localDefs.Any(Function(d) _nodeCounter = d.Start AndAlso _nodeCounter <= d.End))
                Dim isLast As Boolean = IsLastAccess(locInfo, _nodeCounter)

                If isLast Then
                    ' assigned local is not used later => just emit the Right 
                    Return right

                Else
                    ' assigned local used later - keep assignment. 
                    ' codegen will keep value on stack when sees assignment "stackLocal = expr"
                    Return node.Update(left, node.LeftOnTheRightOpt, right, node.SuppressObjectClone, node.Type)
                End If
            End Function

            Public Overrides Function VisitLoweredConditionalAccess(node As BoundLoweredConditionalAccess) As BoundNode
                Dim receiverOrCondition As BoundExpression = DirectCast(Me.Visit(node.ReceiverOrCondition), BoundExpression)
                Dim whenNotNull As BoundExpression = DirectCast(Me.Visit(node.WhenNotNull), BoundExpression)
                Dim whenNullOpt As BoundExpression = node.WhenNullOpt

                If whenNullOpt IsNot Nothing Then
                    whenNullOpt = DirectCast(Me.Visit(whenNullOpt), BoundExpression)
                End If

                Return node.Update(receiverOrCondition, node.CaptureReceiver, node.PlaceholderId, whenNotNull, whenNullOpt, node.Type)
            End Function

        End Class

    End Class
End Namespace

