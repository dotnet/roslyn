' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA2231DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA2231DiagnosticAnalyzer
        Inherits CA2231DiagnosticAnalyzer
    End Class
End Namespace