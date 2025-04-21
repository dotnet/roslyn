' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' A field that's part of declaration with a single identifier and As New clause:
    '''   Dim [|a As New C()|]
    ''' </summary>
    Friend NotInheritable Class FieldWithSingleAsNewClauseDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Private ReadOnly _variableDeclarator As VariableDeclaratorSyntax

        Public Sub New(variableDeclarator As VariableDeclaratorSyntax)
            _variableDeclarator = variableDeclarator
        End Sub

        Private ReadOnly Property NewExpression As SyntaxNode
            Get
                Return DirectCast(_variableDeclarator.AsClause, AsNewClauseSyntax).NewExpression
            End Get
        End Property

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return _variableDeclarator
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return NewExpression
            End Get
        End Property

        Public Overrides Function GetUserCodeTokens(getDescendantTokens As Func(Of SyntaxNode, IEnumerable(Of SyntaxToken))) As IEnumerable(Of SyntaxToken)
            Return getDescendantTokens(NewExpression)
        End Function
    End Class
End Namespace
