Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.Wrapping.InitializerExpression

Friend MustInherit Class AbstractVisualBasicInitializerExpression(
    Of TListSyntax As SyntaxNode, TListItemSyntax As SyntaxNode)
    Inherits AbstractInitializerExpression(Of TListSyntax, TListItemSyntax)

    Protected Overrides ReadOnly Property Indent_all_items As String = FeaturesResources.Indent_all_arguments
    Protected Overrides ReadOnly Property Unwrap_all_items As String = FeaturesResources.Unwrap_all_arguments
    Protected Overrides ReadOnly Property Unwrap_list As String = FeaturesResources.Unwrap_element_list
    Protected Overrides ReadOnly Property Wrap_every_item As String = FeaturesResources.Wrap_every_element
    Protected Overrides ReadOnly Property Wrap_long_list As String = FeaturesResources.Wrap_long_element_list
    Protected Overrides ReadOnly Property DoWrapInitializerOpenBrace As Boolean = False

    Protected Sub New()
        MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance)
    End Sub
End Class
