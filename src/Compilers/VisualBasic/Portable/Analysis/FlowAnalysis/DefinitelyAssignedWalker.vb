' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

#If DEBUG Then
' See comment in DefiniteAssignment.
#Const REFERENCE_STATE = True
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A region analysis walker that computes the set of variables that are definitely assigned
    ''' when a region is entered.
    ''' </summary>
    Friend Class DefinitelyAssignedWalker
        Inherits AbstractRegionDataFlowPass

        Private ReadOnly _definitelyAssignedOnEntry As New HashSet(Of Symbol)()
        Private ReadOnly _definitelyAssignedOnExit As New HashSet(Of Symbol)()

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As (entry As HashSet(Of Symbol), ex As HashSet(Of Symbol))
            Dim walker = New DefinitelyAssignedWalker(info, region)
            Try
                Dim success = walker.Analyze()
                Return If(success,
                    (walker._definitelyAssignedOnEntry, walker._definitelyAssignedOnExit),
                    (New HashSet(Of Symbol), New HashSet(Of Symbol)))
            Finally
                walker.Free()
            End Try
        End Function

        Protected Overrides Sub EnterRegion()
            ProcessRegion(_definitelyAssignedOnEntry)
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub LeaveRegion()
            ProcessRegion(_definitelyAssignedOnExit)
            MyBase.LeaveRegion()
        End Sub

        Private Sub ProcessRegion(definitelyAssigned As HashSet(Of Symbol))
            ' this can happen multiple times as flow analysis Is multi-pass.  Always 
            ' take the latest data And use that to determine our result.
            definitelyAssigned.Clear()

            If Me.IsConditionalState Then
                ' We're in a state where there are different flow paths (i.e. when-true and when-false).
                ' In that case, a variable Is only definitely assigned if it's definitely assigned through
                ' both paths.
                Me.ProcessState(definitelyAssigned, Me.StateWhenTrue, Me.StateWhenFalse)
            Else
                Me.ProcessState(definitelyAssigned, Me.State, state2opt:=Nothing)
            End If
        End Sub

#If REFERENCE_STATE Then
        Private Sub ProcessState(definitelyAssigned As HashSet(Of Symbol), state1 As LocalState, state2opt As LocalState)
#Else
        Private Sub ProcessState(definitelyAssigned As HashSet(Of Symbol), state1 As LocalState, state2opt As LocalState?)
#End If
            For Each slot In state1.Assigned.TrueBits()
                If slot < variableBySlot.Length Then
#If REFERENCE_STATE Then
                    If state2opt Is Nothing OrElse state2opt.IsAssigned(slot) Then
#Else
                    If state2opt Is Nothing OrElse state2opt.Value.IsAssigned(slot) Then
#End If
                        Dim symbol = variableBySlot(slot).Symbol
                        If symbol IsNot Nothing AndAlso
                           symbol.Kind <> SymbolKind.Field Then

                            definitelyAssigned.Add(symbol)
                        End If
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace
