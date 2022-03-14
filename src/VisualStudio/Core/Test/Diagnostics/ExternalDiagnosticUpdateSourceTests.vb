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
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddExcludedPartTypes(GetType(IDiagnosticUpdateSourceRegistrationService)) _
            .AddParts(GetType(MockDiagnosticUpdateSourceRegistrationService))

        <Fact>
        Public Sub TestExternalDiagnostics_SupportGetDiagnostics()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)
                    Assert.False(source.SupportGetDiagnostics)
                End Using
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_RaiseEvents() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim project = workspace.CurrentSolution.Projects.First()
                    Dim diagnostic = GetDiagnosticData(project.Id)

                    Dim expected = 1
                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              Dim diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)
                                                              Assert.Equal(expected, diagnostics.Length)
                                                              If expected = 1 Then
                                                                  Assert.Equal(diagnostics(0), diagnostic)
                                                              End If
                                                          End Sub

                    source.AddNewErrors(project.DocumentIds.First(), diagnostic)
                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()

                    expected = 0
                    source.ClearErrors(project.Id)
                    Await waiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedId()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim analyzer = New AnalyzerForErrorLogTest()

                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(
                    ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)).Empty.Add(LanguageNames.CSharp, ImmutableArray.Create(Of DiagnosticAnalyzer)(analyzer)))

                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()

                    Assert.True(source.IsSupportedDiagnosticId(project.Id, "ID1"))
                    Assert.False(source.IsSupportedDiagnosticId(project.Id, "CA1002"))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedDiagnosticId_Concurrent()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim project = workspace.CurrentSolution.Projects.First()
                    source.OnSolutionBuildStarted()

                    Parallel.For(0, 100, Sub(i As Integer) source.IsSupportedDiagnosticId(project.Id, "CS1002"))
                End Using
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_DuplicatedError() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(globalOptions, ImmutableArray.Create(diagnostic))
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                    map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                    source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)

                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              Dim diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)
                                                              Assert.Equal(1, diagnostics.Length)
                                                          End Sub

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildStartEvent() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)
                    AddHandler source.BuildProgressChanged, Sub(o, progress)
                                                                If progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Done Then
                                                                    Assert.Equal(2, source.GetBuildErrors().Length)
                                                                End If
                                                            End Sub

                    Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                    map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                    source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                    Await waiter.ExpeditedWaitAsync()

                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()
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

            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim project = workspace.CurrentSolution.Projects.First()

                Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True)
                Assert.True(diagnostic.IsBuildDiagnostic())

                diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=False)
                Assert.False(diagnostic.IsBuildDiagnostic())
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_AddDuplicatedErrors() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(globalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    ' we shouldn't crash here
                    source.AddNewErrors(project.Id, diagnostic)
                    source.AddNewErrors(project.Id, diagnostic)

                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              Dim diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)
                                                              Assert.Equal(1, diagnostics.Length)
                                                          End Sub

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        <Theory, CombinatorialData>
        <WorkItem(47754, "https://github.com/dotnet/roslyn/issues/47754")>
        Public Async Function TestExternalDiagnostics_CompilationEndAnalyzer(hasCompilationEndTag As Boolean) As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim analyzer = New CompilationEndAnalyzer(hasCompilationEndTag)
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim analyzerReference = New AnalyzerImageReference(New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()

                Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                    source.AddNewErrors(project.Id, diagnostic)

                    Dim buildDiagnosticCallbackSeen = False
                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              buildDiagnosticCallbackSeen = True

                                                              Dim diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)
                                                              Assert.Equal(1, diagnostics.Length)
                                                              Assert.Equal(diagnostics(0).Properties(WellKnownDiagnosticPropertyNames.Origin), WellKnownDiagnosticTags.Build)
                                                          End Sub

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()

                    Assert.Equal(hasCompilationEndTag, buildDiagnosticCallbackSeen)
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

                Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                    source.AddNewErrors(project.Id, diagnostic)

                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              Dim diagnostics = a.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)

                                                              Assert.Equal(1, diagnostics.Length)
                                                              Assert.Equal(diagnostics(0).Properties(WellKnownDiagnosticPropertyNames.Origin), WellKnownDiagnosticTags.Build)
                                                          End Sub

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationAnalyzerWithFSAOn() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                ' turn on FSA
                workspace.GlobalOptions.SetGlobalOption(New OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution)

                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim analyzerReference = New AnalyzerImageReference(New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim waiter = New AsynchronousOperationListener()
                Dim project = workspace.CurrentSolution.Projects.First()

                Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                    source.AddNewErrors(project.Id, diagnostic)

                    Dim called = False
                    AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                              called = True
                                                          End Sub

                    source.OnSolutionBuildCompleted()

                    Await waiter.ExpeditedWaitAsync()

                    ' error is considered live error, so event shouldn't be raised
                    Assert.False(called)
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildProgressUpdated() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                workspace.AddTestProject(New TestHostProject(workspace, language:=LanguageNames.CSharp))

                Dim projectId1 = workspace.CurrentSolution.ProjectIds(0)
                Dim projectId2 = workspace.CurrentSolution.ProjectIds(1)

                Dim service = New TestDiagnosticAnalyzerService(workspace.GlobalOptions)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    source.AddNewErrors(projectId1, GetDiagnosticData(projectId1))
                    Await waiter.ExpeditedWaitAsync()

                    Dim numberOfUpdateCalls = 0
                    AddHandler source.BuildProgressChanged, Sub(o, progress)
                                                                If progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Updated Then
                                                                    numberOfUpdateCalls += 1
                                                                    Assert.Equal(numberOfUpdateCalls, source.GetBuildErrors().Length)
                                                                ElseIf progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Done Then
                                                                    Assert.Equal(2, source.GetBuildErrors().Length)
                                                                End If
                                                            End Sub

                    source.AddNewErrors(projectId2, GetDiagnosticData(projectId2))
                    Await waiter.ExpeditedWaitAsync()

                    source.OnSolutionBuildCompleted()
                    Await waiter.ExpeditedWaitAsync()
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

                Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim service = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)
                Using source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter, CancellationToken.None)

                    Dim diagnostic = New DiagnosticData(
                    id:="CS1002",
                    category:="Test",
                    message:="Test Message",
                    severity:=DiagnosticSeverity.Error,
                    defaultSeverity:=DiagnosticSeverity.Error,
                    isEnabledByDefault:=True,
                    warningLevel:=0,
                    customTags:=ImmutableArray(Of String).Empty,
                    properties:=ImmutableDictionary(Of String, String).Empty,
                    project.Id,
                    location:=New DiagnosticDataLocation(documentId:=Nothing, sourceSpan:=Nothing, "Test.txt", 4, 4),
                    language:=project.Language)

                    AddHandler service.DiagnosticsUpdated, Sub(o, args)
                                                               Dim diagnostics = args.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode)

                                                               Assert.Single(diagnostics)
                                                               Assert.Equal(diagnostics(0).Id, diagnostic.Id)
                                                           End Sub

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

            Private ReadOnly _descriptor As DiagnosticDescriptor

            Public Sub New(hasCompilationEndTag As Boolean)
                Dim additionalCustomTags = If(hasCompilationEndTag, {WellKnownDiagnosticTags.CompilationEnd}, Array.Empty(Of String))
                _descriptor = DescriptorFactory.CreateSimpleDescriptor("CompilationEndAnalyzer", additionalCustomTags)
            End Sub
            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(_descriptor)
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

        Private Shared Function GetDiagnosticData(projectId As ProjectId, Optional isBuildDiagnostic As Boolean = False, Optional id As String = "id") As DiagnosticData
            Dim properties = If(isBuildDiagnostic, DiagnosticData.PropertiesForBuildDiagnostic, ImmutableDictionary(Of String, String).Empty)
            Return New DiagnosticData(
                id,
                category:="Test",
                message:="Test Message",
                severity:=DiagnosticSeverity.Error,
                defaultSeverity:=DiagnosticSeverity.Error,
                isEnabledByDefault:=True,
                warningLevel:=0,
                projectId:=projectId,
                customTags:=ImmutableArray(Of String).Empty,
                properties:=properties,
                language:=LanguageNames.VisualBasic)
        End Function

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService, IDiagnosticUpdateSource

            Private ReadOnly _data As ImmutableArray(Of DiagnosticData)
            Private ReadOnly _analyzerInfoCache As DiagnosticAnalyzerInfoCache

            Public ReadOnly Property GlobalOptions As IGlobalOptionService Implements IDiagnosticAnalyzerService.GlobalOptions

            Public Sub New(globalOptions As IGlobalOptionService, Optional data As ImmutableArray(Of DiagnosticData) = Nothing)
                _data = data.NullToEmpty
                _analyzerInfoCache = New DiagnosticAnalyzerInfoCache()
                Me.GlobalOptions = globalOptions
            End Sub

            Public ReadOnly Property SupportGetDiagnostics As Boolean Implements IDiagnosticUpdateSource.SupportGetDiagnostics
                Get
                    Return True
                End Get
            End Property

            Public ReadOnly Property AnalyzerInfoCache As DiagnosticAnalyzerInfoCache Implements IDiagnosticAnalyzerService.AnalyzerInfoCache
                Get
                    Return _analyzerInfoCache
                End Get
            End Property

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticUpdateSource.DiagnosticsUpdated
            Public Event DiagnosticsCleared As EventHandler Implements IDiagnosticUpdateSource.DiagnosticsCleared

            Public Function GetDiagnosticsAsync(workspace As Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, includeSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As ValueTask(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticUpdateSource.GetDiagnosticsAsync
                Return New ValueTask(Of ImmutableArray(Of DiagnosticData))(If(includeSuppressedDiagnostics, _data, _data.WhereAsArray(Function(d) Not d.IsSuppressed)))
            End Function

            Public Sub Reanalyze(workspace As Workspace, Optional projectIds As IEnumerable(Of ProjectId) = Nothing, Optional documentIds As IEnumerable(Of DocumentId) = Nothing, Optional highPriority As Boolean = False) Implements IDiagnosticAnalyzerService.Reanalyze
            End Sub

            Public Function GetDiagnosticsForSpanAsync(document As Document, range As TextSpan?, shouldIncludeDiagnostic As Func(Of String, Boolean), Optional includeSuppressedDiagnostics As Boolean = False, Optional priority As CodeActionRequestPriority = CodeActionRequestPriority.None, Optional addOperationScope As Func(Of String, IDisposable) = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)
            End Function

            Public Function TryAppendDiagnosticsForSpanAsync(document As Document, range As TextSpan, diagnostics As ArrayBuilder(Of DiagnosticData), Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDiagnosticAnalyzerService.TryAppendDiagnosticsForSpanAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetSpecificCachedDiagnosticsAsync(workspace As Workspace, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Workspace, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetProjectDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function ContainsDiagnostics(workspace As Workspace, projectId As ProjectId) As Boolean Implements IDiagnosticAnalyzerService.ContainsDiagnostics
                Throw New NotImplementedException()
            End Function

            Public Function ForceAnalyzeAsync(solution As Solution, onProjectAnalyzed As Action(Of Project), Optional projectId As ProjectId = Nothing, Optional cancellationToken As CancellationToken = Nothing) As Task Implements IDiagnosticAnalyzerService.ForceAnalyzeAsync
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
