' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class AnalyzersFolderItemTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function Name() As Threading.Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Foo" FullPath="C:\Users\Bill\Documents\Analyzers\Foo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)

                Assert.Equal(expected:=SolutionExplorerShim.AnalyzersFolderItem_Name, actual:=analyzerFolder.Text)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function BrowseObject1() As Threading.Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Foo" FullPath="C:\Users\Bill\Documents\Analyzers\Foo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim browseObject = DirectCast(analyzerFolder.GetBrowseObject(), AnalyzersFolderItem.BrowseObject)

                Assert.Equal(expected:=SolutionExplorerShim.AnalyzersFolderItem_Name, actual:=browseObject.GetComponentName())
                Assert.Equal(expected:=SolutionExplorerShim.AnalyzersFolderItem_PropertyWindowClassName, actual:=browseObject.GetClassName())
            End Using
        End Function
    End Class
End Namespace
