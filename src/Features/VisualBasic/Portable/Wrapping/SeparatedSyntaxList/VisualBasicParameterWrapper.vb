' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
    Partial Friend Class VisualBasicParameterWrapper
        Inherits AbstractVisualBasicSeparatedSyntaxListWrapper(Of ParameterListSyntax, ParameterSyntax)

        Protected Overrides ReadOnly Property Align_wrapped_items As String = FeaturesResources.Align_wrapped_parameters
        Protected Overrides ReadOnly Property Indent_all_items As String = FeaturesResources.Indent_all_parameters
        Protected Overrides ReadOnly Property Indent_wrapped_items As String = FeaturesResources.Indent_wrapped_parameters
        Protected Overrides ReadOnly Property Unwrap_all_items As String = FeaturesResources.Unwrap_all_parameters
        Protected Overrides ReadOnly Property Unwrap_and_indent_all_items As String = FeaturesResources.Unwrap_and_indent_all_parameters
        Protected Overrides ReadOnly Property Unwrap_list As String = FeaturesResources.Unwrap_parameter_list
        Protected Overrides ReadOnly Property Wrap_every_item As String = FeaturesResources.Wrap_every_parameter
        Protected Overrides ReadOnly Property Wrap_long_list As String = FeaturesResources.Wrap_long_parameter_list

        Protected Overrides Function GetListItems(listSyntax As ParameterListSyntax) As SeparatedSyntaxList(Of ParameterSyntax)
            Return listSyntax.Parameters
        End Function

        Protected Overrides Function TryGetApplicableList(node As SyntaxNode) As ParameterListSyntax
            Return VisualBasicSyntaxGenerator.GetParameterList(node)
        End Function

        Protected Overrides Function PositionIsApplicable(
                root As SyntaxNode, position As Integer,
                declaration As SyntaxNode, listSyntax As ParameterListSyntax) As Boolean

            Dim generator = VisualBasicSyntaxGenerator.Instance
            Dim attributes = generator.GetAttributes(declaration)

            ' We want to offer this feature in the header of the member.  For now, we consider
            ' the header to be the part after the attributes, to the end of the parameter list.
            Dim firstToken = If(attributes?.Count > 0,
                attributes.Last().GetLastToken().GetNextToken(),
                declaration.GetFirstToken())

            Dim lastToken = listSyntax.GetLastToken()

            Dim headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End)
            Return headerSpan.IntersectsWith(position)
        End Function
    End Class
End Namespace
