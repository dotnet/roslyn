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
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics

    <[UseExportProvider]>
    Public Class VisualStudioDiagnosticIdCacheTests
        Private Shared ReadOnly s_composition As TestComposition = VisualStudioTestCompositions.LanguageServices

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_Populates() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                Dim analyzer = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference)

                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference)
                Assert.True(workspace.TryApplyChanges(project.Solution))

                project = workspace.CurrentSolution.Projects.Single()
                Dim service = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()

                Assert.False(service.TryGetDiagnosticIds(project.Id, Nothing))

                ' Waiting for DiagnosticService to process should populate the cache.
                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace, FeatureAttribute.DiagnosticService})

                Dim diagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(service.TryGetDiagnosticIds(project.Id, diagnosticIds))

                AssertEx.NotNull(diagnosticIds)
                Assert.Contains("CACHE001", diagnosticIds)
            End Using
        End Function

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_RefreshesOnAnalyzerReferenceChange() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                Dim analyzer1 = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference1 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer1))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference1)

                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference1)
                Assert.True(workspace.TryApplyChanges(project.Solution))

                project = workspace.CurrentSolution.Projects.Single()
                Dim service = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()

                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace, FeatureAttribute.DiagnosticService})

                Dim initialDiagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(service.TryGetDiagnosticIds(project.Id, initialDiagnosticIds))
                Assert.Contains("CACHE001", initialDiagnosticIds)

                Dim analyzer2 = New DescriptorOnlyAnalyzer("CACHE002")
                Dim analyzerReference2 = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer2))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference2)

                project = workspace.CurrentSolution.Projects.Single().WithAnalyzerReferences({analyzerReference2})
                Assert.True(workspace.TryApplyChanges(project.Solution))

                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace, FeatureAttribute.DiagnosticService})

                project = workspace.CurrentSolution.Projects.Single()
                Dim refreshedDiagnosticIds As ImmutableHashSet(Of String) = Nothing
                Assert.True(service.TryGetDiagnosticIds(project.Id, refreshedDiagnosticIds))

                Assert.Contains("CACHE002", refreshedDiagnosticIds)
                Assert.DoesNotContain("CACHE001", refreshedDiagnosticIds)
            End Using
        End Function

        <Fact>
        Public Async Function TestDiagnosticDescriptorCache_RemovesDeletedProjects() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp("class A { }", composition:=s_composition)
                Dim listenerProvider = workspace.GetService(Of AsynchronousOperationListenerProvider)()

                Dim analyzer = New DescriptorOnlyAnalyzer("CACHE001")
                Dim analyzerReference = New AnalyzerImageReference(ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer))
                SerializerService.TestAccessor.AddAnalyzerImageReference(analyzerReference)

                Dim project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference)
                Assert.True(workspace.TryApplyChanges(project.Solution))

                project = workspace.CurrentSolution.Projects.Single()
                Dim projectId = project.Id
                Dim service = workspace.Services.GetRequiredService(Of VisualStudioDiagnosticIdCache)()

                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace, FeatureAttribute.DiagnosticService})

                Assert.True(service.TryGetDiagnosticIds(projectId, Nothing))

                Dim newSolution = workspace.CurrentSolution.RemoveProject(projectId)
                Assert.True(workspace.TryApplyChanges(newSolution))

                Await listenerProvider.WaitAllAsync(workspace, {FeatureAttribute.Workspace, FeatureAttribute.DiagnosticService})

                Assert.False(service.TryGetDiagnosticIds(projectId, Nothing))
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
