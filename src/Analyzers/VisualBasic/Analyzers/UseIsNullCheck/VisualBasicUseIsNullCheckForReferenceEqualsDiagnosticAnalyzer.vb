' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.UseIsNullCheck
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer
        Inherits AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(Of SyntaxKind)

        Public Sub New()
            MyBase.New(VisualBasicAnalyzersResources.Use_Is_Nothing_check)
        End Sub

        Protected Overrides Function IsLanguageVersionSupported(compilation As Compilation) As Boolean
            Return True
        End Function

        Protected Overrides Function IsUnconstrainedGenericSupported(compilation As Compilation) As Boolean
            Return True
        End Function

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function
    End Class
End Namespace
