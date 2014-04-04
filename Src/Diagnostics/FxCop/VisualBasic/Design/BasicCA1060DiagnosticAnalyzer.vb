' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ''' <summary>
    ''' CA1060 - Move P/Invokes to native methods class
    ''' </summary>
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(CA1060DiagnosticAnalyzer.RuleId, LanguageNames.VisualBasic)>
    Public Class BasicCA1060DiagnosticAnalyzer
        Inherits CA1060DiagnosticAnalyzer
    End Class
End Namespace