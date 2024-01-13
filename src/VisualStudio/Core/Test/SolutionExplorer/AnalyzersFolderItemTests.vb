' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class AnalyzersFolderItemTests
        <Fact>
        Public Sub Name()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Goo" FullPath="C:\Users\Bill\Documents\Analyzers\Goo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)

                Assert.Equal(expected:=SolutionExplorerShim.Analyzers, actual:=analyzerFolder.Text)
            End Using
        End Sub

        <Fact>
        Public Sub BrowseObject1()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Goo" FullPath="C:\Users\Bill\Documents\Analyzers\Goo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim browseObject = DirectCast(analyzerFolder.GetBrowseObject(), AnalyzersFolderItem.BrowseObject)

                Assert.Equal(expected:=SolutionExplorerShim.Analyzers, actual:=browseObject.GetComponentName())
                Assert.Equal(expected:=SolutionExplorerShim.Folder_Properties, actual:=browseObject.GetClassName())
            End Using
        End Sub
    End Class
End Namespace
