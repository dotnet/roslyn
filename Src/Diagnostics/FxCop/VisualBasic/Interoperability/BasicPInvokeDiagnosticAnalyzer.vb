' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Interoperability

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Interoperability
    <DiagnosticAnalyzer>
    <ExportDiagnosticAnalyzer(PInvokeDiagnosticAnalyzer.PInvokeInteroperabilityRuleName, LanguageNames.VisualBasic)>
    Public Class BasicPInvokeDiagnosticAnalyzer
        Inherits PInvokeDiagnosticAnalyzer
    End Class
End Namespace
