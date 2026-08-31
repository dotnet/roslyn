' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Class ReferenceRewriter
    Inherits VisualBasicSyntaxRewriter

    Private ReadOnly _name As String
    Private ReadOnly _symbol As ISymbol
    Private ReadOnly _semanticModel As SemanticModel

    Public Sub New(name As String, symbol As ISymbol, semanticModel As SemanticModel)
        _name = name
        _symbol = symbol
        _semanticModel = semanticModel
    End Sub

    Public Overrides Function VisitIdentifierName(identifierName As IdentifierNameSyntax) As SyntaxNode
        If identifierName.Identifier.ValueText = _symbol.Name Then
            Dim identifierSymbol = _semanticModel.GetSymbolInfo(identifierName).Symbol
            If identifierSymbol IsNot Nothing AndAlso identifierSymbol.Equals(_symbol) Then
                identifierName = identifierName.WithIdentifier(
                    SyntaxFactory.Identifier(_name))

                Return identifierName.WithAdditionalAnnotations(Formatter.Annotation)
            End If
        End If

        Return identifierName
    End Function

End Class
