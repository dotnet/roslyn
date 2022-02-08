' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend MustInherit Class AbstractCastExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_expression_to_be_evaluated_and_converted
                Case 1
                    Return VBWorkspaceResources.The_name_of_the_data_type_to_which_the_value_of_expression_will_be_converted
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.expression
                Case 1
                    Return VBWorkspaceResources.typeName
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property IncludeAsType As Boolean
            Get
                Return True
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 2
            End Get
        End Property

        Public Overrides Function TryGetTypeNameParameter(syntaxNode As SyntaxNode, index As Integer) As TypeSyntax
            Dim castExpression = TryCast(syntaxNode, CastExpressionSyntax)

            If castExpression IsNot Nothing AndAlso index = 1 Then
                Return castExpression.Type
            Else
                Return Nothing
            End If
        End Function
    End Class
End Namespace
