' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that records jumps out of the region.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class ExitPointsWalker
        Inherits AbstractRegionControlFlowPass

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As IEnumerable(Of StatementSyntax)
            Dim walker = New ExitPointsWalker(info, region)
            Try
                Return If(walker.Analyze(), walker._branchesOutOf.ToImmutable(), SpecializedCollections.EmptyEnumerable(Of StatementSyntax)())
            Finally
                walker.Free()
            End Try
        End Function

        Private _branchesOutOf As ArrayBuilder(Of StatementSyntax) = ArrayBuilder(Of StatementSyntax).GetInstance()

        Private Overloads Function Analyze() As Boolean
            '  only one pass is needed.
            Return Scan()
        End Function

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Protected Overrides Sub Free()
            Me._branchesOutOf.Free()
            Me._branchesOutOf = Nothing
            Me._labelsInside.Free()
            Me._labelsInside = Nothing
            MyBase.Free()
        End Sub

        Private _labelsInside As ArrayBuilder(Of LabelSymbol) = ArrayBuilder(Of LabelSymbol).GetInstance()

        Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
            ' The syntax can be a label or an end block statement when the label represents an exit
            Dim syntax = node.Syntax
            If IsInside Then
                _labelsInside.Add(node.Label)
            End If
            Return MyBase.VisitLabelStatement(node)
        End Function

        Public Overrides Function VisitDoLoopStatement(node As BoundDoLoopStatement) As BoundNode
            If IsInside Then
                _labelsInside.Add(node.ExitLabel)
                _labelsInside.Add(node.ContinueLabel)
            End If
            Return MyBase.VisitDoLoopStatement(node)
        End Function

        Public Overrides Function VisitForToStatement(node As BoundForToStatement) As BoundNode
            If IsInside Then
                _labelsInside.Add(node.ExitLabel)
                _labelsInside.Add(node.ContinueLabel)
            End If
            Return MyBase.VisitForToStatement(node)
        End Function

        Public Overrides Function VisitForEachStatement(node As BoundForEachStatement) As BoundNode
            If IsInside Then
                _labelsInside.Add(node.ExitLabel)
                _labelsInside.Add(node.ContinueLabel)
            End If
            Return MyBase.VisitForEachStatement(node)
        End Function

        Public Overrides Function VisitWhileStatement(node As BoundWhileStatement) As BoundNode
            If IsInside Then
                _labelsInside.Add(node.ExitLabel)
                _labelsInside.Add(node.ContinueLabel)
            End If
            Return MyBase.VisitWhileStatement(node)
        End Function

        Public Overrides Function VisitSelectStatement(node As BoundSelectStatement) As BoundNode
            If IsInside Then
                _labelsInside.Add(node.ExitLabel)
            End If
            Return MyBase.VisitSelectStatement(node)
        End Function

        Protected Overrides Sub LeaveRegion()
            '  Process the pending returns only from this region. 
            For Each pending In Me.PendingBranches
                If IsInsideRegion(pending.Branch.Syntax.Span) Then
                    Select Case pending.Branch.Kind
                        Case BoundKind.GotoStatement
                            If _labelsInside.Contains((TryCast((pending.Branch), BoundGotoStatement)).Label) Then
                                Continue For
                            End If
                        Case BoundKind.ExitStatement
                            If _labelsInside.Contains((TryCast((pending.Branch), BoundExitStatement)).Label) Then
                                Continue For
                            End If
                        Case BoundKind.ContinueStatement
                            If _labelsInside.Contains((TryCast((pending.Branch), BoundContinueStatement)).Label) Then
                                Continue For
                            End If
                        Case BoundKind.YieldStatement
                        Case BoundKind.ReturnStatement
                            ' These are always included (we don't dive into lambda expressions)
                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(pending.Branch.Kind) ' there are no other branch statements
                    End Select
                    _branchesOutOf.Add(DirectCast(pending.Branch.Syntax, StatementSyntax))
                End If
            Next

            MyBase.LeaveRegion()
        End Sub

    End Class

End Namespace
