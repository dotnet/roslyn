' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A region analysis walker that computes whether or not the region completes normally.  It does this by determining 
    ''' if the point at which the region ends is reachable.
    ''' </summary>
    Friend Class RegionReachableWalker
        Inherits AbstractRegionControlFlowPass

        Friend Overloads Shared Sub Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                                            <Out()> ByRef startPointIsReachable As Boolean, <Out()> ByRef endPointIsReachable As Boolean)

            Dim walker = New RegionReachableWalker(info, region)
            Try
                If walker.Analyze() Then
                    startPointIsReachable = If(walker._regionStartPointIsReachable.HasValue, walker._regionStartPointIsReachable.Value, True)
                    endPointIsReachable = If(walker._regionEndPointIsReachable.HasValue, walker._regionEndPointIsReachable.Value, walker.State.Alive)
                Else
                    startPointIsReachable = True
                    startPointIsReachable = False
                End If
            Finally
                walker.Free()
            End Try
        End Sub

        Private _regionStartPointIsReachable As Boolean?
        Private _regionEndPointIsReachable As Boolean?

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Protected Overrides Sub EnterRegion()
            _regionStartPointIsReachable = State.Alive
            MyBase.EnterRegion()
        End Sub

        Protected Overrides Sub LeaveRegion()
            _regionEndPointIsReachable = State.Alive
            MyBase.LeaveRegion()
        End Sub

    End Class

End Namespace
