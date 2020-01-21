' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.OrderModifiers
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.OrderModifiers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicOrderModifiersDiagnosticAnalyzer
        Inherits AbstractOrderModifiersDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFactsService.Instance,
                       VisualBasicCodeStyleOptions.PreferredModifierOrder,
                       VisualBasicOrderModifiersHelper.Instance,
                       LanguageNames.VisualBasic)
        End Sub

        Protected Overrides Sub Recurse(
            context As SyntaxTreeAnalysisContext,
            preferredOrder As Dictionary(Of Integer, Integer),
            severity As ReportDiagnostic,
            root As SyntaxNode)

            For Each child In root.ChildNodesAndTokens()
                If child.IsNode Then
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

        Private Function ShouldCheck(statement As DeclarationStatementSyntax) As Boolean
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
