' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        Friend Overrides Function IsInsertionTrigger(text As SourceText, insertedCharacterPosition As Integer, options As OptionSet) As Boolean
            Dim ch = text(insertedCharacterPosition)
            If ch = """"c Then
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function IsPositionEntirelyWithinStringLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean
            Return syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken)
        End Function

        Protected Overrides Function GetAttributeNameOfAttributeSyntaxNode(attributeSyntaxNode As SyntaxNode) As String
            Dim attributeSyntax = TryCast(attributeSyntaxNode, AttributeSyntax)
            If attributeSyntax Is Nothing Then
                Return String.Empty
            End If

            Dim nameParts As IList(Of String) = Nothing
            If attributeSyntax.Name.TryGetNameParts(nameParts) AndAlso nameParts.Count > 0 Then
                Dim lastName = nameParts(nameParts.Count - 1)
                Return lastName
            End If

            Return String.Empty
        End Function
    End Class
End Namespace
