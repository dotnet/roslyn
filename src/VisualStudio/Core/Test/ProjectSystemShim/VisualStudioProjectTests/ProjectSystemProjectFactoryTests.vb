' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Workspaces.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class ProjectSystemProjectFactoryTests
        <WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7402")>
        Public Async Function ProjectInstantiatedWithCompilationOutputAssemblyFilePathCanBeChanged() As Task
            Using environment = New TestEnvironment()
                Dim creationInfo = New VisualStudioProjectCreationInfo()
                creationInfo.CompilationOutputAssemblyFilePath = "C:\output\project.dll"
                Dim project1 = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project1", LanguageNames.CSharp, creationInfo, CancellationToken.None)

                Dim projectInSolution = environment.Workspace.CurrentSolution.GetProject(project1.Id)

                Assert.Equal(creationInfo.CompilationOutputAssemblyFilePath, projectInSolution.CompilationOutputInfo.AssemblyPath)

                ' Change the path and ensure it's updated
                Dim newOutputPath = "C:\output\new\project.dll"
                project1.CompilationOutputAssemblyFilePath = newOutputPath

                Dim newProjectInSolution As Project = environment.Workspace.CurrentSolution.GetProject(project1.Id)
                Assert.Equal(newOutputPath, newProjectInSolution.CompilationOutputInfo.AssemblyPath)
            End Using
        End Function
    End Class
End Namespace
