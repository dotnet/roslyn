' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class GetTypeExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_type_name_to_return_a_System_Type_object_for
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.typeName
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 1
            End Get
        End Property

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.Returns_a_System_Type_object_for_the_specified_type_name
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {
                    New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "GetType"),
                    New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")
                }
            End Get
        End Property

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function TryGetTypeNameParameter(syntaxNode As SyntaxNode, index As Integer) As TypeSyntax
            Dim getTypeExpression = TryCast(syntaxNode, GetTypeExpressionSyntax)

            If getTypeExpression IsNot Nothing Then
                Return getTypeExpression.Type
            Else
                Return Nothing
            End If
        End Function

        Public Overrides ReadOnly Property ReturnTypeMetadataName As String
            Get
                Return "System.Type"
            End Get
        End Property
    End Class
End Namespace
