' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class SyntaxTreeAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
	Public Sub M()
	End Sub
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.SyntaxTreeAnalyzerRuleId,
                .Message = String.Format(My.Resources.SyntaxTreeAnalyzerMessageFormat, "Test0.vb"),
                .Severity = DiagnosticSeverity.Warning
            }

            Dim parseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose)
            VerifyBasicDiagnostic(test, parseOptions, compilationOptions:=Nothing)

            parseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.None)
            VerifyBasicDiagnostic(test, parseOptions, Nothing, expected)

            parseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse)
            VerifyBasicDiagnostic(test, parseOptions, Nothing, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New SyntaxTreeAnalyzer()
        End Function
    End Class
End Namespace
