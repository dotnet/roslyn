' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class WorkspaceChangedEventTests
        <WpfTheory>
        <CombinatorialData>
        Public Async Function AddingASingleSourceFileRaisesDocumentAdded(addInBatch As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace("Project", LanguageNames.CSharp)
                Dim workspaceChangeEvents = New WorkspaceChangeWatcher(environment)

                Using If(addInBatch, project.CreateBatchScope(), Nothing)
                    project.AddSourceFile("Z:\Test.vb")
                End Using

                Dim change = Assert.Single(Await workspaceChangeEvents.GetNewChangeEventsAsync())

                Assert.Equal(WorkspaceChangeKind.DocumentAdded, change.Kind)
                Assert.Equal(project.Id, change.ProjectId)
                Assert.Equal(environment.Workspace.CurrentSolution.Projects.Single().DocumentIds.Single(), change.DocumentId)
            End Using
        End Function

        <WpfFact>
        Public Async Function AddingTwoDocumentsInBatchRaisesProjectChanged() As Task
            Using environment = New TestEnvironment()
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace("Project", LanguageNames.CSharp)
                Dim workspaceChangeEvents = New WorkspaceChangeWatcher(environment)

                Using project.CreateBatchScope()
                    project.AddSourceFile("Z:\Test1.vb")
                    project.AddSourceFile("Z:\Test2.vb")
                End Using

                Dim change = Assert.Single(Await workspaceChangeEvents.GetNewChangeEventsAsync())

                Assert.Equal(WorkspaceChangeKind.ProjectChanged, change.Kind)
                Assert.Equal(project.Id, change.ProjectId)
                Assert.Null(change.DocumentId)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function AddingASingleAdditionalFileInABatchRaisesDocumentAdded(addInBatch As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace("Project", LanguageNames.CSharp)
                Dim workspaceChangeEvents = New WorkspaceChangeWatcher(environment)

                Using If(addInBatch, project.CreateBatchScope(), Nothing)
                    project.AddAdditionalFile("Z:\Test.vb")
                End Using

                Dim change = Assert.Single(Await workspaceChangeEvents.GetNewChangeEventsAsync())

                Assert.Equal(WorkspaceChangeKind.AdditionalDocumentAdded, change.Kind)
                Assert.Equal(project.Id, change.ProjectId)
                Assert.Equal(environment.Workspace.CurrentSolution.Projects.Single().AdditionalDocumentIds.Single(), change.DocumentId)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function AddingASingleMetadataReferenceRaisesProjectChanged(addInBatch As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace("Project", LanguageNames.CSharp)
                Dim workspaceChangeEvents = New WorkspaceChangeWatcher(environment)

                Using If(addInBatch, project.CreateBatchScope(), Nothing)
                    project.AddMetadataReference("Z:\Test.dll", MetadataReferenceProperties.Assembly)
                End Using

                Dim change = Assert.Single(Await workspaceChangeEvents.GetNewChangeEventsAsync())

                Assert.Equal(WorkspaceChangeKind.ProjectChanged, change.Kind)
                Assert.Equal(project.Id, change.ProjectId)
                Assert.Null(change.DocumentId)
            End Using
        End Function

        <WpfFact>
        <WorkItem(34309, "https://github.com/dotnet/roslyn/issues/34309")>
        Public Async Function StartingAndEndingBatchWithNoChangesDoesNothing() As Task
            Using environment = New TestEnvironment()
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace("Project", LanguageNames.CSharp)
                Dim workspaceChangeEvents = New WorkspaceChangeWatcher(environment)
                Dim startingSolution = environment.Workspace.CurrentSolution

                project.CreateBatchScope().Dispose()

                Assert.Empty(Await workspaceChangeEvents.GetNewChangeEventsAsync())
                Assert.Same(startingSolution, environment.Workspace.CurrentSolution)
            End Using
        End Function
    End Class
End Namespace
