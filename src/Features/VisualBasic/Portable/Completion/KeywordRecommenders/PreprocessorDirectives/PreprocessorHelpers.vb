' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.PreprocessorDirectives
    Friend Module PreprocessorHelpers
        <Extension()>
        Public Function GetInnermostIfPreprocessorKind(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxKind?
            Dim kindStack = New IfDirectiveVisitor(syntaxTree, position, cancellationToken).GetStack()

            If kindStack.Count = 0 Then
                Return Nothing
            Else
                Return kindStack.Peek()
            End If
        End Function

        Private Class IfDirectiveVisitor
            Inherits VisualBasicSyntaxVisitor

            Private ReadOnly _kindStack As New Stack(Of SyntaxKind)
            Private ReadOnly _maxPosition As Integer

            Public Sub New(syntaxTree As SyntaxTree, maxPosition As Integer, cancellationToken As CancellationToken)
                _maxPosition = maxPosition
                Visit(syntaxTree.GetRoot(cancellationToken))
            End Sub

            Public Overrides Sub DefaultVisit(node As SyntaxNode)
                If Not node.ContainsDirectives Then
                    Return
                End If

                For Each leadingTrivia In node.GetLeadingTrivia()
                    If leadingTrivia.HasStructure Then
                        Visit(leadingTrivia.GetStructure())
                    End If
                Next

                For Each child In node.ChildNodesAndTokens()
                    If child.FullSpan.Start > _maxPosition Then
                        Exit For
                    End If
                    If child.IsNode Then
                        Visit(child.AsNode())
                    End If
                Next

                For Each followingTrivia In node.GetTrailingTrivia()
                    If followingTrivia.HasStructure Then
                        Visit(followingTrivia.GetStructure())
                    End If
                Next
            End Sub

            Public Overrides Sub VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax)
                If node.Kind = SyntaxKind.IfDirectiveTrivia Then
                    _kindStack.Push(node.Kind)
                ElseIf node.Kind = SyntaxKind.ElseIfDirectiveTrivia Then
                    ' If we're closing our previous context, then pop that one
                    If _kindStack.Count > 0 Then
                        _kindStack.Pop()
                    End If

                    _kindStack.Push(node.Kind)
                End If
            End Sub

            Public Overrides Sub VisitElseDirectiveTrivia(node As ElseDirectiveTriviaSyntax)
                If _kindStack.Count > 0 Then
                    _kindStack.Pop()
                End If

                _kindStack.Push(node.Kind)

            End Sub

            Public Overrides Sub VisitEndIfDirectiveTrivia(node As EndIfDirectiveTriviaSyntax)
                If _kindStack.Count > 0 Then
                    _kindStack.Pop()
                End If
            End Sub

            Public Function GetStack() As Stack(Of SyntaxKind)
                Return _kindStack
            End Function
        End Class
    End Module
End Namespace
