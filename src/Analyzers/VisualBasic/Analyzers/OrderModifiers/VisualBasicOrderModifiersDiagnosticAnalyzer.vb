' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.OrderModifiers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicOrderModifiersDiagnosticAnalyzer
        Inherits AbstractOrderModifiersDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance,
                       VisualBasicCodeStyleOptions.PreferredModifierOrder,
                       VisualBasicOrderModifiersHelper.Instance)
        End Sub

        Protected Overrides Function GetPreferredOrderStyle(context As SyntaxTreeAnalysisContext) As CodeStyleOption2(Of String)
            Return context.GetVisualBasicAnalyzerOptions().PreferredModifierOrder
        End Function

        Protected Overrides Sub Recurse(
            context As SyntaxTreeAnalysisContext,
            preferredOrder As Dictionary(Of Integer, Integer),
            severity As ReportDiagnostic,
            root As SyntaxNode)

            For Each child In root.ChildNodesAndTokens()
                If child.IsNode And context.ShouldAnalyzeSpan(child.Span) Then
                    Dim declarationStatement = TryCast(child.AsNode(), DeclarationStatementSyntax)
                    If declarationStatement IsNot Nothing Then
                        If ShouldCheck(declarationStatement) Then
                            CheckModifiers(context, preferredOrder, severity, declarationStatement)
                        End If

                        Recurse(context, preferredOrder, severity, declarationStatement)
                    End If
                End If
            Next
        End Sub

        Private Shared Function ShouldCheck(statement As DeclarationStatementSyntax) As Boolean
            Dim modifiers = statement.GetModifiers()
            If modifiers.Count >= 2 Then
                ' We'll see modifiers twice in some circumstances.  First, on a VB block
                ' construct, and then on the VB begin statement for that block.  In order
                ' to not double report, only check the statement that the modifier actually
                ' belongs to.
                Return modifiers.First().Parent Is statement
            End If

            Return False
        End Function
    End Class
End Namespace
