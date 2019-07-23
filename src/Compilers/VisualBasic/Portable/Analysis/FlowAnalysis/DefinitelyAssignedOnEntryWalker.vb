' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If DEBUG Then
' See comment in DefiniteAssignment.
#Const REFERENCE_STATE = True
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A region analysis walker that computes the set of variables that are definitely assigned
    ''' when a region is entered.
    ''' </summary>
    Friend Class DefinitelyAssignedOnEntryWalker
        Inherits AbstractRegionDataFlowPass

        Private ReadOnly _definitelyAssignedOnEntry As New HashSet(Of Symbol)()

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As HashSet(Of Symbol)
            Dim walker = New DefinitelyAssignedOnEntryWalker(info, region)
            Try
                Dim success = walker.Analyze()
                Return If(success, walker._definitelyAssignedOnEntry, New HashSet(Of Symbol))
            Finally
                walker.Free()
            End Try
        End Function

        Protected Overrides Sub EnterRegion()
            ' this can happen multiple times as flow analysis Is multi-pass.  Always 
            ' take the latest data And use that to determine our result.
            _definitelyAssignedOnEntry.Clear()

            If Me.IsConditionalState Then
                ' We're in a state where there are different flow paths (i.e. when-true and when-false).
                ' In that case, a variable Is only definitely assigned if it's definitely assigned through
                ' both paths.
                Me.ProcessState(Me.StateWhenTrue, Me.StateWhenFalse)
            Else
                Me.ProcessState(Me.State, state2opt:=Nothing)
            End If

            MyBase.EnterRegion()
        End Sub

#If REFERENCE_STATE Then
        Private Sub ProcessState(state1 As LocalState, state2opt As LocalState)
#Else
        Private Sub ProcessState(state1 As LocalState, state2opt As LocalState?)
#End If
            For Each slot In state1.Assigned.TrueBits()
                If slot < variableBySlot.Length AndAlso
                    state2opt?.IsAssigned(slot) <> False Then

                    Dim symbol = variableBySlot(slot).Symbol
                    If symbol IsNot Nothing AndAlso
                       symbol.Kind <> SymbolKind.Field Then

                        _definitelyAssignedOnEntry.Add(symbol)
                    End If
                End If
            Next
        End Sub
    End Class
End Namespace
