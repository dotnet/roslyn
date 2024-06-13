' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing

<Obsolete(ObsoleteMessages.FrameworkPackages)>
Module CodeRefactoringVerifier
    Public Function Create(Of TCodeRefactoring As {CodeRefactoringProvider, New})() As CodeRefactoringVerifier(Of TCodeRefactoring)
        Return New CodeRefactoringVerifier(Of TCodeRefactoring)
    End Function
End Module
