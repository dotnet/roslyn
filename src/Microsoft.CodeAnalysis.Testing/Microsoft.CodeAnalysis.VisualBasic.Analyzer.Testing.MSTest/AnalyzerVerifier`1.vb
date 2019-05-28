' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, MSTestVerifier)
End Class
