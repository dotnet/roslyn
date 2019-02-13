' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Editor.Wrapping.SeparatedSyntaxList
Imports Microsoft.CodeAnalysis.Formatting.Rules

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Wrapping.SeparatedSyntaxList
    Friend MustInherit Class AbstractVisualBasicSeparatedSyntaxListWrapper(
        Of TListSyntax As SyntaxNode, TListItemSyntax As SyntaxNode)
        Inherits AbstractSeparatedSyntaxListWrapper(Of TListSyntax, TListItemSyntax)

        Protected Overrides Function GetIndentationService() As IBlankLineIndentationService
            Return New IndentationService()
        End Function

        Private Class IndentationService
            Inherits VisualBasicIndentationService

            Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
                ' Override default indentation behavior.  The special indentation rule tries to 
                ' align parameters.  But that's what we're actually trying to control, so we need
                ' to remove this.
                Return New NoOpFormattingRule()
            End Function
        End Class
    End Class
End Namespace
