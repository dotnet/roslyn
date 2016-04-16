' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Structure SyntaxDiagnosticInfoList
        Implements IEnumerable(Of DiagnosticInfo), IEnumerable
        Private ReadOnly _node As VisualBasicSyntaxNode
        Private _count As Integer
        Private _list As List(Of DiagnosticInfo)

        Friend Sub New(node As VisualBasicSyntaxNode)
            Me._node = node
            Me._count = -1
            Me._list = Nothing
        End Sub

        Public ReadOnly Property Count As Integer
            Get
                If (Me._count = -1) Then
                    Me._count = SyntaxDiagnosticInfoList.CountDiagnostics(Me._node)
                End If
                Return Me._count
            End Get
        End Property

        Private Shared Function CountDiagnostics(_node As GreenNode) As Integer
            Dim n As Integer = 0
            If _node.ContainsDiagnostics Then
                Dim nodeErrors = _node.GetDiagnostics
                n = If(nodeErrors Is Nothing, 0, nodeErrors.Length)

                Dim token As SyntaxToken = TryCast(_node, SyntaxToken)
                If token IsNot Nothing Then
                    Dim leading = token.GetLeadingTrivia
                    If leading IsNot Nothing Then
                        n = (n + SyntaxDiagnosticInfoList.CountDiagnostics(leading))
                    End If
                    Dim trailing = token.GetTrailingTrivia
                    If trailing IsNot Nothing Then
                        n = (n + SyntaxDiagnosticInfoList.CountDiagnostics(trailing))
                    End If
                    Return n
                Else
                    Dim i As Integer = 0
                    Dim nc As Integer = _node.SlotCount
                    Do While (i < nc)
                        Dim child As GreenNode = _node.GetSlot(i)
                        If (Not child Is Nothing) Then
                            n = (n + SyntaxDiagnosticInfoList.CountDiagnostics(child))
                        End If
                        i += 1
                    Loop
                End If
            End If
            Return n
        End Function

        Default Public ReadOnly Property Item(index As Integer) As DiagnosticInfo
            Get
                If (Me._list Is Nothing) Then
                    Dim tmp As New List(Of DiagnosticInfo)
                    Dim d As DiagnosticInfo
                    For Each d In Me
                        tmp.Add(d)
                    Next
                    Me._list = tmp
                End If
                Return Me._list.Item(index)
            End Get
        End Property

        Private ReadOnly Property Nodes As DiagnosticInfo()
            Get
                Return Me.ToArray()
            End Get
        End Property

        Public Function GetEnumerator() As Enumerator
            Return New Enumerator(Me._node)
        End Function

        Private Function GetEnumerator1() As IEnumerator(Of DiagnosticInfo) Implements IEnumerable(Of DiagnosticInfo).GetEnumerator
            If (Me.Count = 0) Then
                Return SpecializedCollections.EmptyEnumerator(Of DiagnosticInfo)()
            End If
            Return Me.GetEnumerator
        End Function

        Private Function GetEnumerator2() As IEnumerator Implements IEnumerable.GetEnumerator
            If (Me.Count = 0) Then
                Return SpecializedCollections.EmptyEnumerator(Of DiagnosticInfo)()
            End If
            Return Me.GetEnumerator
        End Function

        Public Structure Enumerator
            Implements IEnumerator(Of DiagnosticInfo), IDisposable, IEnumerator

            Private Structure NodeIteration
                Friend ReadOnly node As GreenNode
                Friend diagnosticIndex As Integer
                Friend slotIndex As Integer

                Friend Sub New(node As GreenNode)
                    Me.node = node
                    Me.slotIndex = -1
                    Me.diagnosticIndex = -1
                End Sub
            End Structure

            Private _stack As NodeIteration()
            Private _count As Integer
            Private _current As DiagnosticInfo

            Friend Sub New(node As VisualBasicSyntaxNode)

                If node IsNot Nothing AndAlso node.ContainsDiagnostics Then
                    Me._stack = New NodeIteration(8 - 1) {}
                    Me.Push(node)
                Else
                    Me._stack = Nothing
                    Me._count = 0
                End If

                Me._current = Nothing
            End Sub

            Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
                While _count > 0

                    Dim diagIndex = Me._stack(_count - 1).diagnosticIndex
                    Dim node = Me._stack(_count - 1).node
                    Dim diags As DiagnosticInfo() = node.GetDiagnostics

                    If diags IsNot Nothing AndAlso diagIndex < diags.Length - 1 Then
                        diagIndex += 1
                        Me._current = diags(diagIndex)
                        Me._stack(_count - 1).diagnosticIndex = diagIndex
                        Return True
                    End If

                    Dim slotIndex = Me._stack(_count - 1).slotIndex

tryAgain:
                    If slotIndex < node.SlotCount - 1 Then

                        slotIndex += 1
                        Dim child = node.GetSlot(slotIndex)

                        If child Is Nothing OrElse Not child.ContainsDiagnostics Then
                            GoTo tryAgain
                        End If

                        Me._stack(_count - 1).slotIndex = slotIndex

                        Push(child)

                    Else
                        Me.Pop()

                    End If

                End While
                Return False
            End Function

            Private Sub Push(node As GreenNode)
                Dim token = TryCast(node, SyntaxToken)

                If token IsNot Nothing Then
                    PushToken(token)
                Else
                    PushNode(node)
                End If
            End Sub

            Private Sub PushToken(token As SyntaxToken)
                Dim trailing = token.GetTrailingTrivia
                If trailing IsNot Nothing Then
                    Me.Push(trailing)
                End If

                PushNode(token)

                Dim leading = token.GetLeadingTrivia
                If leading IsNot Nothing Then
                    Me.Push(leading)
                End If
            End Sub

            Private Sub PushNode(node As GreenNode)
                If Me._count >= Me._stack.Length Then
                    Dim tmp As NodeIteration() = New NodeIteration((Me._stack.Length * 2) - 1) {}
                    Array.Copy(Me._stack, tmp, Me._stack.Length)
                    Me._stack = tmp
                End If
                Me._stack(Me._count) = New NodeIteration(node)
                Me._count += 1
            End Sub

            Private Sub Pop()
                Me._count -= 1
            End Sub

            Public ReadOnly Property Current As DiagnosticInfo Implements IEnumerator(Of DiagnosticInfo).Current
                Get
                    Return Me._current
                End Get
            End Property

            Private ReadOnly Property Current1 As Object Implements IEnumerator.Current
                Get
                    Return Me.Current
                End Get
            End Property

            Private Sub Reset() Implements IEnumerator.Reset
            End Sub

            Private Sub Dispose() Implements IDisposable.Dispose
            End Sub
        End Structure
    End Structure

End Namespace
