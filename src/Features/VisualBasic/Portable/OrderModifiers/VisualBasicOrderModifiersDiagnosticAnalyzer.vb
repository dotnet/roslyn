' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.OrderModifiers
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicOrderModifiersDiagnosticAnalyzer
        Inherits AbstractOrderModifiersDiagnosticAnalyzer

        Public Sub New()
            MyBase.new(VisualBasicCodeStyleOptions.PreferredModifierOrder, VisualBasicOrderModifiersHelper.Instance)
        End Sub

        Protected Overrides Function GetModifiers(node As SyntaxNode) As SyntaxTokenList
            Return node.GetModifiers()
        End Function

        Protected Overrides Sub Recurse(
            context As SyntaxTreeAnalysisContext,
            preferredOrder As Dictionary(Of Integer, Integer),
            descriptor As DiagnosticDescriptor,
            root As SyntaxNode)

        End Sub
    End Class
End Namespace