' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

' NOTE: VB does not support constant expressions in flow analysis during command-line compilation, but supports them when 
'       analysis is being called via public API. This distinction is governed by 'suppressConstantExpressions' flag

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class AbstractFlowPass(Of LocalState As AbstractLocalState)
        Inherits BoundTreeVisitor

        ''' <summary>
        ''' BlockLevel is used to keep track of the lexical nesting level of label and goto statements. 
        ''' The other most block has a path of {}
        ''' </summary>
        ''' <remarks></remarks>
        Friend Structure BlockNesting
            Private ReadOnly _path As ImmutableArray(Of Integer)

            Public Function IsPrefixedBy(other As ArrayBuilder(Of Integer), ignoreLast As Boolean) As Boolean
                Dim count As Integer = other.Count
                If ignoreLast Then
                    count -= 1
                End If

                If count <= Me._path.Length Then
                    For i = 0 To count - 1
                        If Me._path(i) <> other(i) Then
                            Return False
                        End If
                    Next

                    Return True
                End If

                Return False
            End Function

            Private Sub New(builder As ArrayBuilder(Of Integer))
                Me._path = builder.ToImmutable()
            End Sub

            Public Shared Widening Operator CType(builder As ArrayBuilder(Of Integer)) As BlockNesting
                Return New BlockNesting(builder)
            End Operator

        End Structure

        ''' <summary>
        ''' The state associated with a label includes the statement itself, the local state and the nesting.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Structure LabelStateAndNesting
            Public ReadOnly Target As BoundLabelStatement
            Public ReadOnly State As LocalState
            Public ReadOnly Nesting As BlockNesting

            Public Sub New(target As BoundLabelStatement, state As LocalState, nesting As BlockNesting)
                Me.Target = target
                Me.State = state
                Me.Nesting = nesting
            End Sub
        End Structure

        ''' <summary>
        ''' A pending branch.  There are created for a return, break, continue, or goto statement.  The
        ''' idea is that we don't know if the branch will eventually reach its destination because of an
        ''' intervening finally block that cannot complete normally.  So we store them up and handle them
        ''' as we complete processing each construct.  At the end of a block, if there are any pending
        ''' branches to a label in that block we process the branch.  Otherwise we relay it up to the
        ''' enclosing construct as a pending branch of the enclosing construct.
        ''' </summary>
        Friend Class PendingBranch
            Public ReadOnly Branch As BoundStatement
            Public State As LocalState
            Public Nesting As BlockNesting

            Public ReadOnly Property Label As LabelSymbol
                Get
                    Select Case Branch.Kind
                        Case BoundKind.ConditionalGoto
                            Return CType(Branch, BoundConditionalGoto).Label
                        Case BoundKind.GotoStatement
                            Return CType(Branch, BoundGotoStatement).Label
                        Case BoundKind.ExitStatement
                            Return CType(Branch, BoundExitStatement).Label
                        Case BoundKind.ContinueStatement
                            Return CType(Branch, BoundContinueStatement).Label
                        Case Else
                            Return Nothing
                    End Select
                End Get
            End Property

            Public Sub New(branch As BoundStatement, state As LocalState, nesting As BlockNesting)
                Me.Branch = branch
                Me.State = state.Clone()
                Me.Nesting = nesting
            End Sub
        End Class

    End Class

End Namespace
