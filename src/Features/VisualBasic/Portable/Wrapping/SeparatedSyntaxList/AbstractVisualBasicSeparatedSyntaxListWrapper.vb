' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
    Partial Friend MustInherit Class AbstractVisualBasicSeparatedSyntaxListWrapper(
        Of TListSyntax As SyntaxNode, TListItemSyntax As SyntaxNode)
        Inherits AbstractSeparatedSyntaxListWrapper(Of TListSyntax, TListItemSyntax)

        Protected Sub New()
            MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance)
        End Sub
    End Class
End Namespace
