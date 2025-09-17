' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators
    Friend NotInheritable Class BinaryConditionalExpressionDocumentation
        Inherits AbstractIntrinsicOperatorDocumentation

        Public Overrides ReadOnly Property DocumentationText As String =
            VBWorkspaceResources.If_expression_evaluates_to_a_reference_or_Nullable_value_that_is_not_Nothing_the_function_returns_that_value_Otherwise_it_calculates_and_returns_expressionIfNothing

        Public Overrides Function GetParameterDocumentation(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.Returned_if_it_evaluates_to_a_reference_or_nullable_type_that_is_not_Nothing
                Case 1
                    Return VBWorkspaceResources.Evaluated_and_returned_if_expression_evaluates_to_Nothing
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides Function GetParameterName(index As Integer) As String
            Select Case index
                Case 0
                    Return VBWorkspaceResources.expression
                Case 1
                    Return VBWorkspaceResources.expressionIfNothing
                Case Else
                    Throw New ArgumentException(NameOf(index))
            End Select
        End Function

        Public Overrides ReadOnly Property IncludeAsType As Boolean = True

        Public Overrides ReadOnly Property ParameterCount As Integer = 2

        Public Overrides ReadOnly Property PrefixParts As ImmutableArray(Of SymbolDisplayPart) = ImmutableArray.Create(
            New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, "If"),
            New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))
    End Class
End Namespace
