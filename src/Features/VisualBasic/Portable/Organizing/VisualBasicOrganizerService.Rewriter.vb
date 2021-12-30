' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Organizing.Organizers

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing
    Partial Friend Class VisualBasicOrganizingService
        Public Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _nodeToOrganizersGetter As Func(Of SyntaxNode, IEnumerable(Of ISyntaxOrganizer))
            Private ReadOnly _semanticModel As SemanticModel
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(treeOrganizer As VisualBasicOrganizingService,
                           organizers As IEnumerable(Of ISyntaxOrganizer),
                           semanticModel As SemanticModel,
                           token As CancellationToken)
                Me._nodeToOrganizersGetter = treeOrganizer.GetNodeToOrganizers(organizers.ToList())
                Me._semanticModel = semanticModel
                Me._cancellationToken = token
            End Sub

            Public Overrides Function DefaultVisit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()
                Return MyBase.DefaultVisit(node)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                If node Is Nothing Then
                    Return Nothing
                End If

                ' First, recurse into our children, updating them.
                node = MyBase.Visit(node)

                ' Now, try to update this new node itself.
                Dim organizers = Me._nodeToOrganizersGetter(node)
                For Each organizer In organizers
                    node = organizer.OrganizeNode(_semanticModel, node, _cancellationToken)
                Next

                Return node
            End Function
        End Class
    End Class
End Namespace
