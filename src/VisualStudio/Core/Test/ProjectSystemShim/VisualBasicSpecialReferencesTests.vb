' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class VisualBasicSpecialReferencesTests
        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub ProjectIncludesReferencesToMscorlibSystemAndMicrosoftVisualBasic()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                project.SetCompilerOptions(CreateMinimalCompilerOptions(project))

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()

                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("mscorlib.dll")))
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("System.dll")))

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub ProjectWithoutStandardLibsDoesNotReferenceSystem()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")
                Dim options = CreateMinimalCompilerOptions(project)

                options.bNoStandardLibs = True
                project.SetCompilerOptions(options)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()

                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("mscorlib.dll")))
                Assert.False(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("System.dll")))

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub ProjectWithoutVisualBasicRuntimeDoesNotReferenceMicrosoftVisualBasic()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")
                Dim options = CreateMinimalCompilerOptions(project)

                options.vbRuntimeKind = VBRuntimeKind.NoRuntime
                project.SetCompilerOptions(options)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()

                Assert.False(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("mscorlib.dll")))
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("System.dll")))

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(860964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860964")>
        Public Sub AddingReferenceToMicrosoftVisualBasicBeforeSettingOptionsShouldNotCrash()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                project.AddMetaDataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll"), bAssembly:=True)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                Dim options = CreateMinimalCompilerOptions(project)
                project.SetCompilerOptions(options)

                ' It should still reference the VB runtime
                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                ' Now, remove the VB runtime reference
                options.vbRuntimeKind = VBRuntimeKind.NoRuntime
                project.SetCompilerOptions(options)

                ' It should still reference the VB runtime
                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(3477, "https://github.com/dotnet/roslyn/issues/3477")>
        Public Sub ProjectWithEmptySdkPathHasNoReferences()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test", compilerHost:=MockCompilerHost.NoSdkCompilerHost)

                project.SetCompilerOptions(CreateMinimalCompilerOptions(project))

                ' We should have no references
                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.Empty(workspaceProject.MetadataReferences)

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(860964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860964")>
        Public Sub AddingReferenceToMicrosoftVisualBasicAfterSettingOptionsShouldNotCrash()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                project.SetCompilerOptions(CreateMinimalCompilerOptions(project))

                ' We already should be referencing the VB runtime at this point
                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                project.AddMetaDataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll"), bAssembly:=True)

                ' It still should be referencing it
                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                project.RemoveMetaDataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll"))

                ' It still should be referencing it since we're implicitly adding it as a part of the options
                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Assert.True(workspaceProject.HasMetadataReference(MockCompilerHost.FullFrameworkCompilerHost.GetWellKnownDllName("Microsoft.VisualBasic.dll")))

                project.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddingReferenceToProjectMetadataPromotesToProjectReference()
            Using environment = New TestEnvironment()

                Dim project1 = CreateVisualBasicProject(environment, "project1")
                environment.ProjectTracker.UpdateProjectBinPath(project1, Nothing, "C:\project1.dll")

                Dim project2 = CreateVisualBasicProject(environment, "project2")
                environment.ProjectTracker.UpdateProjectBinPath(project2, Nothing, "C:\project2.dll")

                ' since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project2.AddMetaDataReference("c:\project1.dll", True)

                Assert.Equal(True, project2.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project1.Id))

                project2.Disconnect()
                project1.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddCyclicProjectMetadataReferences()
            Using environment = New TestEnvironment()

                Dim project1 = CreateVisualBasicProject(environment, "project1")
                environment.ProjectTracker.UpdateProjectBinPath(project1, Nothing, "C:\project1.dll")

                Dim project2 = CreateVisualBasicProject(environment, "project2")
                environment.ProjectTracker.UpdateProjectBinPath(project2, Nothing, "C:\project2.dll")

                project1.AddProjectReference(project2)

                ' normally this metadata reference would be elevated to a project reference, but fails because of cyclicness
                project2.AddMetaDataReference("c:\project1.dll", True)

                Assert.Equal(True, project1.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project2.Id))
                Assert.Equal(False, project2.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project1.Id))

                project2.Disconnect()
                project1.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddCyclicProjectReferences()
            Using environment = New TestEnvironment()

                Dim project1 = CreateVisualBasicProject(environment, "project1")
                Dim project2 = CreateVisualBasicProject(environment, "project2")

                project1.AddProjectReference(project2)
                project2.AddProjectReference(project1)

                Assert.Equal(True, project1.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project2.Id))
                Assert.Equal(False, project2.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project1.Id))

                project2.Disconnect()
                project1.Disconnect()
            End Using
        End Sub

        <Fact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub AddCyclicProjectReferencesDeep()
            Using environment = New TestEnvironment()

                Dim project1 = CreateVisualBasicProject(environment, "project1")
                Dim project2 = CreateVisualBasicProject(environment, "project2")
                Dim project3 = CreateVisualBasicProject(environment, "project3")
                Dim project4 = CreateVisualBasicProject(environment, "project4")

                project1.AddProjectReference(project2)
                project2.AddProjectReference(project3)
                project3.AddProjectReference(project4)
                project4.AddProjectReference(project1)

                Assert.Equal(True, project1.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project2.Id))
                Assert.Equal(True, project2.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project3.Id))
                Assert.Equal(True, project3.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project4.Id))
                Assert.Equal(False, project4.GetCurrentProjectReferences().Any(Function(pr) pr.ProjectId = project1.Id))

                project4.Disconnect()
                project3.Disconnect()
                project2.Disconnect()
                project1.Disconnect()
            End Using
        End Sub

    End Class
End Namespace
