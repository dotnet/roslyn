' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.Testing.Verifiers

<Obsolete(ObsoleteMessages.FrameworkPackages)>
Public Class CodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New})
    Inherits VisualBasicCodeRefactoringVerifier(Of TCodeRefactoring, XUnitVerifier)
End Class
