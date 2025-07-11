' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class MetadataToProjectReferenceConversionTests
        <WpfTheory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/32554")>
        Public Async Function ProjectReferenceConvertedToMetadataReferenceCanBeRemoved(convertReferenceBackFirst As Boolean, removeInBatch As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                project2.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getProject2 = Function() environment.Workspace.CurrentSolution.GetProject(project2.Id)

                Assert.Single(getProject2().ProjectReferences)
                Assert.Empty(getProject2().MetadataReferences)

                If convertReferenceBackFirst Then
                    project1.OutputFilePath = Nothing

                    Assert.Single(getProject2().MetadataReferences)
                    Assert.Empty(getProject2().ProjectReferences)
                End If

                Using If(removeInBatch, project2.CreateBatchScope(), Nothing)
                    project2.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
                End Using

                Assert.Empty(getProject2().MetadataReferences)
                Assert.Empty(getProject2().ProjectReferences)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function ProjectReferenceConvertedToMetadataReferenceCaseInsensitiveCanBeRemoved(convertReferenceBackFirst As Boolean, removeInBatch As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project1.dll"
                Const ReferencePathUppercase = "C:\PROJECT1.dll"

                project1.OutputFilePath = ReferencePathUppercase
                project2.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getProject2 = Function() environment.Workspace.CurrentSolution.GetProject(project2.Id)

                Assert.Single(getProject2().ProjectReferences)
                Assert.Empty(getProject2().MetadataReferences)

                If convertReferenceBackFirst Then
                    project1.OutputFilePath = Nothing

                    Assert.Single(getProject2().MetadataReferences)
                    Assert.Empty(getProject2().ProjectReferences)
                End If

                Using If(removeInBatch, project2.CreateBatchScope(), Nothing)
                    project2.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
                End Using

                Assert.Empty(getProject2().MetadataReferences)
                Assert.Empty(getProject2().ProjectReferences)
            End Using
        End Function

        <WpfFact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/857595")>
        Public Async Function TwoProjectsProducingSameOutputPathBehavesCorrectly() As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' First: have a single project producing this DLL, and ensure we wired up correctly
                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Assert.Equal(project1.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)

                ' Create a second project referencing this same DLL. By rule, we now don't know which project to reference, so we just convert back to
                ' a file reference because something is screwed up.
                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                project2.OutputFilePath = ReferencePath

                Assert.Empty(getReferencingProject().ProjectReferences)
                Assert.Single(getReferencingProject().MetadataReferences)

                ' Remove project1. We should then be referencing project2
                project1.RemoveFromWorkspace()

                Assert.Equal(project2.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Function

        <WpfFact>
        Public Async Function TwoProjectsProducingSameOutputPathAndIntermediateOutputBehavesCorrectly() As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' First: have a single project producing this DLL, and ensure we wired up correctly
                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Assert.Equal(project1.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)

                ' Create a second project referencing this same DLL. We'll make this one even more complicated by using the same path for both the
                ' regular OutputFilePath and the IntermediateOutputFilePath
                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                project2.CompilationOutputAssemblyFilePath = ReferencePath
                project2.OutputFilePath = ReferencePath

                Assert.Empty(getReferencingProject().ProjectReferences)
                Assert.Single(getReferencingProject().MetadataReferences)

                ' Remove project1. We should then be referencing project2
                project1.RemoveFromWorkspace()

                Assert.Equal(project2.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Function

        ' This is a test for a potential race between two operations; with 20 iterations on my machine either all would fail
        ' or one might pass, it seems the race is easy enough to hit without the fix.
#Disable Warning IDE0060 ' Remove unused parameter - used for test iterations.
        <WpfTheory>
        <CombinatorialData>
        Public Async Function ProjectBeingAddedWhileOutputPathBeingUpdatedDoesNotRace(<CombinatorialRange(0, 20)> iteration As Integer) As Task
#Enable Warning IDE0060 ' Remove unused parameter
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim referencedProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencedProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' First: have a single project producing this DLL, and ensure we wired up correctly
                Const ReferencePath = "C:\project.dll"
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)
                Assert.Single(getReferencingProject().MetadataReferences)

                ' We will simultaneously start the setting of an output path (which converts the metadata reference
                ' to a project reference) along with the removal of the project that contains the reference.

                Dim task1 = Task.Run(Sub()
                                         referencedProject.OutputFilePath = ReferencePath
                                     End Sub)

                Dim task2 = Task.Run(Sub()
                                         referencingProject.RemoveFromWorkspace()
                                     End Sub)

                Task.WaitAll(task1, task2)
            End Using
        End Function

        <WpfFact>
        Public Async Function AddingAndRemovingTwoReferencesWithDifferentPropertiesConvertsCorrectly() As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim referencedProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencedProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project.dll"
                referencingProject.AddMetadataReference(ReferencePath, New MetadataReferenceProperties(aliases:=ImmutableArray.Create("alias1")))
                referencedProject.OutputFilePath = ReferencePath

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                ' The alias should have flowed through correctly
                Assert.Equal("alias1", Assert.Single(Assert.Single(getReferencingProject().ProjectReferences).Aliases))

                referencingProject.AddMetadataReference(ReferencePath, New MetadataReferenceProperties(aliases:=ImmutableArray.Create("alias2")))

                Assert.Equal(2, getReferencingProject().ProjectReferences.Count())

                referencingProject.RemoveMetadataReference(ReferencePath, New MetadataReferenceProperties(aliases:=ImmutableArray.Create("alias2")))

                ' Should be back to the single reference again
                Assert.Equal("alias1", Assert.Single(Assert.Single(getReferencingProject().ProjectReferences).Aliases))

                referencingProject.RemoveMetadataReference(ReferencePath, New MetadataReferenceProperties(aliases:=ImmutableArray.Create("alias1")))

                Assert.Empty(getReferencingProject().ProjectReferences)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function MetadataReferenceBeingAddedWhileOutputPathUpdateInInterleavedBatches(closeReferencedProjectBatchFirst As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim referencedProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencedProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim referencingProjectBatch = referencingProject.CreateBatchScope()
                Dim referencedProjectBatch = referencedProject.CreateBatchScope()

                Const ReferencePath = "C:\project.dll"

                ' Set the OutputFilePath first -- we don't expect this to be visible to other projects until the batch is closed
                referencedProject.OutputFilePath = ReferencePath

                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                If closeReferencedProjectBatchFirst Then
                    referencedProjectBatch.Dispose()
                    referencingProjectBatch.Dispose()
                Else
                    referencingProjectBatch.Dispose()
                    referencedProjectBatch.Dispose()
                End If

                ' Either way, we should have a single project reference
                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)
                Assert.Single(getReferencingProject().ProjectReferences)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function MetadataReferenceBeingRemovedAndReAddedWhileOutputPathUpdateInInterleavedBatches(closeReferencedProjectBatchFirst As Boolean) As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim referencedProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencedProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project.dll"
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("temporary")))
                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Dim referencingProjectBatch = referencingProject.CreateBatchScope()
                Dim referencedProjectBatch = referencedProject.CreateBatchScope()

                ' Set the OutputFilePath second -- we don't expect this to be visible to other projects until the batch is closed
                referencedProject.OutputFilePath = ReferencePath

                referencingProject.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("temporary")))
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                If closeReferencedProjectBatchFirst Then
                    referencedProjectBatch.Dispose()
                    referencingProjectBatch.Dispose()
                Else
                    referencingProjectBatch.Dispose()
                    referencedProjectBatch.Dispose()
                End If

                ' Either way, we should have a single project reference
                Assert.Single(getReferencingProject().ProjectReferences)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39032")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/43632")>
        Public Async Function RemoveAndReAddReferenceInSingleBatchWhileChangingCase() As Task
            Using environment = New TestEnvironment()
                Dim referencingProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencingProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim referencedProject = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("referencedProject", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project.dll"
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
                referencedProject.OutputFilePath = ReferencePath

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Assert.Single(getReferencingProject().ProjectReferences)
                Assert.Empty(getReferencingProject().MetadataReferences)

                Using referencingProject.CreateBatchScope()
                    referencingProject.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
                    referencingProject.AddMetadataReference(ReferencePath.ToUpper(), MetadataReferenceProperties.Assembly)
                End Using

                ' We should still have a project reference
                Assert.Single(getReferencingProject().ProjectReferences)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39904")>
        Public Async Function MetadataReferenceCycleDoesNotCreateProjectReferenceCycleWhenAddingReferencesFirst() As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath1 = "C:\project1.dll"
                Const ReferencePath2 = "C:\project2.dll"

                project1.AddMetadataReference(ReferencePath2, MetadataReferenceProperties.Assembly)
                project2.AddMetadataReference(ReferencePath1, MetadataReferenceProperties.Assembly)

                project1.OutputFilePath = ReferencePath1
                project2.OutputFilePath = ReferencePath2

                ' Remove both from the workspace to ensure we aren't in a corrupted state somehow where removal will break further
                project1.RemoveFromWorkspace()
                project2.RemoveFromWorkspace()
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39904")>
        Public Async Function MetadataReferenceCycleDoesNotCreateProjectReferenceCycleWhenSettingOutputPathsFirst() As Task
            Using environment = New TestEnvironment()
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("project1", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim project2 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("project2", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath1 = "C:\project1.dll"
                Const ReferencePath2 = "C:\project2.dll"

                project1.OutputFilePath = ReferencePath1
                project2.OutputFilePath = ReferencePath2

                project1.AddMetadataReference(ReferencePath2, MetadataReferenceProperties.Assembly)
                project2.AddMetadataReference(ReferencePath1, MetadataReferenceProperties.Assembly)

                ' Remove both from the workspace to ensure we aren't in a corrupted state somehow where removal will break further
                project1.RemoveFromWorkspace()
                project2.RemoveFromWorkspace()
            End Using
        End Function

        <WpfFact, WorkItem(39904, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1279845")>
        Public Async Function DoNotCreateProjectReferenceWhenReferencingOwnOutput() As Task
            Using environment = New TestEnvironment()
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync("project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Const ReferencePath = "C:\project.dll"

                project.OutputFilePath = ReferencePath

                project.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Assert.Single(environment.Workspace.CurrentSolution.Projects.Single().MetadataReferences)
            End Using
        End Function
    End Class
End Namespace
