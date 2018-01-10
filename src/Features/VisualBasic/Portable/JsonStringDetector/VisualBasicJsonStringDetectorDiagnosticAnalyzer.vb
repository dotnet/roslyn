' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.JsonStringDetector
Imports Microsoft.CodeAnalysis.VisualBasic.VirtualChars

Namespace Microsoft.CodeAnalysis.VisualBasic.JsonStringDetector
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicJsonStringDetectorDiagnosticAnalyzer
        Inherits AbstractJsonStringDetectorDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(SyntaxKind.StringLiteralToken,
                       VisualBasicSyntaxFactsService.Instance,
                       VisualBasicSemanticFactsService.Instance,
                       VisualBasicVirtualCharService.Instance)
        End Sub
    End Class
End Namespace
