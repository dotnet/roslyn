﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.[Shared].TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class AnalyzerReferenceTests
        <WpfFact>
        Public Async Function RemoveAndReAddInSameBatchWorksCorrectly() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)
                Const analyzerPath = "Z:\TestAnalyzer.dll"

                project.AddAnalyzerReference(analyzerPath)

                Using project.CreateBatchScope()
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                End Using

                ' In the end, we should have exactly one reference
                Assert.Single(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function LoadDiagnosticsRemovedOnAnalyzerReferenceRemoval(removeInBatch As Boolean) As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)

                Assert.Single(Await GetDiagnostics(environment), Function(d) d.Id = DocumentAnalysisExecutor.WRN_UnableToLoadAnalyzerIdCS)

                Using If(removeInBatch, project.CreateBatchScope(), Nothing)
                    project.RemoveAnalyzerReference(analyzerPath)
                End Using

                Assert.Empty(Await GetDiagnostics(environment))
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function LoadDiagnosticsRemovedOnProjectRemoval(removeInBatch As Boolean) As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)
                Assert.Single(Await GetDiagnostics(environment), Function(d) d.Id = DocumentAnalysisExecutor.WRN_UnableToLoadAnalyzerIdCS)

                Using If(removeInBatch, project.CreateBatchScope(), Nothing)
                    project.RemoveFromWorkspace()
                End Using

                Assert.Empty(Await GetDiagnostics(environment))
            End Using
        End Function

        <WpfFact>
        Public Async Function LoadDiagnosticsStayIfRemoveAndAddInBatch() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)
                Assert.Single(Await GetDiagnostics(environment), Function(d) d.Id = DocumentAnalysisExecutor.WRN_UnableToLoadAnalyzerIdCS)

                Using project.CreateBatchScope()
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                End Using

                ' We should still have a diagnostic; the real point of this assertion isn't that
                ' we keep it around immediately, but we don't accidentally screw up the batching and 
                ' lose the diagnostic permanently.
                Assert.Single(Await GetDiagnostics(environment), Function(d) d.Id = DocumentAnalysisExecutor.WRN_UnableToLoadAnalyzerIdCS)
            End Using
        End Function

        <WpfFact>
        Public Async Function RazorSourceGenerator_FromVsix() As Task
            Using environment = New TestEnvironment()
                Dim providerFactory = DirectCast(environment.ExportProvider.GetExportedValue(Of IVisualStudioDiagnosticAnalyzerProviderFactory), MockVisualStudioDiagnosticAnalyzerProviderFactory)
                providerFactory.Extensions =
                {
                    ({
                        Path.Combine(TempRoot.Root, "RazorVsix", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"),
                        Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency1.dll"),
                        Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency2.dll")
                     },
                     "Microsoft.VisualStudio.RazorExtension"),
                     ({
                        Path.Combine(TempRoot.Root, "File.dll")
                     },
                     "AnotherExtension")
                }

                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)

                ' adding just Razor dependency and not the main source generator is a no-op
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll"))

                Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)

                ' removing just Razor dependency and not the main source generator is a no-op
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll"))

                Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)

                ' add Razor source generator and a couple more other analyzer files:
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Some other directory", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Dir", "File.dll"))

                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "RazorVsix", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"),
                    Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency1.dll"),
                    Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency2.dll"),
                    Path.Combine(TempRoot.Root, "Some other directory", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"),
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

                ' add Razor source generator again:
                Assert.Throws(Of ArgumentException)(
                    "fullPath",
                    Sub() project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll")))

                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "RazorVsix", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"),
                    Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency1.dll"),
                    Path.Combine(TempRoot.Root, "RazorVsix", "VsixDependency2.dll"),
                    Path.Combine(TempRoot.Root, "Some other directory", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"),
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

                ' remove:
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Some other directory", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll"))

                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

                ' remove again:
                Assert.Throws(Of ArgumentException)(
                    "fullPath",
                    Sub() project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators.dll")))

                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))
            End Using
        End Function

        <WpfFact>
        Public Async Function RazorSourceGenerator_FromSdk() As Task
            Using environment = New TestEnvironment()
                Dim providerFactory = DirectCast(environment.ExportProvider.GetExportedValue(Of IVisualStudioDiagnosticAnalyzerProviderFactory), MockVisualStudioDiagnosticAnalyzerProviderFactory)
                providerFactory.Extensions =
                {
                     ({
                        Path.Combine(TempRoot.Root, "File.dll")
                     },
                     "AnotherExtension")
                }

                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)

                ' add Razor source generator and a couple more other analyzer filess:
                Dim path1 = Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.NET.Sdk.Razor.SourceGenerators.dll")
                Dim path2 = Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll")
                project.AddAnalyzerReference(path1)
                project.AddAnalyzerReference(path2)

                AssertEx.Equal({path1, path2}, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

            End Using
        End Function

        Private Shared Async Function GetDiagnostics(environment As TestEnvironment) As Task(Of ImmutableArray(Of DiagnosticData))
            ' Wait for diagnostics to be updated asynchronously
            Dim waiter = environment.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider).GetWaiter(FeatureAttribute.DiagnosticService)
            Await waiter.ExpeditedWaitAsync()

            Dim diagnosticService = environment.ExportProvider.GetExportedValue(Of IDiagnosticService)
            Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                environment.Workspace,
                projectId:=Nothing,
                documentId:=Nothing,
                id:=Nothing,
                includeSuppressedDiagnostics:=True,
                CancellationToken.None)
            Return diagnostics
        End Function
    End Class
End Namespace
