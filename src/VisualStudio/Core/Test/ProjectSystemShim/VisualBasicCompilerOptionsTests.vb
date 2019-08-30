' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class VisualBasicCompilerOptions
        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(867840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867840")>
        Public Sub ConditionalCompilationOptionsIncludesTargetAndVersion()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                project.SetCompilerOptions(CreateMinimalCompilerOptions(project))

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Contains(New KeyValuePair(Of String, Object)("VBC_VER", PredefinedPreprocessorSymbols.CurrentVersionNumber), options.PreprocessorSymbols)
                Assert.Contains(New KeyValuePair(Of String, Object)("TARGET", "exe"), options.PreprocessorSymbols)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")>
        Public Sub DocumentationModeSetToDiagnoseIfProducingDocFile()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszXMLDocName = "DocFile.xml"
                project.SetCompilerOptions(compilerOptions)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Equal(DocumentationMode.Diagnose, options.DocumentationMode)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SetCompilerOptions_LangVersion14()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptionsHost = DirectCast(project, Implementation.ProjectSystem.Interop.ICompilerOptionsHostObject)
                Dim supported As Boolean
                compilerOptionsHost.SetCompilerOptions("/langversion:14", supported)
                Assert.True(supported)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                ' SetCompilerOptions only handles versions 15.3 and up, so we are ignoring the
                ' /langversion:14 above in favor of the legacy value. Since the legacy value was
                ' not set, it'll just be default.
                Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), options.LanguageVersion)
                Assert.Equal(LanguageVersion.Default, options.SpecifiedLanguageVersion)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SetCompilerOptions_LangVersion15()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptionsHost = DirectCast(project, Implementation.ProjectSystem.Interop.ICompilerOptionsHostObject)
                Dim supported As Boolean
                compilerOptionsHost.SetCompilerOptions("/langversion:15", supported)
                Assert.True(supported)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                ' SetCompilerOptions only handles versions 15.3 and up, so we are ignoring the
                ' /langversion:14 above in favor of the legacy value. Since the legacy value was
                ' not set, it'll just be default.
                Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), options.LanguageVersion)
                Assert.Equal(LanguageVersion.Default, options.SpecifiedLanguageVersion)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SetCompilerOptions_LangVersionDefault()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptionsHost = DirectCast(project, Implementation.ProjectSystem.Interop.ICompilerOptionsHostObject)
                Dim supported As Boolean
                compilerOptionsHost.SetCompilerOptions("/langversion:Default", supported)
                Assert.True(supported)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Equal(LanguageVersion.Default.MapSpecifiedToEffectiveVersion(), options.LanguageVersion)
                Assert.Equal(LanguageVersion.Default, options.SpecifiedLanguageVersion)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SetCompilerOptions_LangVersion15_3()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptionsHost = DirectCast(project, Implementation.ProjectSystem.Interop.ICompilerOptionsHostObject)
                Dim supported As Boolean
                compilerOptionsHost.SetCompilerOptions("/langversion:15.3", supported)
                Assert.True(supported)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Equal(LanguageVersion.VisualBasic15_3, options.LanguageVersion)
                Assert.Equal(LanguageVersion.VisualBasic15_3, options.SpecifiedLanguageVersion)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub SetCompilerOptions_LangVersionLatest()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptionsHost = DirectCast(project, Implementation.ProjectSystem.Interop.ICompilerOptionsHostObject)
                Dim supported As Boolean
                compilerOptionsHost.SetCompilerOptions("/langversion:latest", supported)
                Assert.True(supported)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Equal(LanguageVersion.Latest.MapSpecifiedToEffectiveVersion(), options.LanguageVersion)
                Assert.Equal(LanguageVersion.Latest, options.SpecifiedLanguageVersion)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")>
        Public Sub DocumentationModeSetToParseIfNotProducingDocFile()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszXMLDocName = ""
                project.SetCompilerOptions(compilerOptions)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.ParseOptions, VisualBasicParseOptions)

                Assert.Equal(DocumentationMode.Parse, options.DocumentationMode)

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(1092636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")>
        <WorkItem(1040247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")>
        <WorkItem(1048368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")>
        Public Sub ProjectWarningsOptionSetAndUnset()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                Dim compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszWarningsAsErrors = "1234"
                project.SetCompilerOptions(compilerOptions)

                Dim workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                Dim options = DirectCast(workspaceProject.CompilationOptions, VisualBasicCompilationOptions)

                Assert.Equal(ReportDiagnostic.Error, options.SpecificDiagnosticOptions("BC1234"))

                compilerOptions.wszWarningsAsErrors = ""
                project.SetCompilerOptions(compilerOptions)

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single()
                options = DirectCast(workspaceProject.CompilationOptions, VisualBasicCompilationOptions)

                Assert.False(options.SpecificDiagnosticOptions.ContainsKey("BC1234"))

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        <WorkItem(33401, "https://github.com/dotnet/roslyn/pull/33401")>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        Public Sub ProjectOutputPathAndOutputExeNameChange()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")
                Dim compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszOutputPath = "C:\"
                compilerOptions.wszExeName = "test.dll"
                project.SetCompilerOptions(compilerOptions)
                Assert.Equal("C:\test.dll", project.GetOutputFileName())

                Dim outputs = CType(environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id), CompilationOutputFilesWithImplicitPdbPath)
                Assert.Equal("C:\test.dll", outputs.AssemblyFilePath)

                ' Change output folder from command line arguments - verify that objOutputPath changes.
                Dim newPath = "C:\NewFolder\test.dll"
                compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszOutputPath = "C:\NewFolder"
                compilerOptions.wszExeName = "test.dll"
                project.SetCompilerOptions(compilerOptions)
                Assert.Equal(newPath, project.GetOutputFileName())

                outputs = CType(environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id), CompilationOutputFilesWithImplicitPdbPath)
                Assert.Equal("C:\NewFolder\test.dll", outputs.AssemblyFilePath)

                ' Change output file name - verify that outputPath changes.
                newPath = "C:\NewFolder\test2.dll"
                compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszOutputPath = "C:\NewFolder"
                compilerOptions.wszExeName = "test2.dll"
                project.SetCompilerOptions(compilerOptions)
                Assert.Equal(newPath, project.GetOutputFileName())

                outputs = CType(environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id), CompilationOutputFilesWithImplicitPdbPath)
                Assert.Equal("C:\NewFolder\test2.dll", outputs.AssemblyFilePath)

                ' Change output file name and folder - verify that outputPath changes.
                newPath = "C:\NewFolder3\test3.dll"
                compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszOutputPath = "C:\NewFolder3"
                compilerOptions.wszExeName = "test3.dll"
                project.SetCompilerOptions(compilerOptions)
                Assert.Equal(newPath, project.GetOutputFileName())

                outputs = CType(environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id), CompilationOutputFilesWithImplicitPdbPath)
                Assert.Equal("C:\NewFolder3\test3.dll", outputs.AssemblyFilePath)

                ' Relative path - set by VBIntelliProj in VB Web App project
                compilerOptions = CreateMinimalCompilerOptions(project)
                compilerOptions.wszOutputPath = "\"
                compilerOptions.wszExeName = "test3.dll"
                project.SetCompilerOptions(compilerOptions)
                Assert.Equal(Nothing, project.GetOutputFileName())
                outputs = CType(environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id), CompilationOutputFilesWithImplicitPdbPath)
                Assert.Equal(Nothing, outputs.AssemblyFilePath)
            End Using
        End Sub
    End Class
End Namespace
