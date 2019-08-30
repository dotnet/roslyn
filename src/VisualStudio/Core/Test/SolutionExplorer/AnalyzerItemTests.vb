' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <[UseExportProvider]>
    Public Class AnalyzerItemTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub Name()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Goo" FullPath="C:\Users\Bill\Documents\Analyzers\Goo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim analyzer = New AnalyzerItem(analyzerFolder, project.AnalyzerReferences.Single(), Nothing)

                Assert.Equal(expected:="Goo", actual:=analyzer.Text)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub BrowseObject1()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Goo" FullPath="C:\Users\Bill\Documents\Analyzers\Goo.dll"/>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim analyzer = New AnalyzerItem(analyzerFolder, project.AnalyzerReferences.Single(), Nothing)
                Dim browseObject = DirectCast(analyzer.GetBrowseObject(), AnalyzerItem.BrowseObject)

                Assert.Equal(expected:=SolutionExplorerShim.Analyzer_Properties, actual:=browseObject.GetClassName())
                Assert.Equal(expected:="Goo", actual:=browseObject.GetComponentName())
                Assert.Equal(expected:="Goo", actual:=browseObject.Name)
                Assert.Equal(expected:="C:\Users\Bill\Documents\Analyzers\Goo.dll", actual:=browseObject.Path)
            End Using
        End Sub
    End Class
End Namespace
