' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class MetadataToProjectReferenceConversionTests
        <WpfFact>
        <WorkItem(32554, "https://github.com/dotnet/roslyn/issues/32554")>
        Public Sub ProjectReferenceConvertedToMetadataReferenceCanBeRemoved()
            Using environment = New TestEnvironment()
                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project1",
                    LanguageNames.CSharp)

                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project2",
                    LanguageNames.CSharp)

                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                project2.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getProject2 = Function() environment.Workspace.CurrentSolution.GetProject(project2.Id)

                Assert.Single(getProject2().ProjectReferences)
                Assert.Empty(getProject2().MetadataReferences)

                project1.OutputFilePath = Nothing

                Assert.Single(getProject2().MetadataReferences)
                Assert.Empty(getProject2().ProjectReferences)

                project2.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(857595, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/857595")>
        Public Sub TwoProjectsProducingSameOutputPathBehavesCorrectly()
            Using environment = New TestEnvironment()
                Dim referencingProject = environment.ProjectFactory.CreateAndAddToWorkspace("referencingProject", LanguageNames.CSharp)

                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace("project1", LanguageNames.CSharp)

                ' First: have a single project producing this DLL, and ensure we wired up correctly
                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Assert.Equal(project1.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)

                ' Create a second project referencing this same DLL. By rule, we now don't know which project to reference, so we just convert back to
                ' a file reference because something is screwed up.
                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace("project2", LanguageNames.CSharp)
                project2.OutputFilePath = ReferencePath

                Assert.Empty(getReferencingProject().ProjectReferences)
                Assert.Single(getReferencingProject().MetadataReferences)

                ' Remove project1. We should then be referencing project2
                project1.RemoveFromWorkspace()

                Assert.Equal(project2.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Sub

        <WpfFact>
        Public Sub TwoProjectsProducingSameOutputPathAndIntermediateOutputBehavesCorrectly()
            Using environment = New TestEnvironment()
                Dim referencingProject = environment.ProjectFactory.CreateAndAddToWorkspace("referencingProject", LanguageNames.CSharp)

                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace("project1", LanguageNames.CSharp)

                ' First: have a single project producing this DLL, and ensure we wired up correctly
                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                referencingProject.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getReferencingProject = Function() environment.Workspace.CurrentSolution.GetProject(referencingProject.Id)

                Assert.Equal(project1.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)

                ' Create a second project referencing this same DLL. We'll make this one even more complicated by using the same path for both the
                ' regular OutputFilePath and the IntermediateOutputFilePath
                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace("project2", LanguageNames.CSharp)
                project2.CompilationOutputAssemblyFilePath = ReferencePath
                project2.OutputFilePath = ReferencePath

                Assert.Empty(getReferencingProject().ProjectReferences)
                Assert.Single(getReferencingProject().MetadataReferences)

                ' Remove project1. We should then be referencing project2
                project1.RemoveFromWorkspace()

                Assert.Equal(project2.Id, Assert.Single(getReferencingProject().ProjectReferences).ProjectId)
                Assert.Empty(getReferencingProject().MetadataReferences)
            End Using
        End Sub

        ' This is a test for a potential race between two operations; with 20 iterations on my machine either all would fail
        ' or one might pass, it seems the race is easy enough to hit without the fix.
        <WpfTheory>
        <CombinatorialData>
        Public Sub ProjectBeingAddedWhileOutputPathBeingUpdatedDoesNotRace(<CombinatorialRange(0, 20)> iteration As Integer)
            Using environment = New TestEnvironment()
                Dim referencingProject = environment.ProjectFactory.CreateAndAddToWorkspace("referencingProject", LanguageNames.CSharp)
                Dim referencedProject = environment.ProjectFactory.CreateAndAddToWorkspace("referencedProject", LanguageNames.CSharp)

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
        End Sub
    End Class
End Namespace
