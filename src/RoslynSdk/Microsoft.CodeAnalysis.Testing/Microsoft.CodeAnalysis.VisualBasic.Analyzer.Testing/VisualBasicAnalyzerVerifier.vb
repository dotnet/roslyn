' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicAnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TVerifier As {IVerifier, New})
    Inherits AnalyzerVerifier(Of TAnalyzer, VisualBasicAnalyzerTest(Of TAnalyzer, TVerifier), TVerifier)
End Class
