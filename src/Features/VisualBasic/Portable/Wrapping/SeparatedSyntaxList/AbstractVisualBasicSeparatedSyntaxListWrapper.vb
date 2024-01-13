' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Indentation
Imports Microsoft.CodeAnalysis.Wrapping
Imports Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
    Partial Friend MustInherit Class AbstractVisualBasicSeparatedSyntaxListWrapper(
        Of TListSyntax As SyntaxNode, TListItemSyntax As SyntaxNode)
        Inherits AbstractSeparatedSyntaxListWrapper(Of TListSyntax, TListItemSyntax)

        Protected Sub New()
            MyBase.New(VisualBasicIndentationService.WithoutParameterAlignmentInstance)
        End Sub

        ' The visual basic language always requires the open brace to be on the same line as the collection
        ' being initialized.
        Protected NotOverridable Overrides Function ShouldMoveOpenBraceToNewLine(options As SyntaxWrappingOptions) As Boolean
            Return False
        End Function
    End Class
End Namespace
