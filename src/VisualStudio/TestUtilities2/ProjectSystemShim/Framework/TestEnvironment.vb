' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.ComponentModel.Composition.Hosting
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.Telemetry
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework

    ''' <summary>
    ''' Class that holds onto the project tracker, fake IServiceProvider, and other interfaces needed to
    ''' unit test the project system shims outside of Visual Studio.
    ''' </summary>
    Friend Class TestEnvironment
        Implements IDisposable

        ' TODO:
        ' Use VisualStudioTestComposition.LanguageServices instead, With mocked services replaced With test mocks.
        '   
        '    WithoutParts(
        '        GetType(VisualStudioWorkspaceImpl),
        '        GetType(IServiceProvider),
        '        GetType(IDiagnosticUpdateSourceRegistrationService)).
        '    WithAdditionalParts(
        '        GetType(MockVisualStudioWorkspace),
        '        GetType(MockServiceProvider),
        '        GetType(MockDiagnosticUpdateSourceRegistrationService),
        '        GetType(MockWorkspaceEventListenerProvider))

        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddParts(
                GetType(FileChangeWatcherProvider),
                GetType(MockVisualStudioWorkspace),
                GetType(VisualStudioProjectFactory),
                GetType(MockVisualStudioDiagnosticAnalyzerProviderFactory),
                GetType(MockServiceProvider),
                GetType(StubVsServiceExporter(Of )),
                GetType(StubVsServiceExporter(Of ,)),
                GetType(SolutionEventsBatchScopeCreator),
                GetType(ProjectCodeModelFactory),
                GetType(CPSProjectFactory),
                GetType(VisualStudioRuleSetManagerFactory),
                GetType(VisualStudioMetadataServiceFactory),
                GetType(VisualStudioMetadataReferenceManager),
                GetType(MockWorkspaceEventListenerProvider),
                GetType(HierarchyItemToProjectIdMap),
                GetType(DiagnosticAnalyzerService),
                GetType(VisualStudioWorkspaceTelemetryService),
                GetType(OpenTextBufferProvider),
                GetType(StubVsEditorAdaptersFactoryService),
                GetType(ExternalErrorDiagnosticUpdateSource),
                GetType(MockServiceBroker))

        Private ReadOnly _workspace As VisualStudioWorkspaceImpl
        Private ReadOnly _projectFilePaths As New List(Of String)

        Public Sub New(ParamArray extraParts As Type())
            Dim composition = s_composition.AddParts(extraParts)

            ExportProvider = composition.ExportProviderFactory.CreateExportProvider()
            _workspace = ExportProvider.GetExportedValue(Of VisualStudioWorkspaceImpl)
            ThreadingContext = ExportProvider.GetExportedValue(Of IThreadingContext)()
            Implementation.Interop.WrapperPolicy.s_ComWrapperFactory = MockComWrapperFactory.Instance

            ServiceProvider = ExportProvider.GetExportedValue(Of MockServiceProvider)()
        End Sub

        Public ReadOnly Property ProjectFactory As VisualStudioProjectFactory
            Get
                Return ExportProvider.GetExportedValue(Of VisualStudioProjectFactory)
            End Get
        End Property

        <PartNotDiscoverable>
        <Export(GetType(VisualStudioWorkspace))>
        <Export(GetType(VisualStudioWorkspaceImpl))>
        Private Class MockVisualStudioWorkspace
            Inherits VisualStudioWorkspaceImpl

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New(exportProvider As Composition.ExportProvider)
                MyBase.New(exportProvider,
                           exportProvider.GetExportedValue(Of MockServiceProvider))
            End Sub

            Public Overrides Sub DisplayReferencedSymbols(solution As Solution, referencedSymbols As IEnumerable(Of ReferencedSymbol))
                Throw New NotImplementedException()
            End Sub

            Public Overrides Function TryGoToDefinition(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean
                Throw New NotImplementedException()
            End Function

            Public Overrides Function TryGoToDefinitionAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of Boolean)
                Throw New NotImplementedException()
            End Function

            Public Overrides Function TryFindAllReferences(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function OpenInvisibleEditor(documentId As DocumentId) As IInvisibleEditor
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function GetBrowseObjectAsync(symbolListItem As SymbolListItem, cancellationToken As CancellationToken) As Task(Of Object)
                Throw New NotImplementedException()
            End Function
        End Class

        Public ReadOnly Property ThreadingContext As IThreadingContext
        Public ReadOnly Property ServiceProvider As IServiceProvider
        Public ReadOnly Property ExportProvider As Composition.ExportProvider

        Public ReadOnly Property Workspace As VisualStudioWorkspaceImpl
            Get
                Return _workspace
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            _workspace.Dispose()

            For Each filePath In _projectFilePaths
                File.Delete(filePath)
                Directory.Delete(Path.GetDirectoryName(filePath))
            Next
        End Sub

        Private Function CreateProjectFile(projectName As String) As String
            Dim dir = Path.Combine(Path.GetTempPath, Guid.NewGuid().ToString("N"))
            Directory.CreateDirectory(dir)
            Dim result = Path.Combine(dir, projectName + ".vbproj")
            File.WriteAllText(result, "<Project></Project>")
            _projectFilePaths.Add(result)
            Return result
        End Function

        Public Function CreateHierarchy(projectName As String, projectBinPath As String, projectRefPath As String, projectCapabilities As String) As IVsHierarchy
            Return New MockHierarchy(projectName, CreateProjectFile(projectName), projectBinPath, projectRefPath, projectCapabilities)
        End Function

        Public Function GetUpdatedCompilationOptionOfSingleProject() As CompilationOptions
            Return Workspace.CurrentSolution.Projects.Single().CompilationOptions
        End Function

        Friend Async Function GetFileChangeServiceAsync() As Task(Of MockVsFileChangeEx)
            ' Ensure we've pushed everything to the file change watcher
            Dim fileChangeProvider = ExportProvider.GetExportedValue(Of FileChangeWatcherProvider)
            Dim mockFileChangeService = Assert.IsType(Of MockVsFileChangeEx)(ServiceProvider.GetService(GetType(SVsFileChangeEx)))
            Await ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
            Return mockFileChangeService
        End Function

        Friend Async Function RaiseFileChangeAsync(path As String) As Task
            Dim service = Await GetFileChangeServiceAsync()
            service.FireUpdate(path)
        End Function

        ''' <inheritdoc cref="MockVsFileChangeEx.FireStaleUpdate(String, Action)" />
        Friend Async Function RaiseStaleFileChangeAsync(path As String, unsubscribingAction As Action) As Task
            Dim service = Await GetFileChangeServiceAsync()
            service.FireStaleUpdate(path, unsubscribingAction)
        End Function
    End Class
End Namespace
