' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class DeferredProjectLoadingTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SimpleDeferredLoading()
            Using testEnvironment = New TestEnvironment(solutionIsFullyLoaded:=False)
                CreateVisualBasicProject(testEnvironment, "TestProject")

                ' We should not yet have pushed this project to the workspace
                Assert.Empty(testEnvironment.Workspace.CurrentSolution.Projects)

                testEnvironment.NotifySolutionAsFullyLoaded()

                Assert.Single(testEnvironment.Workspace.CurrentSolution.Projects)
            End Using
        End Sub

        <Fact, WorkItem(1094112)>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub DoNotDeferLoadIfInNonBackgroundBatch()
            Using testEnvironment = New TestEnvironment(solutionIsFullyLoaded:=False)
                testEnvironment.GetSolutionLoadEvents().OnBeforeLoadProjectBatch(fIsBackgroundIdleBatch:=False)
                CreateVisualBasicProject(testEnvironment, "TestProject")
                testEnvironment.GetSolutionLoadEvents().OnAfterLoadProjectBatch(fIsBackgroundIdleBatch:=False)

                ' We should have pushed this project to the workspace
                Assert.Single(testEnvironment.Workspace.CurrentSolution.Projects)

                ' This should (in theory) do nothing
                testEnvironment.NotifySolutionAsFullyLoaded()

                Assert.Single(testEnvironment.Workspace.CurrentSolution.Projects)
            End Using
        End Sub

        <Fact, WorkItem(1094112)>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddingProjectInBatchDoesntAddAllProjects()
            Using testEnvironment = New TestEnvironment(solutionIsFullyLoaded:=False)
                CreateVisualBasicProject(testEnvironment, "TestProject1")

                testEnvironment.GetSolutionLoadEvents().OnBeforeLoadProjectBatch(fIsBackgroundIdleBatch:=False)
                CreateVisualBasicProject(testEnvironment, "TestProject2")
                testEnvironment.GetSolutionLoadEvents().OnAfterLoadProjectBatch(fIsBackgroundIdleBatch:=False)

                ' We should have pushed the second project only
                Assert.Equal("TestProject2", testEnvironment.Workspace.CurrentSolution.Projects.Single().Name)

                ' This pushes the other project too
                testEnvironment.NotifySolutionAsFullyLoaded()

                Assert.Equal(2, testEnvironment.Workspace.CurrentSolution.Projects.Count())
            End Using
        End Sub

        <Fact, WorkItem(1094112)>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddingProjectReferenceInBatchMayPushOtherProjects()
            Using testEnvironment = New TestEnvironment(solutionIsFullyLoaded:=False)
                Dim project1 = CreateVisualBasicProject(testEnvironment, "TestProject1")

                ' Include a project reference in this batch. This means that project1 must also be pushed
                testEnvironment.GetSolutionLoadEvents().OnBeforeLoadProjectBatch(fIsBackgroundIdleBatch:=False)
                Dim project2 = CreateVisualBasicProject(testEnvironment, "TestProject2")
                project2.AddProjectReference(project1)
                testEnvironment.GetSolutionLoadEvents().OnAfterLoadProjectBatch(fIsBackgroundIdleBatch:=False)

                ' We should have pushed both projects
                Assert.Equal(2, testEnvironment.Workspace.CurrentSolution.Projects.Count())
            End Using
        End Sub

        <Fact, WorkItem(1094112)>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddingProjectReferenceAfterBatchMayPushOtherProjects()
            Using testEnvironment = New TestEnvironment(solutionIsFullyLoaded:=False)
                Dim project1 = CreateVisualBasicProject(testEnvironment, "TestProject1")

                ' Include a project reference in this batch. This means that project1 must also be pushed
                testEnvironment.GetSolutionLoadEvents().OnBeforeLoadProjectBatch(fIsBackgroundIdleBatch:=False)
                Dim project2 = CreateVisualBasicProject(testEnvironment, "TestProject2")
                testEnvironment.GetSolutionLoadEvents().OnAfterLoadProjectBatch(fIsBackgroundIdleBatch:=False)

                ' We should have pushed the second project only
                Assert.Equal("TestProject2", testEnvironment.Workspace.CurrentSolution.Projects.Single().Name)

                project2.AddProjectReference(project1)

                ' We should have pushed both projects
                Assert.Equal(2, testEnvironment.Workspace.CurrentSolution.Projects.Count())
            End Using
        End Sub
    End Class
End Namespace
