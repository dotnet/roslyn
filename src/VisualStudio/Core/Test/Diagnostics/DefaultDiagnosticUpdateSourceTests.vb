' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Tagging)>
    Public Class DefaultDiagnosticUpdateSourceTests
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddExcludedPartTypes(GetType(IDiagnosticUpdateSourceRegistrationService)) _
            .AddParts(GetType(MockDiagnosticUpdateSourceRegistrationService))

        Private Shared Function GetDefaultDiagnosticAnalyzerService(workspace As EditorTestWorkspace) As DefaultDiagnosticAnalyzerService
            Dim lazyIncrementalAnalyzerProviders = workspace.ExportProvider.GetExports(Of IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata)()
            Dim lazyMiscService = lazyIncrementalAnalyzerProviders.Single(Function(lazyProvider) lazyProvider.Metadata.Name = WellKnownSolutionCrawlerAnalyzers.Diagnostic AndAlso lazyProvider.Metadata.HighPriorityForActiveFile = False)
            Return Assert.IsType(Of DefaultDiagnosticAnalyzerService)(lazyMiscService.Value)
        End Function

        <WpfFact>
        Public Async Function TestMiscSquiggles() As Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, openDocuments:=True)

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim miscService = DirectCast(workspace.GetService(Of IDiagnosticAnalyzerService), DiagnosticAnalyzerService)

                DiagnosticProvider.Enable(workspace)

                Dim buffer = workspace.Documents.First().GetTextBuffer()

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim provider = workspace.ExportProvider.GetExportedValues(Of ITaggerProvider)().OfType(Of DiagnosticsSquiggleTaggerProvider)().Single()
                Dim tagger = provider.CreateTagger(Of IErrorTag)(buffer)

                Using disposable = TryCast(tagger, IDisposable)
                    Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                    Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                    Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()
                    Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync()

                    Dim snapshot = buffer.CurrentSnapshot
                    Dim spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray()

                    Assert.NotEmpty(spans)
                    Assert.True(spans.All(Function(s) s.Span.Length > 0))
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderSyntaxAndSemantics() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, openDocuments:=True)
                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)

                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()

                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, Nothing, includeSuppressedDiagnostics:=False, CancellationToken.None)

                Assert.Equal(2, diagnostics.Length)
            End Using
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/45877")>
        Public Async Function TestDefaultDiagnosticProviderSemantic() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, openDocuments:=True)
                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()

                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, Nothing, includeSuppressedDiagnostics:=False, CancellationToken.None)

                ' error CS0246: The type or namespace name 'M' could not be found
                AssertEx.Equal({"CS0246"}, diagnostics.Select(Function(d) d.Id))
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderAll() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, openDocuments:=True)
                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()

                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, Nothing, includeSuppressedDiagnostics:=False, CancellationToken.None)

                ' error CS1002: ; expected
                ' error CS0246: The type or namespace name 'M' could not be found
                AssertEx.SetEqual({"CS1002", "CS0246"}, diagnostics.Select(Function(d) d.Id))
            End Using
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/45877")>
        Public Async Function TestDefaultDiagnosticProviderRemove() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, openDocuments:=True)
                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)

                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.RemoveDocumentAsync(document.Id, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()

                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, Nothing, includeSuppressedDiagnostics:=False, CancellationToken.None)

                AssertEx.Empty(diagnostics)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscCSharpErrorSource() As Tasks.Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = EditorTestWorkspace.CreateCSharp(code.Value, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments:=True)
                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)

                DiagnosticProvider.Enable(workspace)

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Single().Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscVBErrorSource() As Task
            Dim code = <code>
Class 123
End Class
                       </code>
            Using workspace = EditorTestWorkspace.CreateVisualBasic(code.Value, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService, openDocuments:=True)
                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)

                DiagnosticProvider.Enable(workspace)

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Single().Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscDocumentWithNoCompilationWithScriptSemantic() As Task
            Dim test =
                <Workspace>
                    <Project Language="NoCompilation">
                        <Document FilePath="Somthing.something">
                            <ParseOptions Kind="Script"/>
                            Dummy content.
                        </Document>
                    </Project>
                </Workspace>

            Dim analyzerMap = ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty.Add(
                NoCompilationConstants.LanguageName, ImmutableArray.Create(Of DiagnosticAnalyzer)(New DiagnosticAnalyzerWithSemanticError()))

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeLanguageService),
                GetType(NoCompilationContentTypeDefinitions))

            Using workspace = EditorTestWorkspace.CreateWorkspace(test, openDocuments:=True, composition:=composition)
                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(analyzerMap)
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim diagnosticService = DirectCast(workspace.ExportProvider.GetExportedValue(Of IDiagnosticService), DiagnosticService)

                Dim miscService = GetDefaultDiagnosticAnalyzerService(workspace)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)

                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync()

                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, Nothing, includeSuppressedDiagnostics:=False, CancellationToken.None)

                Assert.Single(diagnostics)
            End Using
        End Function

        <DiagnosticAnalyzer(NoCompilationConstants.LanguageName)>
        Private Class DiagnosticAnalyzerWithSemanticError
            Inherits DocumentDiagnosticAnalyzer

            Public Shared ReadOnly Descriptor As DiagnosticDescriptor = New DiagnosticDescriptor(
                "Id", "Error", "Error", "Error", DiagnosticSeverity.Error, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Function AnalyzeSemanticsAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of Diagnostic))
                Return Task.FromResult(ImmutableArray.Create(Diagnostic.Create(Descriptor, Location.Create(document.FilePath, Nothing, Nothing))))
            End Function

            Public Overrides Function AnalyzeSyntaxAsync(document As Document, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of Diagnostic))
                Return SpecializedTasks.EmptyImmutableArray(Of Diagnostic)()
            End Function
        End Class
    End Class
End Namespace
