' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class MidAssignmentDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string
            End Get
        End Property

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_name_of_the_string_variable_to_modify
                Case 1
                    Return VBWorkspaceResources.The_one_based_character_position_in_the_string_where_the_replacement_of_text_begins
                Case 2
                    Return VBWorkspaceResources.The_number_of_characters_to_replace_If_omitted_the_length_of_stringExpression_is_used
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.stringName
                Case 1
                    Return VBWorkspaceResources.startIndex
                Case 2
                    Return VBWorkspaceResources.length
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterDisplayParts(index As Integer) As IList(Of SymbolDisplayPart)
            If index = 2 Then
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, "[" + GetParameterName(2) + "]")}
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

        Public Overrides Function GetSuffix(semanticModel As SemanticModel, position As Integer, nodeToBind As SyntaxNode, cancellationToken As CancellationToken) As IList(Of SymbolDisplayPart)
            Return {
                New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, ")"),
                New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                New SymbolDisplayPart(SymbolDisplayPartKind.Operator, Nothing, "="),
                New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                New SymbolDisplayPart(SymbolDisplayPartKind.Text, Nothing, VBWorkspaceResources.stringExpression)
            }
        End Function

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "Mid"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")}
            End Get
        End Property
    End Class
End Namespace
