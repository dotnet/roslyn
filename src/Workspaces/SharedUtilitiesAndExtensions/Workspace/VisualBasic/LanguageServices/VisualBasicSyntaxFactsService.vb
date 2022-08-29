' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class VisualBasicSyntaxFactsServiceFactory
        Private NotInheritable Class VisualBasicSyntaxFactsService
            Inherits VisualBasicSyntaxFacts
            Implements ISyntaxFactsService

            Public Shared Shadows ReadOnly Property Instance As New VisualBasicSyntaxFactsService

            Private Sub New()
            End Sub

            Public Function IsInNonUserCode(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInNonUserCode
                If syntaxTree Is Nothing Then
                    Return False
                End If

                Return syntaxTree.IsInNonUserCode(position, cancellationToken)
            End Function

            Public Sub AddFirstMissingCloseBrace(Of TContextNode As SyntaxNode)(
                    root As SyntaxNode, contextNode As TContextNode,
                    ByRef newRoot As SyntaxNode, ByRef newContextNode As TContextNode) Implements ISyntaxFactsService.AddFirstMissingCloseBrace
                ' Nothing to be done.  VB doesn't have close braces
                newRoot = root
                newContextNode = contextNode
            End Sub

            Public Function GetSelectedFieldsAndPropertiesAsync(tree As SyntaxTree, textSpan As TextSpan, allowPartialSelection As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of SyntaxNode)) Implements ISyntaxFactsService.GetSelectedFieldsAndPropertiesAsync
                Return VisualBasicSelectedMembers.Instance.GetSelectedFieldsAndPropertiesAsync(tree, textSpan, allowPartialSelection, cancellationToken)
            End Function
        End Class
    End Class
End Namespace
