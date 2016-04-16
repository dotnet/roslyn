' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class MidAssignmentDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.ReplacesChars
            End Get
        End Property

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.NameOfStringVariable
                Case 1
                    Return VBWorkspaceResources.OneBasedStartPos
                Case 2
                    Return VBWorkspaceResources.NumberOfCharsToReplace
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.StringName
                Case 1
                    Return VBWorkspaceResources.StartIndex
                Case 2
                    Return VBWorkspaceResources.Length
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterDisplayParts(index As Integer) As IEnumerable(Of SymbolDisplayPart)
            If index = 2 Then
                Return SpecializedCollections.SingletonEnumerable(New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, "[" + GetParameterName(2) + "]"))
            Else
                Return MyBase.GetParameterDisplayParts(index)
            End If
        End Function

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 3
            End Get
        End Property

        Public Overrides Function GetSuffix(semanticModel As SemanticModel, position As Integer, nodeToBind As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SymbolDisplayPart)
            Return {
                New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, ")"),
                New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                New SymbolDisplayPart(SymbolDisplayPartKind.Operator, Nothing, "="),
                New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, VBWorkspaceResources.StringExpression)
            }
        End Function

        Public Overrides ReadOnly Property PrefixParts As IEnumerable(Of SymbolDisplayPart)
            Get
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "Mid"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")}
            End Get
        End Property
    End Class
End Namespace
