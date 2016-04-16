' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.CodeAnalysis

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class VisualBasicCompilerOptions
        <Fact()>
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

        <Fact()>
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

        <Fact()>
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

        <Fact()>
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
    End Class
End Namespace
