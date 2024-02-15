Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Partial Public NotInheritable Class VisualBasicCodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New})
    Public Class Test
        Inherits VisualBasicCodeRefactoringTest(Of TCodeRefactoring, DefaultVerifier)

    End Class
End Class
