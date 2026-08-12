' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicAnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TVerifier As {IVerifier, New})
    Inherits AnalyzerVerifier(Of TAnalyzer, VisualBasicAnalyzerTest(Of TAnalyzer, TVerifier), TVerifier)
End Class
