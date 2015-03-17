' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The Diagnostic class allows formatting of Visual Basic diagnostics. 
    ''' </summary>
    Public Class VisualBasicDiagnosticFormatter
        Inherits DiagnosticFormatter

        Private Shared ReadOnly CompilerAnalyzer As String = GetType(VisualBasicCompilerDiagnosticAnalyzer).ToString()

        Friend Sub New(displayAnalyzer As Boolean)
            MyBase.New(displayAnalyzer)
        End Sub

        Friend Overrides Function FormatSourceSpan(span As LinePositionSpan, formatter As IFormatProvider) As String
            Return String.Format("({0}) ", span.Start.Line + 1)
        End Function

        Protected Overrides Function GetAnalyzerIdentity(analyzer As DiagnosticAnalyzer) As String
            If analyzer Is Nothing Then
                Return CompilerAnalyzer
            End If

            Return MyBase.GetAnalyzerIdentity(analyzer)
        End Function

        ''' <summary>
        ''' Gets the current DiagnosticFormatter instance.
        ''' </summary>
        Public Shared Shadows ReadOnly Instance As VisualBasicDiagnosticFormatter = New VisualBasicDiagnosticFormatter(displayAnalyzer:=False)
    End Class
End Namespace
