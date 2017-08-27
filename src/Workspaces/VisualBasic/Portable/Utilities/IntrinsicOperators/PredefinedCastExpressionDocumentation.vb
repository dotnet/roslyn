' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class PredefinedCastExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Private ReadOnly _resultingType As ITypeSymbol
        Private ReadOnly _keywordText As String

        Public Sub New(keywordKind As SyntaxKind, compilation As Compilation)
            _resultingType = compilation.GetTypeFromPredefinedCastKeyword(keywordKind)
            _keywordText = SyntaxFacts.GetText(keywordKind)
        End Sub

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, _resultingType.ToDisplayString())
            End Get
        End Property

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_expression_to_be_evaluated_and_converted
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.expression
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 1
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, _keywordText),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")}
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeMetadataName As String
            Get
                Return _resultingType.ContainingNamespace.Name + "." + _resultingType.MetadataName
            End Get
        End Property
    End Class
End Namespace
