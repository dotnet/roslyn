' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class CodeBlockStartedAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
	Public Function M1(p1 As Integer, p2 As Integer) As Integer
		Return M2(p1, p1)
	End Function

	Public Function M2(p1 As Integer, p2 As Integer) As Integer
		Return p1 + p2
	End Function
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.CodeBlockStartedAnalyzerRuleId,
                .Message = String.Format(My.Resources.CodeBlockStartedAnalyzerMessageFormat, "p2", "M1"),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 3, 36)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New CodeBlockStartedAnalyzer()
        End Function
    End Class
End Namespace
