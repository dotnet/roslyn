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
    Public Class FileChangeTests
        <WpfTheory>
        <CombinatorialData>
        Public Async Function FileChangeAfterRemovalInUncommittedBatchIgnored(withDirectoryWatch As Boolean) As Task
            Using environment = New TestEnvironment()
                ' If we have a project directory, then we'll also have a watch for the entire directory;
                ' test both cases

                Dim projectInfo = New VisualStudioProjectCreationInfo With {
                    .FilePath = If(withDirectoryWatch, "Z:\Project.csproj", Nothing)
                }

                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, projectInfo, CancellationToken.None)

                project.AddSourceFile("Z:\Foo.cs")

                Using project.CreateBatchScope()
                    ' This shouldn't throw
                    Await environment.RaiseStaleFileChangeAsync("Z:\Foo.cs", Sub() project.RemoveSourceFile("Z:\Foo.cs"))
                End Using
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function FileChangeAfterRemoveOfProjectIgnored(withDirectoryWatch As Boolean) As Task
            Using environment = New TestEnvironment()

                ' If we have a project directory, then we'll also have a watch for the entire directory;
                ' test both cases
                Dim projectInfo = New VisualStudioProjectCreationInfo With {
                    .FilePath = If(withDirectoryWatch, "Z:\Project.csproj", Nothing)
                }

                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, projectInfo, CancellationToken.None)

                project.AddSourceFile("Z:\Foo.cs")

                Using project.CreateBatchScope()
                    ' This shouldn't throw
                    Await environment.RaiseStaleFileChangeAsync("Z:\Foo.cs", Sub() project.RemoveFromWorkspace())
                End Using
            End Using
        End Function
    End Class
End Namespace
