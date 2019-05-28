' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics

Module AnalyzerVerifier
    Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New})() As AnalyzerVerifier(Of TAnalyzer)
        Return New AnalyzerVerifier(Of TAnalyzer)
    End Function
End Module
