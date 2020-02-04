' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.UseIsNullCheck

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNullCheck
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer
        Inherits AbstractUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(Of SyntaxKind)

        Public Sub New()
            MyBase.New(VBFeaturesResources.Use_Is_Nothing_check)
        End Sub

        Protected Overrides Function IsLanguageVersionSupported(options As ParseOptions) As Boolean
            Return True
        End Function

        Protected Overrides Function GetInvocationExpressionKind() As SyntaxKind
            Return SyntaxKind.InvocationExpression
        End Function

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function
    End Class
End Namespace
