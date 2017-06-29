' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Roslyn.Utilities
Imports Microsoft.CodeAnalysis.OrderModifiers

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicOrderModifiersCodeFixProvider
        Inherits AbstractOrderModifiersCodeFixProvider

        Public Sub New()
            MyBase.new(VisualBasicCodeStyleOptions.PreferredModifierOrder, VisualBasicOrderModifiersHelper.Instance)
        End Sub

        Protected Overrides Function TokenList(tokens As IEnumerable(Of SyntaxToken)) As SyntaxTokenList
            Return SyntaxFactory.TokenList(tokens)
        End Function

        Protected Overrides Function GetModifiers(node As SyntaxNode) As SyntaxTokenList
            Return node.GetModifiers()
        End Function

        Protected Overrides Function WithModifiers(node As SyntaxNode, modifiers As SyntaxTokenList) As SyntaxNode
            Return node.WithModifiers(modifiers)
        End Function
    End Class
End Namespace