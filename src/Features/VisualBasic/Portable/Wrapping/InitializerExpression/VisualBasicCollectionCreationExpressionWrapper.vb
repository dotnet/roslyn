' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Wrapping.InitializerExpression

Friend Class VisualBasicCollectionCreationExpressionWrapper
    Inherits AbstractInitializerExpressionWrapper(Of CollectionInitializerSyntax, ExpressionSyntax)

    Public Sub New()
        MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance)
    End Sub

    Protected Overrides ReadOnly Property DoWrapInitializerOpenBrace As Boolean = False

    Protected Overrides Function GetListItems(listSyntax As CollectionInitializerSyntax) As SeparatedSyntaxList(Of ExpressionSyntax)
        Return listSyntax.Initializers
    End Function

    Protected Overrides Function TryGetApplicableList(node As SyntaxNode) As CollectionInitializerSyntax
        Return If(TryCast(node, ArrayCreationExpressionSyntax)?.Initializer,
                  TryCast(node, ObjectCollectionInitializerSyntax)?.Initializer)
    End Function
End Class
