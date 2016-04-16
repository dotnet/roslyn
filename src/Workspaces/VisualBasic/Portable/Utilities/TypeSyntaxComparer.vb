' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class TypeSyntaxComparer
        Implements IComparer(Of TypeSyntax)
        Private ReadOnly _tokenComparer As IComparer(Of SyntaxToken)
        Friend nameComparer As IComparer(Of NameSyntax)

        Friend Sub New(tokenComparer As IComparer(Of SyntaxToken))
            Me._tokenComparer = tokenComparer
        End Sub

        Public Shared Function Create() As IComparer(Of NameSyntax)
            Return Create(TokenComparer.NormalInstance)
        End Function

        Public Shared Function Create(tokenComparer As IComparer(Of SyntaxToken)) As IComparer(Of TypeSyntax)
            Dim nameComparer = New NameSyntaxComparer(tokenComparer)
            Dim typeComparer = New TypeSyntaxComparer(tokenComparer)

            nameComparer.typeComparer = typeComparer
            typeComparer.nameComparer = nameComparer

            Return typeComparer
        End Function

        Public Function Compare(x As TypeSyntax, y As TypeSyntax) As Integer Implements IComparer(Of TypeSyntax).Compare
            If x Is y Then
                Return 0
            End If

            x = UnwrapType(x)
            y = UnwrapType(y)

            If TypeOf x Is NameSyntax AndAlso TypeOf y Is NameSyntax Then
                Return nameComparer.Compare(DirectCast(x, NameSyntax), DirectCast(y, NameSyntax))
            End If

            ' we have two predefined types, or a predefined type and a normal VB name.  We only need
            ' to compare the first tokens here.
            Return _tokenComparer.Compare(x.GetFirstToken(), y.GetFirstToken())
        End Function

        Private Function UnwrapType(type As TypeSyntax) As TypeSyntax
            While True
                Select Case type.Kind
                    Case SyntaxKind.ArrayType
                        type = DirectCast(type, ArrayTypeSyntax).ElementType
                    Case SyntaxKind.NullableType
                        type = DirectCast(type, NullableTypeSyntax).ElementType
                    Case Else
                        Return type
                End Select
            End While

            ' This should be unhittable, but the VB compiler can't tell that. 
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
