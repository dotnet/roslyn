' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicCodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeRefactoringVerifier(Of TCodeRefactoring, VisualBasicCodeRefactoringTest(Of TCodeRefactoring, TVerifier), TVerifier)
End Class
