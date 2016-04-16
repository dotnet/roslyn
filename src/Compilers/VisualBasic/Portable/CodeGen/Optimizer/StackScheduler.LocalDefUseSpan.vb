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
            ''' when current And other use spans are regular spans we can have only 2 conflict cases:
            ''' [1, 3) conflicts with [0, 2)
            ''' [1, 3) conflicts with [2, 4)
            ''' 
            ''' NOTE: With regular spans, it is not possible for two spans to share an edge point 
            ''' unless they belong to the same local. (because we cannot aceess two real locals at the same time)
            ''' 
            ''' specifically:
            ''' [1, 3) does Not conflict with [0, 1)   since such spans would need to belong to the same local
            ''' </summary>
            Public Function ConflictsWith(other As LocalDefUseSpan) As Boolean
                Return Contains(other.Start) Xor Contains(other.End)
            End Function

            Private Function Contains(val As Integer) As Boolean
                Return Me.Start < val AndAlso Me.End > val
            End Function

            ''' <summary>
            ''' Dummy locals represent implicit control flow
            ''' it is not allowed for a regular local span to cross into or 
            ''' be immediately adjacent to a dummy span.
            ''' 
            ''' specifically:
            ''' [1, 3) does conflict with [0, 1)   since that would imply a value flowing into or out of a span surrounded by a branch/label
            ''' 
            ''' </summary>
            Public Function ConflictsWithDummy(dummy As LocalDefUseSpan) As Boolean
                Return Includes(dummy.Start) Xor Includes(dummy.End)
            End Function

            Private Function Includes(val As Integer) As Boolean
                Return Me.Start <= val AndAlso Me.End >= val
            End Function


        End Class

    End Class
End Namespace

