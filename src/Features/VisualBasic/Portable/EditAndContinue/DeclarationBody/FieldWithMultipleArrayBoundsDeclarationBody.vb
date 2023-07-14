' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' A field that's part of declaration with multiple identifiers with array bounds.
    ''' 
    ''' Dim [|a(n)|], [|b(n)|] As Integer
    ''' </summary>
    Friend NotInheritable Class FieldWithMultipleArrayBoundsDeclarationBody
        Inherits FieldOrPropertyDeclarationBody

        Private ReadOnly _identifier As ModifiedIdentifierSyntax

        Public Sub New(identifier As ModifiedIdentifierSyntax)
            _identifier = identifier
        End Sub

        Public Overrides ReadOnly Property InitializerActiveStatement As SyntaxNode
            Get
                Return _identifier
            End Get
        End Property

        Public Overrides ReadOnly Property OtherActiveStatementContainer As SyntaxNode
            Get
                Return _identifier.ArrayBounds
            End Get
        End Property

        Public Overrides Function GetCapturedVariables(model As SemanticModel) As ImmutableArray(Of ISymbol)
            ' Edge case, no need to be efficient, currently there can either be no captured variables or just "Me".
            ' Dim a((Function(n) n + 1).Invoke(1), (Function(n) n + 2).Invoke(2)) As Integer
            Return SyntaxUtilities.GetArrayBoundsCapturedVariables(model, _identifier.ArrayBounds)
        End Function
    End Class
End Namespace
