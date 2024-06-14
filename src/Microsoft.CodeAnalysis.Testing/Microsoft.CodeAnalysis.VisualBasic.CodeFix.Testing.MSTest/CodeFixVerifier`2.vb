' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.Testing.Verifiers

<Obsolete(ObsoleteMessages.FrameworkPackages)>
Public Class CodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})
    Inherits VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, MSTestVerifier)
End Class
