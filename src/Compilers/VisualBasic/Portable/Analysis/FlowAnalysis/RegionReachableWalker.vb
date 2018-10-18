' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind
Imports Microsoft.CodeAnalysis.VisualBasic.VisualBasicControlFlowAnalysis


Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that computes whether or not the region completes normally.  It does this by determining 
    ''' if the point at which the region ends is reachable.
    ''' </summary>
    Friend Class RegionReachableWalker
        Inherits AbstractRegionControlFlowPass

        Friend Overloads Shared Function Analyze(
                                             info As FlowAnalysisInfo,
                                             region As FlowAnalysisRegionInfo
                                           ) As (StartPoint As ReachableStates, EndPoint As ReachableStates)

            Dim walker = New RegionReachableWalker(info, region)
            With walker
                Try
                    If walker.Analyze() Then
                        Return (StartPoint:=If(._RegionIsReachable.StartPoint <> ReachableStates.Unknown, ._RegionIsReachable.StartPoint, ReachableStates.Reachable),
                                EndPoint:=If(._RegionIsReachable.EndPoint <> ReachableStates.Unknown, ._RegionIsReachable.EndPoint, AsReachableState(.State.Alive)))
                    Else
                        Return (ReachableStates.Reachable, ReachableStates.Reachable)
                    End If
                Finally
                    .Free()
                End Try
            End With
        End Function

        Private _RegionIsReachable As (StartPoint As ReachableStates, EndPoint As ReachableStates) = (ReachableStates.Unknown, ReachableStates.Unknown)

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Protected Overrides Sub EnterRegion()
            _RegionIsReachable.StartPoint = AsReachableState(State.Alive)
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub LeaveRegion()
            _RegionIsReachable.EndPoint = AsReachableState(State.Alive)
            MyBase.LeaveRegion()
        End Sub

    End Class

End Namespace
