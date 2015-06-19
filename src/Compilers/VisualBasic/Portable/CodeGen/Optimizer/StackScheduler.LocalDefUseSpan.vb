' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        ''' <summary>
        ''' Represents a span of a value between definition and use. Start/end positions are 
        ''' specified in terms of global node count as visited by StackOptimizer visitors. 
        ''' (i.e. recursive walk not looking into constants)
        ''' </summary>
        Private Class LocalDefUseSpan

            Public ReadOnly Start As Integer

            Private _end As Integer

            Public ReadOnly Property [End] As Integer
                Get
                    Return _end
                End Get
            End Property

            Public Sub New(assigned As Integer)
                Me.Start = assigned
                Me._end = assigned
            End Sub

            Public Sub SetEnd(newEnd As Integer)
                Debug.Assert(Me._end <= newEnd)
                Me._end = newEnd
            End Sub

            Public Overrides Function ToString() As String
                Return "[" & Me.Start.ToString() & ", " & Me.End.ToString() & ")"
            End Function

            ''' <summary>
            ''' That said, when current and other use spans are regular spans we can have 
            ''' only 2 conflict cases:
            '''      [1, 3) conflicts with [0, 2) 
            '''      [1, 3) conflicts with [2, 4) 
            ''' 
            ''' specifically: 
            '''      [1, 3) does not conflict with [0, 1) 
            ''' 
            ''' NOTE: with regular spans, it is not possible to have start1 == start2 or 
            ''' end1 == end2 since at the same node we can access only one real local.
            ''' 
            ''' However at the same node we can access one or more dummy locals. So we can 
            ''' have start1 == start2 and end1 == end2 scenarios, but only if the other span 
            ''' is a span of a dummy. 
            ''' </summary>
            Public Function ConflictsWith(other As LocalDefUseSpan) As Boolean
                ' NOTE: this logic is moved from CS as-is
                ' TODO: revise the definition of ConflictsWith
                Dim containsStart As Boolean = other.ContainsStart(Me.Start)
                Dim containsEnd = other.ContainsEnd(Me.End)
                Return containsStart Xor containsEnd
            End Function

            Private Function ContainsStart(otherStart As Integer) As Boolean
                Return Me.Start <= otherStart AndAlso Me.End > otherStart
            End Function

            Private Function ContainsEnd(otherEnd As Integer) As Boolean
                Return Me.Start < otherEnd AndAlso Me.End > otherEnd
            End Function

        End Class

    End Class
End Namespace

