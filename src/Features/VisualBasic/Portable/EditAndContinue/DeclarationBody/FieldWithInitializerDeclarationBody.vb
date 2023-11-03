' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' Field declarations:
    '''   Dim [|a = expr|]
    '''   Dim [|a As Integer = expr|]
    '''   Dim [|a = expr|], [|b = expr|], [|c As Integer = expr|]
    ''' </summary>
    Friend NotInheritable Class FieldWithInitializerDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Private ReadOnly _variableDeclarator As VariableDeclaratorSyntax

        Public Sub New(variableDeclarator As VariableDeclaratorSyntax)
            Debug.Assert(variableDeclarator.Names.Count = 1)
            _variableDeclarator = variableDeclarator
        End Sub

        Public ReadOnly Property Name As ModifiedIdentifierSyntax
            Get
                Return _variableDeclarator.Names(0)
            End Get
        End Property

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return _variableDeclarator
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return _variableDeclarator.Initializer.Value
            End Get
        End Property
    End Class
End Namespace
