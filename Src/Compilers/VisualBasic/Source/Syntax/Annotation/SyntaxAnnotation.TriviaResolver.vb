#If False Then
Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Partial Public Class SyntaxAnnotation
        Private Class TriviaResolver
            Inherits SyntaxWalker(Of List(Of SyntaxTrivia))
            Private ReadOnly annotation As SyntaxAnnotation

            Public Shared Function Resolve(root As SyntaxNode, annotation As SyntaxAnnotation) As IEnumerable(Of SyntaxTrivia)
                Contract.ThrowIfNull(root)
                Contract.ThrowIfNull(annotation)

                Dim result = New List(Of SyntaxTrivia)()
                Dim resolver = New TriviaResolver(annotation)
                resolver.Visit(root, result)

                Return result
            End Function

            Private Sub New(annotation As SyntaxAnnotation)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me.annotation = annotation
            End Sub

            Public Overrides Function Visit(node As SyntaxNode, Optional results As List(Of SyntaxTrivia) = Nothing) As Object
                ' if it doesnt have annotations, don't even bother to go in.
                If Not node.HasAnnotations Then
                    Return Nothing
                End If

                Return MyBase.Visit(node, results)
            End Function

            Public Overrides Sub VisitToken(token As SyntaxToken, results As List(Of SyntaxTrivia))
                ' if it doesnt have annotations, don't even bother to go in.
                If Not token.HasAnnotations Then
                    Return
                End If

                MyBase.VisitToken(token, results)
            End Sub

            Public Overrides Sub VisitTrivia(trivia As SyntaxTrivia, results As List(Of SyntaxTrivia))
                ' if it doesnt have annotations, don't even bother to go in.
                If Not trivia.HasAnnotations Then
                    Return
                End If

                Dim annotations = trivia.UnderlyingNode.GetAnnotations()
                For i As Integer = 0 To annotations.Length - 1
                    If annotations(i) Is annotation Then
                        results.Add(trivia)
                        Exit For
                    End If
                Next i

                MyBase.VisitTrivia(trivia, results)
            End Sub
        End Class
    End Class
End Namespace
#End If
