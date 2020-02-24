' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicCodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeRefactoringVerifier(Of TCodeRefactoring, VisualBasicCodeRefactoringTest(Of TCodeRefactoring, TVerifier), TVerifier)
End Class
