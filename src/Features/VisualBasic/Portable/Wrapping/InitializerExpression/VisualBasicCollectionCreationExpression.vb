Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Friend Class VisualBasicCollectionCreationExpression
    Inherits AbstractVisualBasicInitializerExpression(Of CollectionInitializerSyntax, ExpressionSyntax)

    Protected Overrides Function GetListItems(listSyntax As CollectionInitializerSyntax) As SeparatedSyntaxList(Of ExpressionSyntax)
        Return listSyntax.Initializers
    End Function

    Protected Overrides Function TryGetApplicableList(node As SyntaxNode) As CollectionInitializerSyntax
        Return If(TryCast(node, ArrayCreationExpressionSyntax)?.Initializer,
                  TryCast(node, ObjectCollectionInitializerSyntax)?.Initializer)
    End Function

    Protected Overrides Function PositionIsApplicable(
            root As SyntaxNode, position As Integer,
            declaration As SyntaxNode, listSyntax As CollectionInitializerSyntax) As Boolean

        Dim startToken = listSyntax.GetFirstToken()

        ' allow anywhere in the arg list, as long we don't end up walking through something
        ' complex Like a lambda/anonymous function.
        Dim token = root.FindToken(position)
        If token.Parent.Ancestors().Contains(listSyntax) Then
            Dim current = token.Parent
            While current IsNot listSyntax
                If VisualBasicSyntaxFactsService.Instance.IsAnonymousFunction(current) Then
                    Return False
                End If

                current = current.Parent
            End While
        End If

        Return True
    End Function
End Class
