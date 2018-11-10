' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Editor.Wrapping
Imports Microsoft.CodeAnalysis.Formatting.Rules

Namespace Microsoft.CodeAnalysis.VisualBasic.Editor.Wrapping
    Friend MustInherit Class AbstractVisualBasicWrappingCodeRefactoringProvider(
        Of TListSyntax As SyntaxNode, TListItemSyntax As SyntaxNode)
        Inherits AbstractWrappingCodeRefactoringProvider(Of TListSyntax, TListItemSyntax)

        Protected Overrides Function GetIndentationService() As ISynchronousIndentationService
            Return New IndentationService()
        End Function

        Private Class IndentationService
            Inherits VisualBasicIndentationService

            Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
                Return New NoOpFormattingRule()
            End Function
        End Class
    End Class
End Namespace
