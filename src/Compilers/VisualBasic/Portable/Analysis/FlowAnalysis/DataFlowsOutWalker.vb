' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that computes the set of variables for
    ''' which their assigned values flow out of the region.
    ''' A variable assigned inside is used outside if an analysis that
    ''' treats assignments in the region as un-assigning the variable would
    ''' cause "unassigned" errors outside the region.
    ''' </summary>
    Friend Class DataFlowsOutWalker
        Inherits AbstractRegionDataFlowPass

        Private ReadOnly _dataFlowsIn As ImmutableArray(Of ISymbol)
        Private ReadOnly _originalUnassigned As HashSet(Of Symbol)
        Private ReadOnly _dataFlowsOut As New HashSet(Of Symbol)()
#If DEBUG Then
        ' we'd like to ensure that only variables get returned in DataFlowsOut that were assigned to inside the region.
        Private ReadOnly _assignedInside As HashSet(Of Symbol) = New HashSet(Of Symbol)()
#End If

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                unassignedVariables As HashSet(Of Symbol), originalUnassigned As HashSet(Of Symbol), dataFlowsIn As ImmutableArray(Of ISymbol))

            MyBase.New(info, region, unassignedVariables, trackUnassignments:=True, trackStructsWithIntrinsicTypedFields:=True)

            Me._dataFlowsIn = dataFlowsIn
            Me._originalUnassigned = originalUnassigned
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                                                 unassignedVariables As HashSet(Of Symbol), dataFlowsIn As ImmutableArray(Of ISymbol)) As HashSet(Of Symbol)

            ' remove static locals from unassigned, otherwise they will never reach ReportUnassigned(...)
            Dim unassignedWithoutStatic As New HashSet(Of Symbol)
            For Each var In unassignedVariables
                If var.Kind <> SymbolKind.Local OrElse Not DirectCast(var, LocalSymbol).IsStatic Then
                    unassignedWithoutStatic.Add(var)
                End If
            Next

            Dim walker = New DataFlowsOutWalker(info, region, unassignedWithoutStatic, unassignedVariables, dataFlowsIn)
            Try
                Dim success = walker.Analyze
                Dim result = walker._dataFlowsOut
#If DEBUG Then
                ' Assert that DataFlowsOut only contains variables that were assigned to inside the region
                Debug.Assert(Not success OrElse Not result.Any(Function(variable) Not walker._assignedInside.Contains(variable)))
#End If

                Return If(success, result, New HashSet(Of Symbol)())
            Finally
                walker.Free()
            End Try
        End Function

        Protected Overrides Sub EnterRegion()
            ' to handle loops properly, we must assume that every variable (except static locals) 
            ' that flows in is assigned at the beginning of the loop.  If it isn't, then it must 
            ' be in a loop and flow out of the region in that loop (and into the region inside the loop).
            For Each variable As Symbol In _dataFlowsIn
                Dim slot As Integer = Me.MakeSlot(variable)
                If Not Me.State.IsAssigned(slot) AndAlso variable.Kind <> SymbolKind.RangeVariable AndAlso
                        (variable.Kind <> SymbolKind.Local OrElse Not DirectCast(variable, LocalSymbol).IsStatic) Then
                    _dataFlowsOut.Add(variable)
                End If
            Next

            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub Assign(node As BoundNode, value As BoundExpression, Optional assigned As Boolean = True)
            If IsInside Then

                If assigned Then
                    Dim symbol = GetNodeSymbol(node)
                    If symbol IsNot Nothing Then
#If DEBUG Then
                        _assignedInside.Add(symbol)
#End If
                    End If
                End If

                ' assignments inside the region are recorded as un-assignments
                assigned = False

                ' any reachable assignment to a ref or out parameter can 
                ' be visible to the caller in the face of exceptions.
                If Me.State.Reachable Then
                    Select Case node.Kind

                        Case BoundKind.Parameter, BoundKind.MeReference
                            Dim expression = DirectCast(node, BoundExpression)
                            Dim slots As SlotCollection = MakeSlotsForExpression(expression)
                            Debug.Assert(slots.Count <= 1)

                            If slots.Count > 0 Then
                                Dim slot As Integer = slots(0)
                                If slot >= SlotKind.FirstAvailable Then

                                    Dim exprIdentifier = DirectCast(variableBySlot(slot).Symbol, ParameterSymbol)
                                    If exprIdentifier IsNot Nothing AndAlso exprIdentifier.IsByRef Then
                                        _dataFlowsOut.Add(exprIdentifier)
                                    End If
                                End If
                            End If

                        Case BoundKind.Local
                            Dim local = DirectCast(node, BoundLocal)
                            Dim locSymbol As LocalSymbol = local.LocalSymbol
                            If locSymbol.IsStatic AndAlso WasUsedBeforeAssignment(locSymbol) Then
                                Dim slots As SlotCollection = MakeSlotsForExpression(local)
                                Debug.Assert(slots.Count <= 1)

                                If slots.Count > 0 Then
                                    Dim slot As Integer = slots(0)
                                    If slot >= SlotKind.FirstAvailable Then
                                        Debug.Assert(locSymbol Is variableBySlot(slot).Symbol)
                                        _dataFlowsOut.Add(variableBySlot(slot).Symbol)
                                    End If
                                End If

                            End If

                    End Select
                End If
            End If

            MyBase.Assign(node, value, assigned)
        End Sub

        Protected Overrides Sub ReportUnassigned(local As Symbol,
                                                 node As VisualBasicSyntaxNode,
                                                 rwContext As ReadWriteContext,
                                                 Optional slot As Integer = SlotKind.NotTracked,
                                                 Optional boundFieldAccess As BoundFieldAccess = Nothing)

            Debug.Assert(local.Kind <> SymbolKind.Field OrElse boundFieldAccess IsNot Nothing)

            If Not _dataFlowsOut.Contains(local) AndAlso local.Kind <> SymbolKind.RangeVariable AndAlso Not IsInside Then
                If local.Kind = SymbolKind.Field Then
                    Dim sym As Symbol = GetNodeSymbol(boundFieldAccess)

                    ' Unreachable for AmbiguousLocalsPseudoSymbol: ambiguous implicit 
                    ' receiver should not ever be considered unassigned
                    Debug.Assert(Not TypeOf sym Is AmbiguousLocalsPseudoSymbol)

                    If sym IsNot Nothing Then
                        _dataFlowsOut.Add(sym)
                    End If

                Else
                    _dataFlowsOut.Add(local)
                End If
            End If

            MyBase.ReportUnassigned(local, node, rwContext, slot, boundFieldAccess)
        End Sub

        Protected Overrides Sub ReportUnassignedByRefParameter(parameter As ParameterSymbol)
            _dataFlowsOut.Add(parameter)

            MyBase.ReportUnassignedByRefParameter(parameter)
        End Sub

        Protected Overrides Sub NoteWrite(variable As Symbol, value As BoundExpression)
            If Me.State.Reachable Then
                Dim isByRefParameter As Boolean = variable.Kind = SymbolKind.Parameter AndAlso DirectCast(variable, ParameterSymbol).IsByRef
                Dim isStaticLocal As Boolean = variable.Kind = SymbolKind.Local AndAlso DirectCast(variable, LocalSymbol).IsStatic

                If IsInside AndAlso (isByRefParameter OrElse isStaticLocal AndAlso WasUsedBeforeAssignment(variable)) Then
                    _dataFlowsOut.Add(variable)
#If DEBUG Then
                    _assignedInside.Add(variable)
#End If
                End If
            End If

            MyBase.NoteWrite(variable, value)
        End Sub

        Private Function WasUsedBeforeAssignment(sym As Symbol) As Boolean
            Return Me._originalUnassigned.Contains(sym)
        End Function

        Friend Overrides Sub AssignLocalOnDeclaration(local As LocalSymbol, node As BoundLocalDeclaration)
            If local.IsStatic Then
                ' We should pretend that all the static locals are 
                ' initialized by previous method invocations
                Assign(node, node.InitializerOpt)
            Else
                MyBase.AssignLocalOnDeclaration(local, node)
            End If
        End Sub

    End Class

End Namespace
