Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services

Namespace Roslyn.Services.Editor.VisualBasic
    Friend Interface IVisualBasicSemanticSnapshot
        ReadOnly Property Document As IDocument
        Function GetSemanticModel(Optional cancellationToken As CancellationToken = Nothing) As SemanticModel
    End Interface

    Friend Module IVisualBasicSemanticSnapshotExtensions
        <Extension()>
        Public Function GetSyntaxTree(snapshot As IVisualBasicSemanticSnapshot, cancellationToken As CancellationToken) As SyntaxTree
            Return DirectCast(snapshot.Document.GetSyntaxTree(cancellationToken), SyntaxTree)
        End Function

        <Extension()>
        Public Function GetCompilation(snapshot As IVisualBasicSemanticSnapshot, cancellationToken As CancellationToken) As Compilation
            Return DirectCast(snapshot.Document.Project.GetCompilation(cancellationToken), Compilation)
        End Function

        <Extension()>
        Public Function GetSolution(snapshot As IVisualBasicSemanticSnapshot) As ISolution
            Return snapshot.Document.Project.Solution
        End Function

        <Extension()>
        Public Function GetProject(snapshot As IVisualBasicSemanticSnapshot) As IProject
            Return snapshot.Document.Project
        End Function
    End Module
End Namespace