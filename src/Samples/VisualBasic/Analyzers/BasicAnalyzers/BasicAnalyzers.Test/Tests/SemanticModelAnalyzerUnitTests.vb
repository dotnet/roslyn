' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class SemanticModelAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Class C
    Public Async Function M() As Integer
    End Function
End Class"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.SemanticModelAnalyzerRuleId,
                .Message = String.Format(My.Resources.SemanticModelAnalyzerMessageFormat, "Test0.vb", 1),
                .Severity = DiagnosticSeverity.Warning
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New SemanticModelAnalyzer()
        End Function
    End Class
End Namespace
