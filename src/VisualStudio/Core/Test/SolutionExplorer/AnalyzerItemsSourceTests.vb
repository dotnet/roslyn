' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class AnalyzerItemsSourceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function Ordering() As Threading.Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Beta" FullPath="C:\Users\Bill\Documents\Analyzers\Beta.dll"/>
                        <Analyzer Name="Alpha" FullPath="C:\Users\Bill\Documents\Analyzers\Alpha.dll"/>
                        <Analyzer Name="Gamma" FullPath="C:\Users\Bill\Documents\Analyzers\Gamma.dll"/>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim analyzerItemsSource = New AnalyzerItemSource(analyzerFolder, New FakeAnalyzersCommandHandler)

                Dim analyzers = analyzerItemsSource.Items.Cast(Of AnalyzerItem)().ToArray()

                Assert.Equal(expected:=3, actual:=analyzers.Length)
                Assert.Equal(expected:="Alpha", actual:=analyzers(0).Text)
                Assert.Equal(expected:="Beta", actual:=analyzers(1).Text)
                Assert.Equal(expected:="Gamma", actual:=analyzers(2).Text)
            End Using
        End Function
    End Class
End Namespace

