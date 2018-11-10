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

            Public Sub New()
                ' In our scenario we want to control all formatting ourselves. So tell the indenter
                ' to not depend on a formatter being available so it does all the work to figure out 
                ' the indentation itself.
                MyBase.New(formatterAvailable:=False)
            End Sub

            Protected Overrides Function GetSpecializedIndentationFormattingRule() As IFormattingRule
                ' Override default indentation behavior.  The special indentation rule tries to 
                ' align parameters.  But that's what we're actually trying to control, so we need
                ' to remove this.
                Return New NoOpFormattingRule()
            End Function
        End Class
    End Class
End Namespace
