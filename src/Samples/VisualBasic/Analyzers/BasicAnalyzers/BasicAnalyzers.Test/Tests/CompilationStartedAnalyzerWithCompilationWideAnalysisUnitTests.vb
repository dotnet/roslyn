' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports TestHelper

Namespace BasicAnalyzers.Test
    <TestClass>
    Public Class CompilationStartedAnalyzerWithCompilationWideAnalysisUnitTests
        Inherits DiagnosticVerifier
        <TestMethod>
        Public Sub Test1()
            Dim test = "
Namespace MyNamespace
	Public Class UnsecureMethodAttribute
		Inherits System.Attribute
	End Class

	Public Interface ISecureType
	End Interface

	Public Interface IUnsecureInterface
		<UnsecureMethodAttribute> _
		Sub F()
	End Interface

	Class MyInterfaceImpl1
		Implements IUnsecureInterface
		Public Sub F() Implements IUnsecureInterface.F
		End Sub
	End Class

	Class MyInterfaceImpl2
		Implements IUnsecureInterface
		Implements ISecureType
		Public Sub F() Implements IUnsecureInterface.F
		End Sub
	End Class

	Class MyInterfaceImpl3
		Implements ISecureType
		Public Sub F()
		End Sub
	End Class
End Namespace"

            Dim expected = New DiagnosticResult() With {
                .Id = DiagnosticIds.CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId,
                .Message = String.Format(My.Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisMessageFormat, "MyInterfaceImpl2", CompilationStartedAnalyzerWithCompilationWideAnalysis.SecureTypeInterfaceName, "IUnsecureInterface"),
                .Severity = DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 21, 8)}
            }

            VerifyBasicDiagnostic(test, expected)
        End Sub

        Protected Overrides Function GetBasicDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New CompilationStartedAnalyzerWithCompilationWideAnalysis()
        End Function
    End Class
End Namespace
