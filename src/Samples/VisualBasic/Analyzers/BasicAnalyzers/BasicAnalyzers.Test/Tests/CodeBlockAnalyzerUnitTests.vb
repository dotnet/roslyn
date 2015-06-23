' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class CodeBlockAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
	Public Sub M1()
	End Sub

	Public Overridable Sub M2()
	End Sub

	Public Function M3() As Integer
	End Function
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.CodeBlockAnalyzerRuleId,
                .Message = String.Format(My.Resources.CodeBlockAnalyzerMessageFormat, "M1"),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 3, 13)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New CodeBlockAnalyzer()
        End Function
    End Class
End Namespace
