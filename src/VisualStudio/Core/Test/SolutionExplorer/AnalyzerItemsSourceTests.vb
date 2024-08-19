' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <UseExportProvider>
    Public NotInheritable Class AnalyzerItemsSourceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function Ordering() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Analyzer Name="Beta" FullPath="C:\Users\Bill\Documents\Analyzers\Beta.dll"/>
                        <Analyzer Name="Alpha" FullPath="C:\Users\Bill\Documents\Analyzers\Alpha.dll"/>
                        <Analyzer Name="Gamma" FullPath="C:\Users\Bill\Documents\Analyzers\Gamma.dll"/>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzerFolder = New AnalyzersFolderItem(workspace, project.Id, Nothing, Nothing)
                Dim listenerProvider = workspace.GetService(Of IAsynchronousOperationListenerProvider)
                Dim analyzerItemsSource = New AnalyzerItemSource(
                    analyzerFolder, New FakeAnalyzersCommandHandler(), listenerProvider)

                Dim waiter = listenerProvider.GetWaiter(FeatureAttribute.SourceGenerators)
                Await waiter.ExpeditedWaitAsync()

                Dim analyzers = analyzerItemsSource.Items.Cast(Of AnalyzerItem)().ToArray()

                Assert.Equal(expected:=3, actual:=analyzers.Length)
                Assert.Equal(expected:="Alpha", actual:=analyzers(0).Text)
                Assert.Equal(expected:="Beta", actual:=analyzers(1).Text)
                Assert.Equal(expected:="Gamma", actual:=analyzers(2).Text)
            End Using
        End Function
    End Class
End Namespace

