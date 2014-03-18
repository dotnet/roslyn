' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    ''' <summary>
    ''' CA1052 - Static holder types should be sealed
    ''' </summary>
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(StaticTypeRulesDiagnosticAnalyzer.RuleNameForExportAttribute, LanguageNames.VisualBasic)>
    Public Class BasicStaticTypeRulesDiagnosticAnalyzer
        Inherits StaticTypeRulesDiagnosticAnalyzer
    End Class
End Namespace