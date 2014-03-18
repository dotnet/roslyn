' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA1012DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA1012DiagnosticAnalyzer
        Inherits CA1012DiagnosticAnalyzer
    End Class
End Namespace