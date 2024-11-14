' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.Shell
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <UseExportProvider>
    Public Class CpsDiagnosticItemSourceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function AnalyzerHasDiagnostics() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim project = workspace.Projects.Single()

                Dim analyzers = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer))

                ' The choice here of this analyzer to test with is arbitray -- there's nothing special about this
                ' analyzer versus any other one.
                analyzers.Add(LanguageNames.VisualBasic, ImmutableArray.Create(Of DiagnosticAnalyzer)(New Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty.VisualBasicUseAutoPropertyAnalyzer()))

                Const analyzerPath = "C:\Analyzer.dll"
                workspace.OnAnalyzerReferenceAdded(project.Id, New TestAnalyzerReferenceByLanguage(analyzers, analyzerPath))

                Dim listenerProvider = workspace.GetService(Of IAsynchronousOperationListenerProvider)
                Dim source As IAttachedCollectionSource = New CpsDiagnosticItemSource(
                    workspace.GetService(Of IThreadingContext),
                    workspace,
                    project.FilePath,
                    project.Id,
                    New MockHierarchyItem() With {.CanonicalName = "\net472\analyzerdependency\" + analyzerPath},
                    New FakeAnalyzersCommandHandler, workspace.GetService(Of IDiagnosticAnalyzerService),
                    listenerProvider)

                Assert.True(source.HasItems)

                Dim waiter = DirectCast(listenerProvider.GetListener(FeatureAttribute.SourceGenerators), IAsynchronousOperationWaiter)
                Await waiter.ExpeditedWaitAsync()

                Dim diagnostic = Assert.IsAssignableFrom(Of ITreeDisplayItem)(Assert.Single(source.Items))
                Assert.Contains(IDEDiagnosticIds.UseAutoPropertyDiagnosticId, diagnostic.Text)
            End Using
        End Function
    End Class
End Namespace
