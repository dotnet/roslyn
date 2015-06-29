' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class CompilationAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
    Public Sub M()
    End Sub
End Class"
            Dim expected = {New DiagnosticResult() With {
                .Id = DiagnosticIds.CompilationAnalyzerRuleId,
                .Message = String.Format(My.Resources.CompilationAnalyzerMessageFormat, DiagnosticIds.SymbolAnalyzerRuleId),
                .Severity = DiagnosticSeverity.Warning
            }}

            Dim specificOption = New KeyValuePair(Of String, ReportDiagnostic)(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Error)

            Dim compilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, specificDiagnosticOptions:={specificOption})
            VerifyBasicDiagnostic(test, parseOptions:=Nothing, compilationOptions:=compilationOptions)

            specificOption = New KeyValuePair(Of String, ReportDiagnostic)(DiagnosticIds.SymbolAnalyzerRuleId, ReportDiagnostic.Suppress)
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions({specificOption})
            VerifyBasicDiagnostic(test, Nothing, compilationOptions, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New CompilationAnalyzer()
        End Function
    End Class
End Namespace
