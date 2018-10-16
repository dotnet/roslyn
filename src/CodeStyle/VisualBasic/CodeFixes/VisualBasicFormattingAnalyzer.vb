' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.CodeStyle
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicFormattingAnalyzer
        Inherits AbstractFormattingAnalyzer

        Protected Overrides Function GetAnalyzerImplType() As Type
            ' Explained at the call site in the base class
            Return GetType(VisualBasicFormattingAnalyzerImpl)
        End Function
    End Class
End Namespace
