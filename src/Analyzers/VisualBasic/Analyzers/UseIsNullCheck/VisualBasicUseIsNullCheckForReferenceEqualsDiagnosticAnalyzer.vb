' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseIsNullCheck
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer
        Inherits AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(Of SyntaxKind)

        Public Sub New()
            MyBase.New(VisualBasicAnalyzersResources.Use_Is_Nothing_check)
        End Sub

        Protected Overrides Function IsLanguageVersionSupported(options As ParseOptions) As Boolean
            Return True
        End Function

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function
    End Class
End Namespace
