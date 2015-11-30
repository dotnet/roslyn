' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
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
                slotIndex = -1
                diagnosticIndex = -1
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
                _tree = tree
                _stack = New NodeIteration(8 - 1) {}
                Push(node, inDocumentationComment)
            Else
                _tree = Nothing
                _stack = Nothing
                _count = 0
            End If
            _current = Nothing
            _position = position
        End Sub

        Public Function MoveNext() As Boolean
            While _count > 0

                Dim diagIndex = _stack(_count - 1).diagnosticIndex
                Dim node = _stack(_count - 1).node
                Dim diags = node.GetDiagnostics
                Dim inDocumentationComment = _stack(_count - 1).inDocumentationComment

                If diags IsNot Nothing AndAlso diagIndex < diags.Length - 1 Then
                    diagIndex += 1
                    Dim di = diags(diagIndex)
                    If inDocumentationComment Then
                        di = ErrorFactory.ErrorInfo(ERRID.WRN_XMLDocParseError1, di)
                    End If

                    ' Tokens have already processed leading trivia, so only add leading trivia width for non-tokens [bug 4745]
                    Dim position As Integer = _position
                    If Not node.IsToken Then
                        position += node.GetLeadingTriviaWidth()
                    End If

                    _current = New VBDiagnostic(di, _tree.GetLocation(New TextSpan(position, node.Width)))
                    _stack(_count - 1).diagnosticIndex = diagIndex
                    Return True
                End If

                Dim slotIndex = _stack(_count - 1).slotIndex
                inDocumentationComment = inDocumentationComment OrElse node.RawKind = SyntaxKind.DocumentationCommentTrivia

tryAgain:
                If slotIndex < node.SlotCount - 1 Then

                    slotIndex += 1
                    Dim child = node.GetSlot(slotIndex)

                    If child Is Nothing Then
                        GoTo tryAgain
                    End If

                    If Not child.ContainsDiagnostics Then
                        _position += child.FullWidth
                        GoTo tryAgain
                    End If

                    _stack(_count - 1).slotIndex = slotIndex

                    Push(child, inDocumentationComment)

                Else
                    If node.SlotCount = 0 Then
                        _position += node.Width
                    End If

                    Pop()

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
                Push(trailing, inDocumentationComment)
            End If

            PushNode(token, inDocumentationComment)

            Dim leading = token.GetLeadingTrivia
            If leading IsNot Nothing Then
                Push(leading, inDocumentationComment)
            End If
        End Sub

        Private Sub PushNode(node As GreenNode, inDocumentationComment As Boolean)
            If _count >= _stack.Length Then
                Dim tmp As NodeIteration() = New NodeIteration((_stack.Length * 2) - 1) {}
                Array.Copy(_stack, tmp, _stack.Length)
                _stack = tmp
            End If
            _stack(_count) = New NodeIteration(node, inDocumentationComment)
            _count += 1
        End Sub

        Private Sub Pop()
            _count -= 1
        End Sub

        Public ReadOnly Property Current As Diagnostic
            Get
                Return _current
            End Get
        End Property

    End Structure

End Namespace
