' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Protected Overrides Function GetMembers(containingType As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return containingType.Members
        End Function

        Protected Overrides Function GetDeclarationsAndIdentifiers(member As StatementSyntax) As IEnumerable(Of (identifier As SyntaxToken, declaration As SyntaxNode))
            If TypeOf member Is FieldDeclarationSyntax Then
                Return DirectCast(member, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(decl) decl.Names.
                        Select(Function(name) (identifier:=name.Identifier, declaration:=DirectCast(name, SyntaxNode))))
            Else
                Return ImmutableArray.Create((identifier:=member.GetNameToken(), declaration:=DirectCast(member, SyntaxNode)))
            End If
        End Function
    End Class
End Namespace
