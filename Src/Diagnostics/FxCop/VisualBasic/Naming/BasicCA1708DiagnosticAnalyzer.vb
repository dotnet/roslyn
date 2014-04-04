' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Naming

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Naming
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA1708DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA1708DiagnosticAnalyzer
        Inherits CA1708DiagnosticAnalyzer
    End Class
End Namespace