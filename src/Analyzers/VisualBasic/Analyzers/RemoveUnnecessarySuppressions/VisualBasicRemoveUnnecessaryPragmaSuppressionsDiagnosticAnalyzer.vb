' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessarySuppressions

#If Not CODE_STYLE Then ' Not exported in CodeStyle layer: https://github.com/dotnet/roslyn/issues/47942
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
#Else
    Friend NotInheritable Class VisualBasicRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer
#End If
        Inherits AbstractRemoveUnnecessaryInlineSuppressionsDiagnosticAnalyzer

        Protected Overrides ReadOnly Property CompilerErrorCodePrefix As String = "BC"

        Protected Overrides ReadOnly Property CompilerErrorCodeDigitCount As Integer = 5

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides ReadOnly Property SemanticFacts As ISemanticFacts = VisualBasicSemanticFacts.Instance

        Protected Overrides Function GetCompilerDiagnosticAnalyzerInfo() As (assembly As Assembly, typeName As String)
            Return (GetType(SyntaxKind).Assembly, CompilerDiagnosticAnalyzerNames.VisualBasicCompilerAnalyzerTypeName)
        End Function
    End Class
End Namespace
