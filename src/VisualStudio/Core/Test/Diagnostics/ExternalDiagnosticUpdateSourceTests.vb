' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.IO.Pipelines
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.ServiceHub.Framework
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
Imports Microsoft.VisualStudio.RpcContracts.DiagnosticManagement
Imports Microsoft.VisualStudio.Shell.ServiceBroker
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class ExternalDiagnosticUpdateSourceTests
        Private Shared ReadOnly s_composition As TestComposition = VisualStudioTestCompositions.LanguageServices.AddParts(
            GetType(TestServiceBroker),
            GetType(MockServiceProvider),
            GetType(StubVsServiceExporter(Of )),
            GetType(StubVsServiceExporter(Of ,)),
            GetType(MockVisualStudioWorkspace),
            GetType(ProjectCodeModelFactory))

        Private Shared ReadOnly s_projectGuid As Guid = Guid.NewGuid()

        <WpfFact>
        Public Async Function TestExternalDiagnostics_SupportedId() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)
                Dim analyzer = New AnalyzerForErrorLogTest()

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(
                    ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)))

                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim threadingContext = workspace.ExportProvider.GetExport(Of IThreadingContext).Value

                Dim service = New TestDiagnosticAnalyzerService()
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()
                    Await waiter.ExpeditedWaitAsync()

                    Assert.True(source.IsSupportedDiagnosticId(project.Id, "ID1"))
                    Assert.False(source.IsSupportedDiagnosticId(project.Id, "CA1002"))
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function TestExternalDiagnostics_SupportedDiagnosticId_Concurrent() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)
                Dim service = New TestDiagnosticAnalyzerService()
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()
                    Await waiter.ExpeditedWaitAsync()

                    Parallel.For(0, 100, Sub(i As Integer) source.IsSupportedDiagnosticId(project.Id, "CS1002"))
                End Using
            End Using
        End Function

        <WpfFact>
        Public Sub TestExternalDiagnostics_SupportedIdFalseIfBuildNotStarted()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)
                Dim analyzer = New AnalyzerForErrorLogTest()

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(
                    ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)))

                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim threadingContext = workspace.ExportProvider.GetExport(Of IThreadingContext).Value

                Dim service = New TestDiagnosticAnalyzerService()
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    Dim project = workspace.CurrentSolution.Projects.First()

                    Assert.False(source.IsSupportedDiagnosticId(project.Id, "ID1"))
                    Assert.False(source.IsSupportedDiagnosticId(project.Id, "CA1002"))
                End Using
            End Using
        End Sub

        <WpfFact>
        Public Async Function TestExternalDiagnosticsReported() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService()
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim testServiceBroker = workspace.ExportProvider.GetExportedValue(Of TestServiceBroker)
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    source.OnSolutionBuildStarted()
                    Await waiter.ExpeditedWaitAsync()
                    Assert.True(testServiceBroker.DiagnosticManagerService.AllDiagnosticsCleared)

                    Dim diagnostics = {diagnostic, GetDiagnosticData(project.Id)}.ToImmutableArray()

                    source.AddNewErrors(project.Id, s_projectGuid, diagnostics)
                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()

                    Assert.Equal(2, testServiceBroker.DiagnosticManagerService.AllDiagnostics.Count)
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function TestOnlySupportsBuildErrors() As Task
            Assert.Equal(1, DiagnosticData.PropertiesForBuildDiagnostic.Count)

            Dim value As String = Nothing
            Assert.True(DiagnosticData.PropertiesForBuildDiagnostic.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, value))
            Assert.Equal(WellKnownDiagnosticTags.Build, value)

            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim service = New TestDiagnosticAnalyzerService()
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim testServiceBroker = workspace.ExportProvider.GetExportedValue(Of TestServiceBroker)
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    source.OnSolutionBuildStarted()
                    Await waiter.ExpeditedWaitAsync()
                    Assert.True(testServiceBroker.DiagnosticManagerService.AllDiagnosticsCleared)

                    Dim diagnostic = New DiagnosticData(
                        "id",
                        category:="Test",
                        message:="Test Message",
                        severity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                        defaultSeverity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                        isEnabledByDefault:=True,
                        warningLevel:=0,
                        projectId:=project.Id,
                        location:=New DiagnosticDataLocation(New FileLinePositionSpan("C:\DocumentDiagnostic", Nothing)),
                        customTags:=ImmutableArray(Of String).Empty,
                        properties:=ImmutableDictionary(Of String, String).Empty,
                        language:=LanguageNames.VisualBasic)
                    Assert.False(diagnostic.IsBuildDiagnostic())
#If DEBUG Then
                    Assert.Throws(Of InvalidOperationException)(Sub() source.AddNewErrors(project.Id, s_projectGuid, {diagnostic}.ToImmutableArray()))
#End If
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function TestExternalDiagnostics_AddDuplicatedErrors() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim waiter = workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.ErrorList)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService()
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim testServiceBroker = workspace.ExportProvider.GetExportedValue(Of TestServiceBroker)
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    source.OnSolutionBuildStarted()
                    Await waiter.ExpeditedWaitAsync()
                    Assert.True(testServiceBroker.DiagnosticManagerService.AllDiagnosticsCleared)

                    ' we shouldn't crash here
                    source.AddNewErrors(project.Id, s_projectGuid, {diagnostic}.ToImmutableArray())
                    source.AddNewErrors(project.Id, s_projectGuid, {diagnostic}.ToImmutableArray())

                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()

                    Assert.Equal(2, testServiceBroker.DiagnosticManagerService.AllDiagnostics.Count)
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function TestCompilerDiagnosticWithoutDocumentId() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty, composition:=s_composition)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim analyzerReference = New AnalyzerImageReference(New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)()
                Dim waiter = TryCast(listenerProvider.GetListener(FeatureAttribute.ErrorList), AsynchronousOperationListener)

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registration = service.CreateIncrementalAnalyzer(workspace)
                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)
                Dim testServiceBroker = workspace.ExportProvider.GetExportedValue(Of TestServiceBroker)
                Dim vsWorkspace = workspace.ExportProvider.GetExportedValue(Of MockVisualStudioWorkspace)()
                vsWorkspace.SetWorkspace(workspace)
                Using source = workspace.ExportProvider.GetExportedValue(Of ExternalErrorDiagnosticUpdateSource)()

                    Dim diagnostic = New DiagnosticData(
                        id:="CS1002",
                        category:="Test",
                        message:="Test Message",
                        severity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                        defaultSeverity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                        isEnabledByDefault:=True,
                        warningLevel:=0,
                        customTags:=ImmutableArray(Of String).Empty,
                        properties:=DiagnosticData.PropertiesForBuildDiagnostic,
                        project.Id,
                        location:=New DiagnosticDataLocation(New FileLinePositionSpan("C:\ProjectDiagnostic", New LinePosition(4, 4), New LinePosition(4, 4)), documentId:=Nothing),
                        language:=project.Language)

                    source.AddNewErrors(project.Id, s_projectGuid, {diagnostic}.ToImmutableArray())
                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        Private Class CompilationAnalyzer
            Inherits DiagnosticAnalyzer

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DescriptorFactory.CreateSimpleDescriptor("CompilationAnalyzer"))
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCompilationAction(
                    Sub(compilationContext)
                        ' do nothing
                    End Sub)
            End Sub
        End Class

        Private Shared Function GetDiagnosticData(projectId As ProjectId, Optional id As String = "id") As DiagnosticData
            Return New DiagnosticData(
                id,
                category:="Test",
                message:="Test Message",
                severity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                defaultSeverity:=Microsoft.CodeAnalysis.DiagnosticSeverity.Error,
                isEnabledByDefault:=True,
                warningLevel:=0,
                projectId:=projectId,
                location:=New DiagnosticDataLocation(New FileLinePositionSpan("C:\DocumentDiagnostic", Nothing)),
                customTags:=ImmutableArray(Of String).Empty,
                properties:=DiagnosticData.PropertiesForBuildDiagnostic,
                language:=LanguageNames.VisualBasic)
        End Function

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService

            Private ReadOnly _analyzerInfoCache As DiagnosticAnalyzerInfoCache = New DiagnosticAnalyzerInfoCache()

            Public ReadOnly Property AnalyzerInfoCache As DiagnosticAnalyzerInfoCache Implements IDiagnosticAnalyzerService.AnalyzerInfoCache
                Get
                    Return _analyzerInfoCache
                End Get
            End Property

            Public Sub RequestDiagnosticRefresh() Implements IDiagnosticAnalyzerService.RequestDiagnosticRefresh
            End Sub

            Public Function GetDiagnosticsForSpanAsync(document As TextDocument, range As TextSpan?, shouldIncludeDiagnostic As Func(Of String, Boolean), includeCompilerDiagnostics As Boolean, includeSuppressedDiagnostics As Boolean, priority As ICodeActionRequestPriorityProvider, diagnosticKinds As DiagnosticKind, isExplicit As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, includeSuppressedDiagnostics As Boolean, includeLocalDocumentDiagnostics As Boolean, includeNonLocalDocumentDiagnostics As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsForIdsAsync(solution As Solution, projectId As ProjectId, documentId As DocumentId, diagnosticIds As ImmutableHashSet(Of String), shouldIncludeAnalyzer As Func(Of DiagnosticAnalyzer, Boolean), getDocuments As Func(Of Project, DocumentId, IReadOnlyList(Of DocumentId)), includeSuppressedDiagnostics As Boolean, includeLocalDocumentDiagnostics As Boolean, includeNonLocalDocumentDiagnostics As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetProjectDiagnosticsForIdsAsync(solution As Solution, projectId As ProjectId, diagnosticIds As ImmutableHashSet(Of String), shouldIncludeAnalyzer As Func(Of DiagnosticAnalyzer, Boolean), includeSuppressedDiagnostics As Boolean, includeNonLocalDocumentDiagnostics As Boolean, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function ForceAnalyzeProjectAsync(project As Project, cancellationToken As CancellationToken) As Task Implements IDiagnosticAnalyzerService.ForceAnalyzeProjectAsync
                Throw New NotImplementedException()
            End Function

            Public Function TryGetDiagnosticsForSpanAsync(document As TextDocument, range As TextSpan, shouldIncludeDiagnostic As Func(Of String, Boolean), includeSuppressedDiagnostics As Boolean, priority As ICodeActionRequestPriorityProvider, diagnosticKinds As DiagnosticKind, isExplicit As Boolean, cancellationToken As CancellationToken) As Task(Of (diagnostics As ImmutableArray(Of DiagnosticData), upToDate As Boolean)) Implements IDiagnosticAnalyzerService.TryGetDiagnosticsForSpanAsync
                Return Task.FromResult((ImmutableArray(Of DiagnosticData).Empty, False))
            End Function
        End Class

        <PartNotDiscoverable>
        <Export(GetType(SVsFullAccessServiceBroker))>
        <Export(GetType(TestServiceBroker))>
        Private Class TestServiceBroker
            Implements IServiceBroker

            Friend DiagnosticManagerService As DiagnosticManagerService = New DiagnosticManagerService()

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Event AvailabilityChanged As EventHandler(Of BrokeredServicesChangedEventArgs) Implements IServiceBroker.AvailabilityChanged

            Public Function GetProxyAsync(Of T As Class)(serviceDescriptor As ServiceRpcDescriptor, Optional options As ServiceActivationOptions = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of T) Implements IServiceBroker.GetProxyAsync
                If (GetType(T) Is GetType(IDiagnosticManagerService)) Then
                    Return New ValueTask(Of T)(Task.FromResult(CType(CType(DiagnosticManagerService, Object), T)))
                End If

                Throw New InvalidOperationException()
            End Function

            Public Function GetPipeAsync(serviceMoniker As ServiceMoniker, Optional options As ServiceActivationOptions = Nothing, Optional cancellationToken As CancellationToken = Nothing) As ValueTask(Of IDuplexPipe) Implements IServiceBroker.GetPipeAsync
                Throw New NotImplementedException()
            End Function
        End Class

        Private Class DiagnosticManagerService
            Implements IDiagnosticManagerService

            Friend DiagnosticsCleared As Boolean = False
            Friend AllDiagnosticsCleared As Boolean = False
            Friend AllDiagnostics As List(Of RpcContracts.DiagnosticManagement.Diagnostic) = New List(Of RpcContracts.DiagnosticManagement.Diagnostic)()

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub

            Public Function SetDiagnosticsAsync(generatorId As String, diagnostics As IReadOnlyList(Of DiagnosticCollection), cancellationToken As CancellationToken) As Task Implements IDiagnosticManagerService.SetDiagnosticsAsync
                Throw New NotImplementedException()
            End Function

            Public Function AppendDiagnosticsAsync(generatorId As String, diagnostics As IReadOnlyList(Of DiagnosticCollection), cancellationToken As CancellationToken) As Task Implements IDiagnosticManagerService.AppendDiagnosticsAsync
                For Each collection In diagnostics
                    For Each diagnostic In collection.Diagnostics
                        AllDiagnostics.Add(diagnostic)
                    Next
                Next
                Return Task.CompletedTask
            End Function

            Public Function ClearDiagnosticsAsync(generatorId As String, cancellationToken As CancellationToken) As Task Implements IDiagnosticManagerService.ClearDiagnosticsAsync
                If (DiagnosticsCleared) Then
                    Throw New InvalidOperationException()
                End If
                DiagnosticsCleared = True
                Return Task.CompletedTask
            End Function

            Public Function ClearAllDiagnosticsAsync(cancellationToken As CancellationToken) As Task Implements IDiagnosticManagerService.ClearAllDiagnosticsAsync
                If (AllDiagnosticsCleared) Then
                    Throw New InvalidOperationException()
                End If
                AllDiagnosticsCleared = True
                Return Task.CompletedTask
            End Function

            Public Function AddBuildOnlyDiagnosticCodesAsync(diagnosticCodes As IReadOnlyList(Of String), cancellationToken As CancellationToken) As Task Implements IDiagnosticManagerService.AddBuildOnlyDiagnosticCodesAsync
                Throw New NotImplementedException()
            End Function
        End Class

    End Class
End Namespace
