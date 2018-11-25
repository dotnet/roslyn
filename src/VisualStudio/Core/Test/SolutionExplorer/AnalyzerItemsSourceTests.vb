' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <[UseExportProvider]>
    Public Class AnalyzerItemsSourceTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub Ordering()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Beta" FullPath="C:\Users\Bill\Documents\Analyzers\Beta.dll"/>
                        <Analyzer Name="Alpha" FullPath="C:\Users\Bill\Documents\Analyzers\Alpha.dll"/>
                        <Analyzer Name="Gamma" FullPath="C:\Users\Bill\Documents\Analyzers\Gamma.dll"/>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim analyzerItemsSource = New AnalyzerItemSource(analyzerFolder, New FakeAnalyzersCommandHandler)

                Dim analyzers = analyzerItemsSource.Items.Cast(Of AnalyzerItem)().ToArray()

                Assert.Equal(expected:=3, actual:=analyzers.Length)
                Assert.Equal(expected:="Alpha", actual:=analyzers(0).Text)
                Assert.Equal(expected:="Beta", actual:=analyzers(1).Text)
                Assert.Equal(expected:="Gamma", actual:=analyzers(2).Text)
            End Using
        End Sub
    End Class
End Namespace

