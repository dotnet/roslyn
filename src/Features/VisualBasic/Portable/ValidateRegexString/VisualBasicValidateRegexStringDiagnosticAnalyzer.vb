' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.RegularExpressions
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.RegularExpressions
Imports Microsoft.CodeAnalysis.ValidateRegexString

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateRegexString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateRegexStringDiagnosticAnalyzer
        Inherits AbstractValidateRegexStringDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken)
        End Sub

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetSemanticFactsService() As ISemanticFactsService
            Return VisualBasicSemanticFactsService.Instance
        End Function

        Protected Overrides Function GetVirtualCharService() As IVirtualCharService
            Return VisualBasicVirtualCharService.Instance
        End Function
    End Class
End Namespace
