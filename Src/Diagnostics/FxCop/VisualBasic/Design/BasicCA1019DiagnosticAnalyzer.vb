' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Design

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Design
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1019DiagnosticAnalyzer
        Inherits CA1019DiagnosticAnalyzer

        Protected Overrides Function IsAssignableTo(fromSymbol As ITypeSymbol, toSymbol As ITypeSymbol, compilation As Compilation) As Boolean
            Return fromSymbol IsNot Nothing AndAlso toSymbol IsNot Nothing AndAlso DirectCast(compilation, VisualBasicCompilation).ClassifyConversion(fromSymbol, toSymbol).IsWidening
        End Function
    End Class
End Namespace