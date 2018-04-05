' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ValidateJsonString
Imports Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateEmbeddedLanguageString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateEmbeddedLanguageStringDiagnosticAnalyzer
        Inherits AbstractValidateEmbeddedLanguageStringDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(VisualBasicEmbeddedLanguageProvider.Instance)
        End Sub
    End Class
End Namespace
