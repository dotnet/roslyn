' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
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

                Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)()
                Dim source As IAttachedCollectionSource = New CpsDiagnosticItemSource(
                    environment.ThreadingContext,
                    environment.Workspace,
                    project.Id,
                    New MockHierarchyItem() With {.CanonicalName = analyzerPath},
                    New FakeAnalyzersCommandHandler(),
                    listenerProvider)

                Assert.True(source.HasItems)

                Dim waiter = DirectCast(listenerProvider.GetListener(FeatureAttribute.SourceGenerators), IAsynchronousOperationWaiter)
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

                project.AddAnalyzerReference(AnalyzerPath)

                Dim analyzerReference = Assert.Single(
                    environment.Workspace.CurrentSolution.GetProject(project.Id).AnalyzerReferences)
                Assert.Equal(GetType(DoNothingGenerator).Assembly.Location, analyzerReference.FullPath)

                Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)()
                Dim source As IAttachedCollectionSource = New CpsDiagnosticItemSource(
                    environment.ThreadingContext,
                    environment.Workspace,
                    project.Id,
                    New MockHierarchyItem() With {.CanonicalName = AnalyzerPath.ToLowerInvariant()},
                    New FakeAnalyzersCommandHandler(),
                    listenerProvider)

                Dim waiter = DirectCast(listenerProvider.GetListener(FeatureAttribute.SourceGenerators), IAsynchronousOperationWaiter)
                Await waiter.ExpeditedWaitAsync()

                Dim generatorItem = Assert.IsType(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Assert.Equal(GetType(DoNothingGenerator).FullName, generatorItem.Text)
            End Using
        End Function

        Private Const AnalyzerPath = "C:\Analyzer.dll"

        <Export(GetType(IAnalyzerAssemblyRedirector))>
        Private NotInheritable Class TestAnalyzerAssemblyRedirector
            Implements IAnalyzerAssemblyRedirector

            <ImportingConstructor, Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function RedirectPath(fullPath As String) As String Implements IAnalyzerAssemblyRedirector.RedirectPath
                Return If(String.Equals(fullPath, AnalyzerPath, StringComparison.OrdinalIgnoreCase), GetType(DoNothingGenerator).Assembly.Location, Nothing)
            End Function
        End Class
    End Class
End Namespace
