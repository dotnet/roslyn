' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend Class VisualBasicSelectedMembers
        Inherits AbstractSelectedMembers(Of
            StatementSyntax,
            FieldDeclarationSyntax,
            PropertyStatementSyntax,
            TypeBlockSyntax,
            ModifiedIdentifierSyntax)

        Public Shared ReadOnly Instance As New VisualBasicSelectedMembers()

        Private Sub New()
        End Sub

        Protected Overrides Function GetAllDeclarators(field As FieldDeclarationSyntax) As IEnumerable(Of ModifiedIdentifierSyntax)
            Return field.Declarators.SelectMany(Function(d) d.Names)
        End Function

        Protected Overrides Function GetMembers(containingType As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return containingType.Members
        End Function

        Protected Overrides Function GetPropertyIdentifier(declarator As PropertyStatementSyntax) As SyntaxToken
            Return declarator.Identifier
        End Function

        Protected Overrides Function GetVariableIdentifier(declarator As ModifiedIdentifierSyntax) As SyntaxToken
            Return declarator.Identifier
        End Function
    End Class
End Namespace
