' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicNamingStyleDiagnosticAnalyzer
        Inherits NamingStyleDiagnosticAnalyzerBase

        Protected NotOverridable Overrides Sub OnCompilationStartAction(
                context As CompilationStartAnalysisContext,
                idToCachedResult As ConcurrentDictionary(Of Guid, ConcurrentDictionary(Of String, String)))
            ' VB doesn't have local functions, so there's nothing to do here.
        End Sub
    End Class
End Namespace
