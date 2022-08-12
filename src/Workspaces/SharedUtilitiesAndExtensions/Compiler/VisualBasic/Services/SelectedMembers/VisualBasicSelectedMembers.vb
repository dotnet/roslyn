' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
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

        Protected Overrides Function GetDeclaratorsAndIdentifiers(member As StatementSyntax) As ImmutableArray(Of (declarator As SyntaxNode, identifier As SyntaxToken))
            If TypeOf member Is FieldDeclarationSyntax Then
                Return DirectCast(member, FieldDeclarationSyntax).Declarators.
                    SelectMany(Function(decl) decl.Names.
                        Select(Function(name) (declarator:=DirectCast(name, SyntaxNode), identifier:=name.Identifier))).
                    AsImmutable()
            Else
                Return ImmutableArray.Create((declaration:=DirectCast(member, SyntaxNode), identifier:=member.GetNameToken()))
            End If
        End Function
    End Class
End Namespace
