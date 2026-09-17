' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.Shell
Imports Roslyn.Test.Utilities
Imports TestResources.Analyzers

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <UseExportProvider>
    Public Class CpsDiagnosticItemSourceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function AnalyzerHasDiagnostics() As Task
            Using environment = New TestEnvironment()
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.VisualBasic, CancellationToken.None)

                Dim analyzers = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer))

                ' The choice here of this analyzer to test with is arbitray -- there's nothing special about this
                ' analyzer versus any other one.
                analyzers.Add(LanguageNames.VisualBasic, ImmutableArray.Create(Of DiagnosticAnalyzer)(New Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty.VisualBasicUseAutoPropertyAnalyzer()))
                Const analyzerPath = "C:\Analyzer.dll"
                environment.Workspace.OnAnalyzerReferenceAdded(project.Id, New TestAnalyzerReferenceByLanguage(analyzers, analyzerPath))

                Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
                Dim source As IAttachedCollectionSource = New CpsDiagnosticItemSource(
                    environment.ThreadingContext,
                    environment.Workspace,
                    ImmutableArray(Of IAnalyzerAssemblyRedirector).Empty,
                    project.Id,
                    New MockHierarchyItem() With {.CanonicalName = analyzerPath},
                    New FakeAnalyzersCommandHandler(),
                    listenerProvider)

                Assert.True(source.HasItems)

                Dim waiter = listenerProvider.GetWaiter(FeatureAttribute.SourceGenerators)
                Await waiter.ExpeditedWaitAsync()

                Dim diagnostic = Assert.IsAssignableFrom(Of ITreeDisplayItem)(Assert.Single(source.Items))
                Assert.Contains(IDEDiagnosticIds.UseAutoPropertyDiagnosticId, diagnostic.Text)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)>
        Public Async Function RedirectedSourceGeneratorIsFound() As Task
            Using environment = New TestEnvironment(GetType(TestAnalyzerAssemblyRedirector))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)

                Dim analyzerPath = Path.Combine(
                    TempRoot.Root, Guid.NewGuid().ToString(), "Microsoft.NET.Sdk", "Analyzer.dll")
                Dim redirectedAnalyzerPath = Path.ChangeExtension(analyzerPath, ".redirected.dll")

                Directory.CreateDirectory(Path.GetDirectoryName(analyzerPath))
                File.Copy(GetType(DoNothingGenerator).Assembly.Location, redirectedAnalyzerPath)

                project.AddAnalyzerReference(analyzerPath)

                Dim analyzerReference = Assert.Single(
                    environment.Workspace.CurrentSolution.GetProject(project.Id).AnalyzerReferences)
                Assert.Equal(redirectedAnalyzerPath, analyzerReference.FullPath)

                Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
                Dim analyzerAssemblyRedirectors = environment.ExportProvider.GetExportedValues(Of IAnalyzerAssemblyRedirector)().ToImmutableArray()
                Dim source As IAttachedCollectionSource = New CpsDiagnosticItemSource(
                    environment.ThreadingContext,
                    environment.Workspace,
                    analyzerAssemblyRedirectors,
                    project.Id,
                    New MockHierarchyItem() With {.CanonicalName = analyzerPath.ToLowerInvariant()},
                    New FakeAnalyzersCommandHandler(),
                    listenerProvider)

                Dim waiter = listenerProvider.GetWaiter(FeatureAttribute.SourceGenerators)
                Await waiter.ExpeditedWaitAsync()

                Dim generatorItem = Assert.IsType(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Assert.Equal(GetType(DoNothingGenerator).FullName, generatorItem.Text)
            End Using
        End Function
    End Class
End Namespace
