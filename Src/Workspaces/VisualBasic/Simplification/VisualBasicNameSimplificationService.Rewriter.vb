Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.Collections

Namespace Roslyn.Services.VisualBasic.Simplification
    Partial Friend Class VisualBasicNameSimplificationService
        Private Class Rewriter
            Inherits SyntaxRewriter

            Private ReadOnly _semanticModel As ISemanticModel
            Private ReadOnly _spans As SimpleIntervalTree(Of TextSpan)
            Private ReadOnly _cancellationToken As CancellationToken

            Private _simplifiedNodes As New HashSet(Of SyntaxNode)
            Public ReadOnly Property SimplifiedNodes As HashSet(Of SyntaxNode)
                Get
                    Return _simplifiedNodes
                End Get
            End Property

            Public Sub New(semanticModel As ISemanticModel,
                           spans As SimpleIntervalTree(Of TextSpan),
                           cancellationToken As CancellationToken)
                _semanticModel = semanticModel
                _spans = spans
                _cancellationToken = cancellationToken
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()
                Return MyBase.Visit(node)
            End Function

            Private Function TrySimplify(node As SyntaxNode, ByRef result As SyntaxNode) As Boolean
                If _spans.GetOverlappingIntervals(node.Span.Start, node.Span.Length).Any() Then
                    Dim simplified = SimplifyNode(_semanticModel, node)
                    If simplified IsNot node Then
                        result = simplified
                        _simplifiedNodes.Add(node)
                        Return True
                    End If
                End If

                Return False
            End Function

            Public Overrides Function VisitQualifiedName(node As QualifiedNameSyntax) As SyntaxNode
                Dim result As SyntaxNode = Nothing
                If TrySimplify(node, result) Then
                    Return result
                End If

                Return MyBase.VisitQualifiedName(node)
            End Function

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Dim result As SyntaxNode = Nothing
                If TrySimplify(node, result) Then
                    Return result
                End If

                Return MyBase.VisitMemberAccessExpression(node)
            End Function
        End Class
    End Class
End Namespace