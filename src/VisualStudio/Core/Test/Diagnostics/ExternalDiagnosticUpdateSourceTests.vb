' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class ExternalDiagnosticUpdateSourceTests
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures

        <Fact>
        Public Sub TestExternalDiagnostics_SupportGetDiagnostics()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedId()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim analyzer = New AnalyzerForErrorLogTest()

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(
                    ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)))

                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()

                    Assert.True(source.IsSupportedDiagnosticId(project.Id, "ID1"))
                    Assert.False(source.IsSupportedDiagnosticId(project.Id, "CA1002"))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedDiagnosticId_Concurrent()
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()

                    Parallel.For(0, 100, Sub(i As Integer) source.IsSupportedDiagnosticId(project.Id, "CS1002"))
                End Using
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_DuplicatedError() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(globalOptions, ImmutableArray.Create(diagnostic))
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                    map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                    source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()

                    Dim buildOnlyDiagnosticService = workspace.Services.GetRequiredService(Of IBuildOnlyDiagnosticsService)
                    Assert.Empty(Await buildOnlyDiagnosticService.GetBuildOnlyDiagnosticsAsync(project.DocumentIds.First(), CancellationToken.None))

                    Dim diagnostics = source.GetBuildErrors()
                    Assert.Equal(2, diagnostics.Length)
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildStartEvent() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                    map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                    source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                    Await waiter.ExpeditedWaitAsync()

                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()
                    Assert.Equal(2, source.GetBuildErrors().Length)
                End Using
            End Using
        End Function

        <Fact>
        Public Sub TestExternalBuildErrorCustomTags()
            Assert.Equal(1, ProjectExternalErrorReporter.CustomTags.Count)
            Assert.Equal(WellKnownDiagnosticTags.Telemetry, ProjectExternalErrorReporter.CustomTags(0))
        End Sub

        <Fact>
        Public Sub TestExternalBuildErrorProperties()
            Assert.Equal(1, DiagnosticData.PropertiesForBuildDiagnostic.Count)

            Dim value As String = Nothing
            Assert.True(DiagnosticData.PropertiesForBuildDiagnostic.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, value))
            Assert.Equal(WellKnownDiagnosticTags.Build, value)

            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim project = workspace.CurrentSolution.Projects.First()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Dim waiter = New AsynchronousOperationListener()
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim diagnostic = New DiagnosticData(
                        "id",
                        category:="Test",
                        message:="Test Message",
                        severity:=DiagnosticSeverity.Error,
                        defaultSeverity:=DiagnosticSeverity.Error,
                        isEnabledByDefault:=True,
                        warningLevel:=0,
                        projectId:=project.Id,
                        location:=New DiagnosticDataLocation(New FileLinePositionSpan("", Nothing)),
                        customTags:=ImmutableArray(Of String).Empty,
                        properties:=DiagnosticData.PropertiesForBuildDiagnostic,
                        language:=LanguageNames.VisualBasic)
                    Assert.True(diagnostic.IsBuildDiagnostic())
                    source.AddNewErrors(project.Id, diagnostic)

                    diagnostic = New DiagnosticData(
                        "id",
                        category:="Test",
                        message:="Test Message",
                        severity:=DiagnosticSeverity.Error,
                        defaultSeverity:=DiagnosticSeverity.Error,
                        isEnabledByDefault:=True,
                        warningLevel:=0,
                        projectId:=project.Id,
                        location:=New DiagnosticDataLocation(New FileLinePositionSpan("", Nothing)),
                        customTags:=ImmutableArray(Of String).Empty,
                        properties:=ImmutableDictionary(Of String, String).Empty,
                        language:=LanguageNames.VisualBasic)
                    Assert.False(diagnostic.IsBuildDiagnostic())
#If DEBUG Then
                    Assert.Throws(Of InvalidOperationException)(Sub() source.AddNewErrors(project.Id, diagnostic))
#End If
                End Using
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_AddDuplicatedErrors() As Task
            Using workspace = EditorTestWorkspace.CreateCSharp(String.Empty)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(globalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    ' we shouldn't crash here
                    source.AddNewErrors(project.Id, diagnostic)
                    source.AddNewErrors(project.Id, diagnostic)

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()
                    Dim diagnostics = source.GetBuildErrors()
                    Assert.Equal(1, diagnostics.Length)
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationAnalyzer() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim analyzerReference = New AnalyzerImageReference(New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registration = service.CreateIncrementalAnalyzer(workspace)

                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim diagnostic = GetDiagnosticData(project.Id, id:=analyzer.SupportedDiagnostics(0).Id)
                    source.AddNewErrors(project.Id, diagnostic)

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()

                    Dim diagnostics = source.GetBuildErrors()

                    Assert.Equal(1, diagnostics.Length)
                    Assert.Equal(diagnostics(0).Properties(WellKnownDiagnosticPropertyNames.Origin), WellKnownDiagnosticTags.Build)
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationAnalyzerWithFSAOn() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                ' turn on FSA
                workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution)

                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim analyzerReference = New AnalyzerImageReference(New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim waiter = New AsynchronousOperationListener()
                Dim project = workspace.CurrentSolution.Projects.First()

                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registration = service.CreateIncrementalAnalyzer(workspace)

                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim diagnostic = GetDiagnosticData(project.Id, id:=analyzer.SupportedDiagnostics(0).Id)
                    source.AddNewErrors(project.Id, diagnostic)

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()
                    Dim diagnostics = source.GetBuildErrors()
                    Assert.NotEmpty(diagnostics)
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestCompilerDiagnosticWithoutDocumentId() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
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
                Using source = New ExternalErrorDiagnosticUpdateSource(
                    workspace, service, workspace.GetService(Of IGlobalOperationNotificationService), waiter, CancellationToken.None)

                    Dim diagnostic = New DiagnosticData(
                        id:="CS1002",
                        category:="Test",
                        message:="Test Message",
                        severity:=DiagnosticSeverity.Error,
                        defaultSeverity:=DiagnosticSeverity.Error,
                        isEnabledByDefault:=True,
                        warningLevel:=0,
                        customTags:=ImmutableArray(Of String).Empty,
                        properties:=DiagnosticData.PropertiesForBuildDiagnostic,
                        project.Id,
                        location:=New DiagnosticDataLocation(New FileLinePositionSpan("Test.txt", New LinePosition(4, 4), New LinePosition(4, 4)), documentId:=Nothing),
                        language:=project.Language)

                    'AddHandler service.DiagnosticsUpdated, Sub(o, argsCollection)
                    '                                           Dim args = argsCollection.Single()
                    '                                           Dim diagnostics = args.Diagnostics

                    '                                           Assert.Single(diagnostics)
                    '                                           Assert.Equal(diagnostics(0).Id, diagnostic.Id)
                    '                                       End Sub

                    source.AddNewErrors(project.Id, diagnostic)
                    Await waiter.ExpeditedWaitAsync()

                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()

                    Dim diagnosticServiceWaiter = TryCast(listenerProvider.GetListener(FeatureAttribute.DiagnosticService), AsynchronousOperationListener)
                    Await diagnosticServiceWaiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        Private Class CompilationEndAnalyzer
            Inherits DiagnosticAnalyzer

            Public ReadOnly Descriptor As DiagnosticDescriptor

            Public Sub New(hasCompilationEndTag As Boolean)
                Dim additionalCustomTags = If(hasCompilationEndTag, {WellKnownDiagnosticTags.CompilationEnd}, Array.Empty(Of String))
                Descriptor = DescriptorFactory.CreateSimpleDescriptor("CompilationEndAnalyzer", additionalCustomTags)
            End Sub
            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCompilationStartAction(
                    Sub(startContext)
                        startContext.RegisterCompilationEndAction(
                            Sub(endContext)
                                ' do nothing
                            End Sub)
                    End Sub)
            End Sub
        End Class

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
                severity:=DiagnosticSeverity.Error,
                defaultSeverity:=DiagnosticSeverity.Error,
                isEnabledByDefault:=True,
                warningLevel:=0,
                projectId:=projectId,
                location:=New DiagnosticDataLocation(New FileLinePositionSpan("", Nothing)),
                customTags:=ImmutableArray(Of String).Empty,
                properties:=DiagnosticData.PropertiesForBuildDiagnostic,
                language:=LanguageNames.VisualBasic)
        End Function

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService

            Private ReadOnly _analyzerInfoCache As DiagnosticAnalyzerInfoCache

            Public ReadOnly Property GlobalOptions As IGlobalOptionService Implements IDiagnosticAnalyzerService.GlobalOptions

            Public Sub New(globalOptions As IGlobalOptionService, Optional data As ImmutableArray(Of DiagnosticData) = Nothing)
                _analyzerInfoCache = New DiagnosticAnalyzerInfoCache()
                Me.GlobalOptions = globalOptions
            End Sub

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
    End Class
End Namespace
