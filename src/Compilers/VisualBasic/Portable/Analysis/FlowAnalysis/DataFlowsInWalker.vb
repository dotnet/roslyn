' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that computes the set of variables whose values flow into (are used in)
    ''' the region.
    ''' A variable assigned outside is used inside if an analysis
    ''' that leaves the variable unassigned on entry to the region would cause the
    ''' generation of "unassigned" errors within the region.
    ''' </summary>
    Friend Class DataFlowsInWalker
        Inherits AbstractRegionDataFlowPass

        ' TODO: normalize the result by removing variables that are unassigned in an unmodified flow analysis.
        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo, unassignedVariables As HashSet(Of Symbol))
            MyBase.New(info, region, unassignedVariables, trackStructsWithIntrinsicTypedFields:=True)
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                                                 unassignedVariables As HashSet(Of Symbol),
                                                 ByRef succeeded As Boolean?,
                                                 ByRef invalidRegionDetected As Boolean) As HashSet(Of Symbol)

            ' remove static locals from unassigned, otherwise they will never reach ReportUnassigned(...)
            Dim unassignedWithoutStatic As New HashSet(Of Symbol)
            For Each var In unassignedVariables
                If var.Kind <> SymbolKind.Local OrElse Not DirectCast(var, LocalSymbol).IsStatic Then
                    unassignedWithoutStatic.Add(var)
                End If
            Next

            Dim walker = New DataFlowsInWalker(info, region, unassignedWithoutStatic)
            Try
                succeeded = walker.Analyze() AndAlso Not walker.InvalidRegionDetected
                invalidRegionDetected = walker.InvalidRegionDetected
                Return If(succeeded, walker._dataFlowsIn, New HashSet(Of Symbol)())
            Finally
                walker.Free()
            End Try
        End Function

        Private ReadOnly _dataFlowsIn As HashSet(Of Symbol) = New HashSet(Of Symbol)()

        Private Function ResetState(state As LocalState) As LocalState
            Dim unreachable As Boolean = Not state.Reachable
            state = ReachableState()
            If unreachable Then
                state.Assign(0)
            End If
            Return state
        End Function

        Protected Overrides Sub EnterRegion()
            Me.SetState(ResetState(Me.State))
            Me._dataFlowsIn.Clear()
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub NoteBranch(pending As PendingBranch, stmt As BoundStatement, labelStmt As BoundLabelStatement)
            If stmt.Syntax IsNot Nothing AndAlso labelStmt.Syntax IsNot Nothing AndAlso Not IsInsideRegion(stmt.Syntax.Span) AndAlso IsInsideRegion(labelStmt.Syntax.Span) Then
                pending.State = ResetState(pending.State)
            End If
            MyBase.NoteBranch(pending, stmt, labelStmt)
        End Sub

        Public Overrides Function VisitRangeVariable(node As BoundRangeVariable) As BoundNode
            ' Sometimes query expressions refer to query range variable just to 
            ' copy its value to a new compound variable. There is no reference
            ' to the range variable in code and, from user point of view, there is
            ' no access to it.
            ' If and only if range variable is declared outside of the region and read inside, it flows in.
            If Not node.WasCompilerGenerated AndAlso
               IsInside AndAlso
               Not IsInsideRegion(node.RangeVariable.Syntax.Span) Then

                _dataFlowsIn.Add(node.RangeVariable)
            End If

            Return Nothing
        End Function

        Protected Overrides Sub VisitAmbiguousLocalSymbol(ambiguous As DataFlowPass.AmbiguousLocalsPseudoSymbol)
            MyBase.VisitAmbiguousLocalSymbol(ambiguous)

            ' Locals from ambiguous implicit receiver can only be unassigned in *REGION* flow analysis 
            ' if a new region starts after they are declared and before the implicit receiver is referenced; 
            ' region data flow analysis for such regions is prohibited and should return Succeeded = False.

            ' Check if the first local in the collection was 'unassigned' by entering a region, 
            ' in which case set a flag that the region is not valid
            If IsInside Then
                Dim firstLocal As LocalSymbol = ambiguous.Locals(0)
                If Not Me.State.IsAssigned(VariableSlot(firstLocal)) Then
                    Me.SetInvalidRegion()
                End If
            End If
        End Sub

        Protected Overrides Sub ReportUnassigned(local As Symbol,
                                                 node As SyntaxNode,
                                                 rwContext As ReadWriteContext,
                                                 Optional slot As Integer = SlotKind.NotTracked,
                                                 Optional boundFieldAccess As BoundFieldAccess = Nothing)

            Debug.Assert(local.Kind <> SymbolKind.Field OrElse boundFieldAccess IsNot Nothing)

            If IsInsideRegion(node.Span) Then
                Debug.Assert(local.Kind <> SymbolKind.RangeVariable)

                If local.Kind = SymbolKind.Field Then
                    Dim sym As Symbol = GetNodeSymbol(boundFieldAccess)

                    ' Unreachable for AmbiguousLocalsPseudoSymbol: ambiguous implicit 
                    ' receiver should not ever be considered unassigned
                    Debug.Assert(Not TypeOf sym Is AmbiguousLocalsPseudoSymbol)

                    If sym IsNot Nothing Then
                        _dataFlowsIn.Add(sym)
                    End If

                Else
                    _dataFlowsIn.Add(local)
                End If
            End If

            MyBase.ReportUnassigned(local, node, rwContext, slot, boundFieldAccess)
        End Sub

        Friend Overrides Sub AssignLocalOnDeclaration(local As LocalSymbol, node As BoundLocalDeclaration)
            ' NOTE: static locals should not be considered assigned even in presence of initializer
            If Not local.IsStatic Then
                MyBase.AssignLocalOnDeclaration(local, node)
            End If
        End Sub

    End Class

End Namespace
