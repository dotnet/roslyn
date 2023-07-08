' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' A field that's part of declaration with single identifier with array bounds.
    '''   Dim [|a(n)|] As Integer
    ''' </summary>
    Friend NotInheritable Class FieldWithSingleArrayBoundsDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Private ReadOnly _variableDeclarator As VariableDeclaratorSyntax

        Public Sub New(variableDeclarator As VariableDeclaratorSyntax)
            _variableDeclarator = variableDeclarator
        End Sub

        Public ReadOnly Property Name As ModifiedIdentifierSyntax
            Get
                Return _variableDeclarator.Names(0)
            End Get
        End Property

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return Name
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return Name.ArrayBounds
            End Get
        End Property

        Public Overrides Function GetCapturedVariables(model As SemanticModel) As ImmutableArray(Of ISymbol)
            ' Edge case, no need to be efficient, currently there can either be no captured variables or just "Me".
            ' Dim a((Function(n) n + 1).Invoke(1), (Function(n) n + 2).Invoke(2)) As Integer
            Return SyntaxUtilities.GetArrayBoundsCapturedVariables(model, Name.ArrayBounds)
        End Function
    End Class
End Namespace
