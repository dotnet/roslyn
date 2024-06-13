' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

<Obsolete(ObsoleteMessages.FrameworkPackages)>
Module CodeFixVerifier
    Public Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})() As CodeFixVerifier(Of TAnalyzer, TCodeFix)
        Return New CodeFixVerifier(Of TAnalyzer, TCodeFix)
    End Function
End Module
