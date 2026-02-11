' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
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
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
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
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)

                Using If(removeInBatch, project.CreateBatchScope(), Nothing)
                    project.RemoveAnalyzerReference(analyzerPath)
                End Using
            End Using
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function LoadDiagnosticsRemovedOnProjectRemoval(removeInBatch As Boolean) As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)

                Using If(removeInBatch, project.CreateBatchScope(), Nothing)
                    project.RemoveFromWorkspace()
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function LoadDiagnosticsStayIfRemoveAndAddInBatch() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)
                Dim analyzerPath = "Z:\TestAnalyzer" + Guid.NewGuid().ToString() + ".dll"

                project.AddAnalyzerReference(analyzerPath)

                ' Force there to be errors trying to load the missing DLL
                Dim analyzers = environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Single().GetAnalyzers(LanguageNames.CSharp)
                Assert.Empty(analyzers)

                Using project.CreateBatchScope()
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function RazorSourceGenerator_FromSdk() As Task
            Using environment = New TestEnvironment()
                Dim providerFactory = DirectCast(environment.ExportProvider.GetExportedValue(Of IVisualStudioDiagnosticAnalyzerProviderFactory), MockVisualStudioDiagnosticAnalyzerProviderFactory)
                providerFactory.ContentTypeName = VisualStudioDiagnosticAnalyzerProvider.RazorContentTypeName
                providerFactory.Extensions =
                {
                    ({
                        Path.Combine(TempRoot.Root, "File.dll")
                     },
                     "AnotherExtension")
                }

                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' add Razor source generator and a couple more other analyzer files:
                Dim path1 = Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "Microsoft.NET.Sdk.Razor.SourceGenerators.dll")
                Dim path2 = Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk.Razor", "source-generators", "SdkDependency1.dll")
                project.AddAnalyzerReference(path1)
                project.AddAnalyzerReference(path2)

                AssertEx.Equal({path1, path2}, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))
            End Using
        End Function

        <WpfFact>
        Public Async Function CodeStyleAnalyzers_CSharp_FromSdk_AreIgnored() As Task
            Using environment = New TestEnvironment()
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' Ensure HasSdkCodeStyleAnalyzers is proper.
                Assert.False(project.HasSdkCodeStyleAnalyzers)

                ' These are the in-box C# codestyle analyzers that ship with the SDK
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CodeStyle.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll"))

                ' Ensure HasSdkCodeStyleAnalyzers is being properly updated.
                Assert.True(project.HasSdkCodeStyleAnalyzers)

                ' Ensure they are not returned when getting AnalyzerReferences
                Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)

                ' Add a non-codestyle analyzer to the project
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Dir", "File.dll"))

                ' Ensure it is returned as expected
                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

                ' Remove codestyle analyzers
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CodeStyle.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "cs", "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.dll"))

                ' Ensure HasSdkCodeStyleAnalyzers is being properly updated.
                Assert.False(project.HasSdkCodeStyleAnalyzers)
            End Using
        End Function

        <WpfFact>
        Public Async Function CodeStyleAnalyzers_VisualBasic_FromSdk_AreIgnored() As Task
            Using environment = New TestEnvironment()
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.VisualBasic, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' Ensure HasSdkCodeStyleAnalyzers is proper.
                Assert.False(project.HasSdkCodeStyleAnalyzers)

                ' These are the in-box VB codestyle analyzers that ship with the SDK
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.CodeStyle.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.VisualBasic.CodeStyle.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.VisualBasic.CodeStyle.Fixes.dll"))

                ' Ensure HasSdkCodeStyleAnalyzers is being properly updated.
                Assert.True(project.HasSdkCodeStyleAnalyzers)

                ' Ensure they are not returned when getting AnalyzerReferences
                Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)

                ' Add a non-codestyle analyzer to the project
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Dir", "File.dll"))

                ' Ensure it is returned as expected
                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))

                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.CodeStyle.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.CodeStyle.Fixes.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.VisualBasic.CodeStyle.dll"))
                project.RemoveAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "codestyle", "vb", "Microsoft.CodeAnalysis.VisualBasic.CodeStyle.Fixes.dll"))

                ' Ensure HasSdkCodeStyleAnalyzers is being properly updated.
                Assert.False(project.HasSdkCodeStyleAnalyzers)
            End Using
        End Function

        <WpfFact>
        Public Async Function RedirectedAnalyzers_CSharp() As Task
            Using environment = New TestEnvironment(GetType(Redirector))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                ' Add analyzers
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "analyzers", "Microsoft.CodeAnalysis.NetAnalyzers.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "analyzers", "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll"))
                project.AddAnalyzerReference(Path.Combine(TempRoot.Root, "Dir", "File.dll"))

                ' Ensure the SDK ones are redirected
                AssertEx.Equal(
                {
                    Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "analyzers", "Microsoft.CodeAnalysis.NetAnalyzers.redirected.dll"),
                    Path.Combine(TempRoot.Root, "Sdks", "Microsoft.NET.Sdk", "analyzers", "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.redirected.dll"),
                    Path.Combine(TempRoot.Root, "Dir", "File.dll")
                }, environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences.Select(Function(r) r.FullPath))
            End Using
        End Function

        <Export(GetType(IAnalyzerAssemblyRedirector))>
        Private Class Redirector
            Implements IAnalyzerAssemblyRedirector

            <ImportingConstructor, Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function RedirectPath(fullPath As String) As String Implements IAnalyzerAssemblyRedirector.RedirectPath
                If fullPath.Contains("Microsoft.NET.Sdk") Then
                    Return Path.ChangeExtension(fullPath, ".redirected.dll")
                End If

                Return Nothing
            End Function
        End Class
    End Class
End Namespace
