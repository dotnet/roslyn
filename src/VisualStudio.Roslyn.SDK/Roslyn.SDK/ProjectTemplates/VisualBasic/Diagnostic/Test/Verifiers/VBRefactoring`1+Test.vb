Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Partial Public NotInheritable Class VisualBasicCodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New})
    Public Class Test
        Inherits VisualBasicCodeRefactoringTest(Of TCodeRefactoring, MSTestVerifier)

    End Class
End Class
