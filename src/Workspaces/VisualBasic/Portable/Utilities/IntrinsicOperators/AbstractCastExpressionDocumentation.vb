' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend MustInherit Class AbstractCastExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.ExpressionToConvert
                Case 1
                    Return VBWorkspaceResources.NameOfTypeToConvert
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.Expression1
                Case 1
                    Return VBWorkspaceResources.Typename
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
