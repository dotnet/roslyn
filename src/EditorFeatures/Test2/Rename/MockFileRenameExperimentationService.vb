Imports System.Composition
Imports Microsoft.CodeAnalysis.Experiments
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    <ExportWorkspaceService(GetType(IExperimentationService), WorkspaceKind.Test), [Shared]>
    <PartNotDiscoverable>
    Public Class MockFileRenameExperimentationService
        Implements IExperimentationService

        Dim IsEnabled As Boolean = True

        Public Function IsExperimentEnabled(experimentName As String) As Boolean Implements IExperimentationService.IsExperimentEnabled
            Return IsEnabled And WellKnownExperimentNames.RoslynInlineRenameFile.Equals(experimentName)
        End Function

        Public Sub SetEnabled(enabled As Boolean)
            IsEnabled = enabled
        End Sub
    End Class
End Namespace
