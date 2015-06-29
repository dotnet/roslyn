' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class SyntaxNodeAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
	Public Sub M()
		Dim implicitTypedLocal = 0
		Dim explicitTypedLocal As Integer = 1
	End Sub
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.SyntaxNodeAnalyzerRuleId,
                .Message = String.Format(My.Resources.SyntaxNodeAnalyzerMessageFormat, "implicitTypedLocal"),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 4, 7)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New SyntaxNodeAnalyzer()
        End Function
    End Class
End Namespace
