' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Performance

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Performance
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA1813DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA1813DiagnosticAnalyzer
        Inherits CA1813DiagnosticAnalyzer
    End Class
End Namespace