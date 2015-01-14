' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers
Imports Roslyn.Diagnostics.Analyzers.Documentation

Namespace Roslyn.Diagnostics.CodeFixes.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDoNotUseVerbatimCrefsAnalyzer
        Inherits DoNotUseVerbatimCrefsAnalyzer

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeXmlAttribute, SyntaxKind.XmlAttribute)
        End Sub

        Private Sub AnalyzeXmlAttribute(context As SyntaxNodeAnalysisContext)
            Dim node = DirectCast(context.Node, XmlAttributeSyntax)

            If DirectCast(node.Name, XmlNameSyntax).LocalName.Text = "cref" Then
                Dim value = TryCast(node.Value, XmlStringSyntax)

                If value IsNot Nothing Then
                    ProcessAttribute(context, value.TextTokens)
                End If
            End If
        End Sub
    End Class
End Namespace
