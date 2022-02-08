' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
    Friend Class VisualBasicCollectionCreationExpressionWrapper
        Inherits AbstractVisualBasicSeparatedSyntaxListWrapper(Of CollectionInitializerSyntax, ExpressionSyntax)

        Protected Overrides ReadOnly Property Align_wrapped_items As String = FeaturesResources.Align_wrapped_arguments
        Protected Overrides ReadOnly Property Indent_all_items As String = FeaturesResources.Indent_all_arguments
        Protected Overrides ReadOnly Property Indent_wrapped_items As String = FeaturesResources.Indent_wrapped_arguments
        Protected Overrides ReadOnly Property Unwrap_all_items As String = FeaturesResources.Unwrap_all_arguments
        Protected Overrides ReadOnly Property Unwrap_and_indent_all_items As String = FeaturesResources.Unwrap_and_indent_all_arguments
        Protected Overrides ReadOnly Property Unwrap_list As String = FeaturesResources.Unwrap_argument_list
        Protected Overrides ReadOnly Property Wrap_every_item As String = FeaturesResources.Wrap_every_argument
        Protected Overrides ReadOnly Property Wrap_long_list As String = FeaturesResources.Wrap_long_argument_list

        Public Overrides ReadOnly Property Supports_UnwrapGroup_WrapFirst_IndentRest As Boolean = False
        Public Overrides ReadOnly Property Supports_WrapEveryGroup_UnwrapFirst As Boolean = False
        Public Overrides ReadOnly Property Supports_WrapLongGroup_UnwrapFirst As Boolean = False

        Protected Overrides ReadOnly Property ShouldMoveCloseBraceToNewLine As Boolean = True

        Protected Overrides Function GetListItems(listSyntax As CollectionInitializerSyntax) As SeparatedSyntaxList(Of ExpressionSyntax)
            Return listSyntax.Initializers
        End Function

        Protected Overrides Function TryGetApplicableList(node As SyntaxNode) As CollectionInitializerSyntax
            Return If(TryCast(node, ArrayCreationExpressionSyntax)?.Initializer,
                      TryCast(node, ObjectCollectionInitializerSyntax)?.Initializer)
        End Function

        Protected Overrides Function PositionIsApplicable(root As SyntaxNode, position As Integer, declaration As SyntaxNode, listSyntax As CollectionInitializerSyntax) As Boolean
            Return listSyntax.Span.Contains(position)
        End Function
    End Class
End Namespace
