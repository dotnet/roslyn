' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class SolutionIdTests
        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31686")>
        Public Async Function RemovingAndAddingProjectCreatesNewSolutionId() As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim solutionId = environment.Workspace.CurrentSolution.Id

                project1.RemoveFromWorkspace()

                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' A new ID should have been generated for the new solution
                Assert.NotEqual(solutionId, environment.Workspace.CurrentSolution.Id)
            End Using
        End Function

        <WpfFact>
        Public Async Function AddingASecondProjectLeavesSolutionIdUntouched() As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim solutionId = environment.Workspace.CurrentSolution.Id
                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Assert.Equal(solutionId, environment.Workspace.CurrentSolution.Id)
            End Using
        End Function
    End Class
End Namespace
