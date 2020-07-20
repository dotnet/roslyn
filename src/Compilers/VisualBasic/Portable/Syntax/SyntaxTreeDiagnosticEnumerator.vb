' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Structure SyntaxTreeDiagnosticEnumerator

        Private Structure NodeIteration
            Friend ReadOnly node As GreenNode
            Friend diagnosticIndex As Integer
            Friend slotIndex As Integer
            Friend ReadOnly inDocumentationComment As Boolean

            Friend Sub New(node As GreenNode, inDocumentationComment As Boolean)
                Me.node = node
                Me.slotIndex = -1
                Me.diagnosticIndex = -1
                Me.inDocumentationComment = inDocumentationComment
            End Sub
        End Structure

        Private ReadOnly _tree As SyntaxTree
        Private _stack As NodeIteration()
        Private _count As Integer
        Private _current As Diagnostic
        Private _position As Integer

        Friend Sub New(tree As SyntaxTree, node As InternalSyntax.VisualBasicSyntaxNode, position As Integer, inDocumentationComment As Boolean)
            If node IsNot Nothing AndAlso node.ContainsDiagnostics Then
                Me._tree = tree
                Me._stack = New NodeIteration(8 - 1) {}
                Me.Push(node, inDocumentationComment)
            Else
                Me._tree = Nothing
                Me._stack = Nothing
                Me._count = 0
            End If
            Me._current = Nothing
            Me._position = position
        End Sub

        Public Function MoveNext() As Boolean
            While _count > 0

                Dim diagIndex = Me._stack(_count - 1).diagnosticIndex
                Dim node = Me._stack(_count - 1).node
                Dim diags = node.GetDiagnostics
                Dim inDocumentationComment = Me._stack(_count - 1).inDocumentationComment

                If diags IsNot Nothing AndAlso diagIndex < diags.Length - 1 Then
                    diagIndex += 1
                    Dim di = diags(diagIndex)
                    If inDocumentationComment Then
                        di = ErrorFactory.ErrorInfo(ERRID.WRN_XMLDocParseError1, di)
                    End If

                    ' Tokens have already processed leading trivia, so only add leading trivia width for non-tokens [bug 4745]
                    Dim position As Integer = Me._position
                    If Not node.IsToken Then
                        position += node.GetLeadingTriviaWidth()
                    End If

                    Me._current = New VBDiagnostic(di, Me._tree.GetLocation(New TextSpan(position, node.Width)))
                    Me._stack(_count - 1).diagnosticIndex = diagIndex
                    Return True
                End If

                Dim slotIndex = Me._stack(_count - 1).slotIndex
                inDocumentationComment = inDocumentationComment OrElse node.RawKind = SyntaxKind.DocumentationCommentTrivia

tryAgain:
                If slotIndex < node.SlotCount - 1 Then

                    slotIndex += 1
                    Dim child = node.GetSlot(slotIndex)

                    If child Is Nothing Then
                        GoTo tryAgain
                    End If

                    If Not child.ContainsDiagnostics Then
                        Me._position += child.FullWidth
                        GoTo tryAgain
                    End If

                    Me._stack(_count - 1).slotIndex = slotIndex

                    Push(child, inDocumentationComment)

                Else
                    If node.SlotCount = 0 Then
                        Me._position += node.Width
                    End If

                    Me.Pop()

                End If

            End While
            Return False
        End Function

        Private Sub Push(node As GreenNode, inDocumentationComment As Boolean)
            Dim token = TryCast(node, InternalSyntax.SyntaxToken)

            If token IsNot Nothing Then
                PushToken(token, inDocumentationComment)
            Else
                PushNode(node, inDocumentationComment)
            End If
        End Sub

        Private Sub PushToken(token As InternalSyntax.SyntaxToken, inDocumentationComment As Boolean)
            Dim trailing = token.GetTrailingTrivia
            If trailing IsNot Nothing Then
                Me.Push(trailing, inDocumentationComment)
            End If

            PushNode(token, inDocumentationComment)

            Dim leading = token.GetLeadingTrivia
            If leading IsNot Nothing Then
                Me.Push(leading, inDocumentationComment)
            End If
        End Sub

        Private Sub PushNode(node As GreenNode, inDocumentationComment As Boolean)
            If Me._count >= Me._stack.Length Then
                Dim tmp As NodeIteration() = New NodeIteration((Me._stack.Length * 2) - 1) {}
                Array.Copy(Me._stack, tmp, Me._stack.Length)
                Me._stack = tmp
            End If
            Me._stack(Me._count) = New NodeIteration(node, inDocumentationComment)
            Me._count += 1
        End Sub

        Private Sub Pop()
            Me._count -= 1
        End Sub

        Public ReadOnly Property Current As Diagnostic
            Get
                Return Me._current
            End Get
        End Property

    End Structure

End Namespace
