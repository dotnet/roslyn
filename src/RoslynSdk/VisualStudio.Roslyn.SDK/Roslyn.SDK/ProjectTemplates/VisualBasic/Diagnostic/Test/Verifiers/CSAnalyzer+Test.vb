Imports Microsoft.CodeAnalysis.CSharp.Testing
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Partial Public NotInheritable Class CSharpAnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Public Class Test
        Inherits CSharpAnalyzerTest(Of TAnalyzer, DefaultVerifier)

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
