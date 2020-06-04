' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryPragmaSuppressionsDiagnosticAnalyzer
        Inherits AbstractRemoveUnnecessaryPragmaSuppressionsDiagnosticAnalyzer

        Protected Overrides ReadOnly Property CompilerErrorCodePrefix As String
            Get
                Return "BC"
            End Get
        End Property

        Protected Overrides ReadOnly Property CompilerErrorCodeDigitCount As Integer
            Get
                Return 5
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides Function GetCompilerDiagnosticAnalyzerInfo() As (assembly As Assembly, typeName As String)
            Return (GetType(SyntaxKind).Assembly, CompilerDiagnosticAnalyzerNames.VisualBasicCompilerAnalyzerTypeName)
        End Function
    End Class
End Namespace
