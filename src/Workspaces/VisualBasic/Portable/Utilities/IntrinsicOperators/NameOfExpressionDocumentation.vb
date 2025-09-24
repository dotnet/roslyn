' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class NameOfExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Produces_a_string_for_the_name_of_the_specified_type_or_member
            End Get
        End Property

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
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "NameOf"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")}
            End Get
        End Property

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_type_of_member_to_return_the_name_of
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.typeOrMember
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property ReturnTypeMetadataName As String
            Get
                Return "System.String"
            End Get
        End Property

    End Class
End Namespace
