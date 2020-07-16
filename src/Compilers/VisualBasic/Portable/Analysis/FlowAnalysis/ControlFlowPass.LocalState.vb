' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class ControlFlowPass
        Inherits AbstractFlowPass(Of LocalState)

        Protected Overrides Function IntersectWith(ByRef self As LocalState, ByRef other As LocalState) As Boolean
            Dim old As LocalState = self
            self.Alive = self.Alive Or other.Alive
            self.Reported = self.Reported And other.Reported
            Debug.Assert(Not self.Alive OrElse Not self.Reported)
            Return Not self.Equals(old)
        End Function

        Protected Overrides Sub UnionWith(ByRef self As LocalState, ByRef other As LocalState)
            self.Alive = self.Alive And other.Alive
            self.Reported = self.Reported And other.Reported
            Debug.Assert(Not self.Alive OrElse Not self.Reported)
        End Sub

        Friend Structure LocalState
            Implements AbstractLocalState

            Friend Alive As Boolean
            Friend Reported As Boolean ' reported unreachable statement

            Public Sub New(live As Boolean, reported As Boolean)
                Me.Alive = live
                Me.Reported = reported
            End Sub

            ''' <summary> Produce a duplicate of this flow analysis state. </summary>
            Public Function Clone() As LocalState Implements AbstractFlowPass(Of LocalState).AbstractLocalState.Clone
                Return Me
            End Function

        End Structure

        Protected Overrides Function Dump(state As LocalState) As String
            Return "[alive: " & state.Alive & "; reported: " & state.Reported & "]"
        End Function
    End Class

End Namespace
