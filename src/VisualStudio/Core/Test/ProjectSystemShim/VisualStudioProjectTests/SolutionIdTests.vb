' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class SolutionIdTests
        <WpfFact>
        <WorkItem(31686, "https://github.com/dotnet/roslyn/issues/31686")>
        Public Sub RemovingAndAddingProjectCreatesNewSolutionId()
            Using environment = New TestEnvironment()
                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace("Project1", LanguageNames.CSharp)
                Dim solutionId = environment.Workspace.CurrentSolution.Id

                project1.RemoveFromWorkspace()

                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace("Project2", LanguageNames.CSharp)

                ' A new ID should have been generated for the new solution
                Assert.NotEqual(solutionId, environment.Workspace.CurrentSolution.Id)
            End Using
        End Sub

        <WpfFact>
        Public Sub AddingASecondProjectLeavesSolutionIdUntouched()
            Using environment = New TestEnvironment()
                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace("Project1", LanguageNames.CSharp)
                Dim solutionId = environment.Workspace.CurrentSolution.Id
                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace("Project2", LanguageNames.CSharp)

                Assert.Equal(solutionId, environment.Workspace.CurrentSolution.Id)
            End Using
        End Sub
    End Class
End Namespace
