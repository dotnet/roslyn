' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ValidateJsonString
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateRegexString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateJsonStringDiagnosticAnalyzer
        Inherits AbstractValidateJsonStringDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken,
                       VisualBasicSyntaxFactsService.Instance,
                       VisualBasicSemanticFactsService.Instance,
                       VisualBasicVirtualCharService.Instance)
        End Sub
    End Class
End Namespace
