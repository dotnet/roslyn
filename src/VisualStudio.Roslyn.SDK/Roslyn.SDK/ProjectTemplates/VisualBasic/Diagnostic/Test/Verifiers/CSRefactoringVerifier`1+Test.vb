Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CSharp.Testing
Imports Microsoft.CodeAnalysis.Testing.Verifiers

Partial Public NotInheritable Class CSharpCodeRefactoringVerifier(Of TCodeRefactoring As {CodeRefactoringProvider, New})
    Public Class Test
        Inherits CSharpCodeRefactoringTest(Of TCodeRefactoring, MSTestVerifier)

        Public Sub New()
            SolutionTransforms.Add(
                Function(solution, projectId)
                    Dim compilationOptions = solution.GetProject(projectId).CompilationOptions
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings))
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions)

                    Return solution
                End Function)
        End Sub
    End Class
End Class
