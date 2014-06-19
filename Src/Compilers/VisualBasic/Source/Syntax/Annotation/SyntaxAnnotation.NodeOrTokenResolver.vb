#If False Then
Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Partial Public Class SyntaxAnnotation
        Private Class NodeOrTokenResolver
            Inherits SyntaxWalker(Of List(Of SyntaxNodeOrToken))
            Private ReadOnly annotation As SyntaxAnnotation

            Public Shared Function Resolve(root As SyntaxNode, annotation As SyntaxAnnotation) As IEnumerable(Of SyntaxNodeOrToken)
                Contract.ThrowIfNull(root)
                Contract.ThrowIfNull(annotation)

                Dim result = New List(Of SyntaxNodeOrToken)()
                Dim resolver = New NodeOrTokenResolver(annotation)
                resolver.Visit(root, result)

                Return result
            End Function

            Private Sub New(annotation As SyntaxAnnotation)
                MyBase.New(VisitIntoStructuredTrivia:=True)
                Me.annotation = annotation
            End Sub

            Public Overrides Function Visit(node As SyntaxNode,Optional results As List(Of SyntaxNodeOrToken) = Nothing) As Object
                ' if it doesnt have annotations, don't even bother to go in.
                If Not node.HasAnnotations Then
                    Return Nothing
                End If

                Dim annotations = node.Green.GetAnnotations()
                AddNodeIfAnnotationExist(annotations, node, results)

                Return MyBase.Visit(node, results)
            End Function

            Public Overrides Sub VisitToken(token As SyntaxToken, results As List(Of SyntaxNodeOrToken))
                ' if it doesnt have annotations, don't even bother to go in.
                If Not token.HasAnnotations Then
                    Return
                End If

                Dim annotations = token.Node.GetAnnotations()
                AddNodeIfAnnotationExist(annotations, token, results)

                MyBase.VisitToken(token, results)
            End Sub

            Private Sub AddNodeIfAnnotationExist(annotations() As SyntaxAnnotation, nodeOrToken As SyntaxNodeOrToken, results As List(Of SyntaxNodeOrToken))
                For i As Integer = 0 To annotations.Length - 1
                    If annotations(i) Is annotation Then
                        results.Add(nodeOrToken)
                        Return
                    End If
                Next i
            End Sub
        End Class
    End Class
End Namespace
#End If