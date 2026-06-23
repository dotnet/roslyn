' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Serialization
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.TaskList
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics

    <[UseExportProvider]>
    Public Class VisualStudioDiagnosticIdCacheTests
        Private Shared ReadOnly s_composition As TestComposition = VisualStudioTestCompositions.LanguageServices

        <WpfFact>
        Public Async Function LegacyProject_RegistersProjectWithCache() As Task
            Using environment = New TestEnvironment()
                Dim workspace = environment.Workspace
                Dim diagnosticIdCache = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()
                Dim listenerProvider = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()

                ' There should be no registered projects in an empty workspace.
                Assert.Equal(0, diagnosticIdCache.GetTestAccessor().RegisteredProjectCount)

                ' Create a legacy project.
                Dim csharpProject = CSharpHelpers.CreateCSharpProject(environment, "Test")
                Dim project = workspace.CurrentSolution.Projects.Single()

                ' The project should have been registered with the cache but it should be empty until diagnostics are fetched.
                Assert.Equal(1, diagnosticIdCache.GetTestAccessor().RegisteredProjectCount)
                Assert.False(diagnosticIdCache.TryGetDiagnosticIds(project.Id, Nothing))

                ' Asking the cache to refresh and waiting for DiagnosticService to process should populate the cache.
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' Cache should now be populated and contain the legacy project id.
                Assert.True(diagnosticIdCache.TryGetDiagnosticIds(project.Id, Nothing))
            End Using
        End Function

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_Populates() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim diagnosticIdCache = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                ' Create an in memory analyzer.
                Dim analyzer = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference)

                ' Add the analyzer reference to our project.
                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference)
                Assert.True(workspace.TryApplyChanges(project.Solution))
                project = workspace.CurrentSolution.Projects.Single()

                ' The cache should not contain the project until it is registered.
                Assert.False(diagnosticIdCache.TryGetDiagnosticIds(project.Id, Nothing))

                ' Register the project with cache (this mimics what LegacyProjects do automatically) and request
                ' the cache to refresh (this mimics what ExternalErrorDiagnosticUpdateSource does when a build is
                ' started). Waiting for the DiagnosticService to process should populate the cache.
                diagnosticIdCache.RegisterProject(project.Id)
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' Cache should be populated and contain the diagnostic id from our analyzer.
                Dim diagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(diagnosticIdCache.TryGetDiagnosticIds(project.Id, diagnosticIds))
                Assert.Contains("CACHE001", diagnosticIds)
            End Using
        End Function

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_RefreshesOnAnalyzerReferenceChange() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim diagnosticIdCache = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                ' Create an in memory analyzer.
                Dim analyzer1 = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference1 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer1))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference1)

                ' Add the analyze reference to our project.
                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference1)
                Assert.True(workspace.TryApplyChanges(project.Solution))
                project = workspace.CurrentSolution.Projects.Single()

                ' Register the project with cache (this mimics what LegacyProjects do automatically) and request
                ' the cache to refresh (this mimics what ExternalErrorDiagnosticUpdateSource does when a build is
                ' started). Waiting for the DiagnosticService to process should populate the cache.
                diagnosticIdCache.RegisterProject(project.Id)
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' Cache should be populated and contain the diagnostic id from our analyzer.
                Dim initialDiagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(diagnosticIdCache.TryGetDiagnosticIds(project.Id, initialDiagnosticIds))
                Assert.Contains("CACHE001", initialDiagnosticIds)

                ' Create an in memory analyzer with a new diagnostic id.
                Dim analyzer2 = New DescriptorOnlyAnalyzer("CACHE002")
                Dim analyzerReference2 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer2))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference2)

                ' Replace the analyzer references with only the new one.
                project = workspace.CurrentSolution.Projects.Single().WithAnalyzerReferences({analyzerReference2})
                Assert.True(workspace.TryApplyChanges(project.Solution))
                project = workspace.CurrentSolution.Projects.Single()

                ' Wait for the workspace change event to propagate to the cache.
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace})

                ' Request the cache to refresh and wait for the DiagnosticService to process should populate the cache.
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' The cache should now contain the new diagnostic id but not the old one.
                Dim refreshedDiagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(diagnosticIdCache.TryGetDiagnosticIds(project.Id, refreshedDiagnosticIds))
                Assert.Contains("CACHE002", refreshedDiagnosticIds)
                Assert.DoesNotContain("CACHE001", refreshedDiagnosticIds)
            End Using
        End Function

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_RemovesDeletedProjects() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim diagnosticIdCache = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                ' Create an in memory analyzer.
                Dim analyzer = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference)

                ' Add the analyze reference to our project.
                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference)
                Assert.True(workspace.TryApplyChanges(project.Solution))
                project = workspace.CurrentSolution.Projects.Single()

                ' Register the project with cache (this mimics what LegacyProjects do automatically) and request
                ' the cache to refresh (this mimics what ExternalErrorDiagnosticUpdateSource does when a build is
                ' started). Waiting for the DiagnosticService to process should populate the cache.
                diagnosticIdCache.RegisterProject(project.Id)
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' The cache should now contain the project and have one registered project.
                Assert.True(diagnosticIdCache.TryGetDiagnosticIds(project.Id, Nothing))
                Assert.Equal(1, diagnosticIdCache.GetTestAccessor().RegisteredProjectCount)

                ' Remove the project from the workspace.
                Dim newSolution = workspace.CurrentSolution.RemoveProject(project.Id)
                Assert.True(workspace.TryApplyChanges(newSolution))

                ' Wait for the workspace change event to propagate to the cache.
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace})

                ' Request the cache to refresh and wait for the DiagnosticService to process should populate the cache.
                diagnosticIdCache.Refresh()
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.DiagnosticService})

                ' Ensure the cache no longer contains the project and it has been unregistered.
                Assert.False(diagnosticIdCache.TryGetDiagnosticIds(project.Id, Nothing))
                Assert.Equal(0, diagnosticIdCache.GetTestAccessor().RegisteredProjectCount)
            End Using
        End Function

        Private NotInheritable Class DescriptorOnlyAnalyzer
            Inherits DiagnosticAnalyzer

            Private ReadOnly _descriptor As DiagnosticDescriptor

            Public Sub New(id As String)
                _descriptor = New DiagnosticDescriptor(id, "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            End Sub

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(_descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
            End Sub
        End Class

    End Class

End Namespace
