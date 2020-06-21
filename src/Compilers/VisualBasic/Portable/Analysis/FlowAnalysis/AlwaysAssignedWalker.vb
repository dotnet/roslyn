' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A region analysis walker that computes the set of variables that are always assigned a value in the region.
    ''' A variable is "always assigned" in a region if an analysis of the
    ''' region that starts with the variable unassigned ends with the variable
    ''' assigned.
    ''' </summary>
    Friend Class AlwaysAssignedWalker
        Inherits AbstractRegionDataFlowPass

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As IEnumerable(Of Symbol)
            Dim walker = New AlwaysAssignedWalker(info, region)
            Try
                Dim result As Boolean = walker.Analyze()
                Return If(result, walker.AlwaysAssigned, SpecializedCollections.EmptyEnumerable(Of Symbol)())
            Finally
                walker.Free()
            End Try
        End Function

        Private _endOfRegionState As LocalState
        Private ReadOnly _labelsInside As New HashSet(Of LabelSymbol)()

        Private ReadOnly Property AlwaysAssigned As IEnumerable(Of Symbol)
            Get
                Dim result As New List(Of Symbol)
                If (_endOfRegionState.Reachable) Then
                    For Each i In _endOfRegionState.Assigned.TrueBits
                        If (i >= variableBySlot.Length) Then
                            Continue For
                        End If

                        Dim v = variableBySlot(i)
                        If v.Exists AndAlso v.Symbol.Kind <> SymbolKind.Field Then
                            result.Add(v.Symbol)
                        End If
                    Next
                End If

                Return result
            End Get
        End Property

        Protected Overrides Sub EnterRegion()
            MyBase.SetState(ReachableState())
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub LeaveRegion()
            If Me.IsConditionalState Then
                ' If the region is in a condition, then the state will be split and 
                ' State.Assigned(will) be null. Merge to get sensible results.
                _endOfRegionState = Me.StateWhenTrue.Clone()
                IntersectWith(_endOfRegionState, Me.StateWhenFalse)
            Else
                _endOfRegionState = MyBase.State.Clone()
            End If

            Debug.Assert(Not _endOfRegionState.Assigned.IsNull)

            For Each branch In PendingBranches
                If IsInsideRegion(branch.Branch.Syntax.Span) AndAlso Not _labelsInside.Contains(branch.Label) Then
                    IntersectWith(_endOfRegionState, branch.State)
                End If
            Next

            MyBase.LeaveRegion()
        End Sub

        Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
            If node.Syntax IsNot Nothing AndAlso IsInsideRegion(node.Syntax.Span) Then
                _labelsInside.Add(node.Label)
            End If

            Return MyBase.VisitLabelStatement(node)
        End Function

        Protected Overrides Sub ResolveBranch(pending As AbstractFlowPass(Of DataFlowPass.LocalState).PendingBranch, label As LabelSymbol, target As BoundLabelStatement, ByRef labelStateChanged As Boolean)
            If IsInside AndAlso pending.Branch IsNot Nothing AndAlso Not IsInsideRegion(pending.Branch.Syntax.Span) Then
                pending.State = If(pending.State.Reachable, ReachableState(), UnreachableState())
            End If

            MyBase.ResolveBranch(pending, label, target, labelStateChanged)
        End Sub

        Protected Overrides Sub WriteArgument(arg As BoundExpression, isOut As Boolean)
            ' ref parameter must be '<Out()>' to "always" assign
            If isOut Then
                MyBase.WriteArgument(arg, isOut)
            End If
        End Sub
    End Class

End Namespace
