' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Naming

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Naming
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA1715DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA1715DiagnosticAnalyzer
        Inherits CA1715DiagnosticAnalyzer
    End Class
End Namespace