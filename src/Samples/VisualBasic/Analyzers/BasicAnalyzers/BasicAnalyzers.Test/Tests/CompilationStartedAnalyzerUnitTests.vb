Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class CompilationStartedAnalyzerUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Namespace MyInterfaces
	Public Interface [Interface]
	End Interface

	Class MyInterfaceImpl
		Implements [Interface]
	End Class

	Class MyInterfaceImpl2
		Implements [Interface]
	End Class
End Namespace"
            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.CompilationStartedAnalyzerRuleId,
                .Message = String.Format(My.Resources.CompilationStartedAnalyzerMessageFormat, "MyInterfaceImpl2", CompilationStartedAnalyzer.DontInheritInterfaceTypeName),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 10, 8)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New CompilationStartedAnalyzer()
        End Function
    End Class
End Namespace
