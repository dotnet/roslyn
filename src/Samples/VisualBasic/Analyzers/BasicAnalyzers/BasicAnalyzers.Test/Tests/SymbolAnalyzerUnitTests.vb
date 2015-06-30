' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class SymbolAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class BadOne
	Public Sub BadOne()
	End Sub
End Class

Class GoodOne
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.SymbolAnalyzerRuleId,
                .Message = String.Format(My.Resources.SymbolAnalyzerMessageFormat, "BadOne"),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 2, 7)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New SymbolAnalyzer()
        End Function
    End Class
End Namespace
