' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Organizing.Organizers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Organizing
    Friend Partial Class VisualBasicOrganizingService
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
