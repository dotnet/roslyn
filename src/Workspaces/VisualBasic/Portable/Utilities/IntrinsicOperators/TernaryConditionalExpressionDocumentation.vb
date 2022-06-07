' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class TernaryConditionalExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides ReadOnly Property DocumentationText As String
            Get
                Return VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse
            End Get
        End Property

        Public Overrides Function GetParameterDisplayParts(index As Integer) As IList(Of SymbolDisplayPart)
            If index = 0 Then
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, GetParameterName(index)),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "As"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Space, Nothing, " "),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "Boolean")}
            Else
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, Nothing, GetParameterName(index))}
            End If
        End Function

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.The_expression_to_evaluate
                Case 1
                    Return VBWorkspaceResources.Evaluated_and_returned_if_condition_evaluates_to_True
                Case 2
                    Return VBWorkspaceResources.Evaluated_and_returned_if_condition_evaluates_to_False
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.condition
                Case 1
                    Return VBWorkspaceResources.expressionIfTrue
                Case 2
                    Return VBWorkspaceResources.expressionIfFalse
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
                Return 3
            End Get
        End Property

        Public Overrides ReadOnly Property PrefixParts As IList(Of SymbolDisplayPart)
            Get
                Return {New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "If"),
                        New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "(")}
            End Get
        End Property
    End Class
End Namespace
