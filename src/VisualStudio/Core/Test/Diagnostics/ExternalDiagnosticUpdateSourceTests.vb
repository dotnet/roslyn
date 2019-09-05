' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class ExternalDiagnosticUpdateSourceTests
        <Fact>
        Public Sub TestExternalDiagnostics_SupportGetDiagnostics()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Assert.False(source.SupportGetDiagnostics)
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_RaiseEvents() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim expected = 1
                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(expected, a.Diagnostics.Length)
                                                          If expected = 1 Then
                                                              Assert.Equal(a.Diagnostics(0), diagnostic)
                                                          End If
                                                      End Sub

                source.AddNewErrors(project.DocumentIds.First(), diagnostic)
                source.OnSolutionBuildCompleted()
                Await waiter.CreateExpeditedWaitTask()

                expected = 0
                source.ClearErrors(project.Id)
                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedId()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                source.OnSolutionBuildStarted()

                Assert.True(source.IsSupportedDiagnosticId(project.Id, "CS1002"))
                Assert.False(source.IsSupportedDiagnosticId(project.Id, "CA1002"))
            End Using
        End Sub

        <Fact>
        Public Sub TestExternalDiagnostics_SupportedDiagnosticId_Concurrent()
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()
                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim project = workspace.CurrentSolution.Projects.First()
                source.OnSolutionBuildStarted()

                Parallel.For(0, 100, Sub(i As Integer) source.IsSupportedDiagnosticId(project.Id, "CS1002"))
            End Using
        End Sub

        <Fact>
        Public Async Function TestExternalDiagnostics_DuplicatedError() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray.Create(Of DiagnosticData)(diagnostic))
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)

                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(1, a.Diagnostics.Length)
                                                      End Sub

                source.OnSolutionBuildCompleted()

                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildStartEvent() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray(Of DiagnosticData).Empty)
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)
                AddHandler source.BuildProgressChanged, Sub(o, progress)
                                                            If progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Done Then
                                                                Assert.Equal(2, source.GetBuildErrors().Length)
                                                            End If
                                                        End Sub

                Dim map = New Dictionary(Of DocumentId, HashSet(Of DiagnosticData))()
                map.Add(project.DocumentIds.First(), New HashSet(Of DiagnosticData)(
                        SpecializedCollections.SingletonEnumerable(GetDiagnosticData(project.Id))))

                source.AddNewErrors(project.Id, New HashSet(Of DiagnosticData)(SpecializedCollections.SingletonEnumerable(diagnostic)), map)
                Await waiter.CreateExpeditedWaitTask()

                source.OnSolutionBuildCompleted()
                Await waiter.CreateExpeditedWaitTask()
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
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()
                Dim diagnostic = GetDiagnosticData(project.Id)

                Dim service = New TestDiagnosticAnalyzerService()
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                ' we shouldn't crash here
                source.AddNewErrors(project.Id, diagnostic)
                source.AddNewErrors(project.Id, diagnostic)

                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(1, a.Diagnostics.Length)
                                                      End Sub

                source.OnSolutionBuildCompleted()

                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationEndAnalyzer() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim analyzer = New CompilationEndAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim service = New Microsoft.CodeAnalysis.Diagnostics.TestDiagnosticAnalyzerService(LanguageNames.CSharp, New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                source.AddNewErrors(project.Id, diagnostic)

                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(1, a.Diagnostics.Length)
                                                          Assert.Equal(a.Diagnostics(0).Properties(WellKnownDiagnosticPropertyNames.Origin), WellKnownDiagnosticTags.Build)
                                                      End Sub

                source.OnSolutionBuildCompleted()

                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationAnalyzer() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim service = New Microsoft.CodeAnalysis.Diagnostics.TestDiagnosticAnalyzerService(LanguageNames.CSharp, New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                source.AddNewErrors(project.Id, diagnostic)

                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          Assert.Equal(1, a.Diagnostics.Length)
                                                          Assert.Equal(a.Diagnostics(0).Properties(WellKnownDiagnosticPropertyNames.Origin), WellKnownDiagnosticTags.Build)
                                                      End Sub

                source.OnSolutionBuildCompleted()

                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestExternalDiagnostics_CompilationAnalyzerWithFSAOn() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                ' turn on FSA
                workspace.Options = workspace.Options.WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, True)

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim service = New Microsoft.CodeAnalysis.Diagnostics.TestDiagnosticAnalyzerService(LanguageNames.CSharp, New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray())
                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim diagnostic = GetDiagnosticData(project.Id, isBuildDiagnostic:=True, id:=analyzer.SupportedDiagnostics(0).Id)
                source.AddNewErrors(project.Id, diagnostic)

                Dim called = False
                AddHandler source.DiagnosticsUpdated, Sub(o, a)
                                                          called = True
                                                      End Sub

                source.OnSolutionBuildCompleted()

                Await waiter.CreateExpeditedWaitTask()

                ' error is considered live error, so event shouldn't be raised
                Assert.False(called)
            End Using
        End Function

        <Fact>
        Public Async Function TestBuildProgressUpdated() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim waiter = New AsynchronousOperationListener()

                workspace.AddTestProject(New TestHostProject(workspace, language:=LanguageNames.CSharp))

                Dim projectId1 = workspace.CurrentSolution.ProjectIds(0)
                Dim projectId2 = workspace.CurrentSolution.ProjectIds(1)

                Dim service = New TestDiagnosticAnalyzerService(ImmutableArray(Of DiagnosticData).Empty)
                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                source.AddNewErrors(projectId1, GetDiagnosticData(projectId1))
                Await waiter.CreateExpeditedWaitTask()

                AddHandler source.BuildProgressChanged, Sub(o, progress)
                                                            If progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Updated Then
                                                                Assert.Equal(1, source.GetBuildErrors().Length)
                                                            ElseIf progress = ExternalErrorDiagnosticUpdateSource.BuildProgress.Done Then
                                                                Assert.Equal(2, source.GetBuildErrors().Length)
                                                            End If
                                                        End Sub

                source.AddNewErrors(projectId2, GetDiagnosticData(projectId2))
                Await waiter.CreateExpeditedWaitTask()

                source.OnSolutionBuildCompleted()
                Await waiter.CreateExpeditedWaitTask()
            End Using
        End Function

        <Fact>
        Public Async Function TestCompilerDiagnosticWithoutDocumentId() As Task
            Using workspace = TestWorkspace.CreateCSharp(String.Empty)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)()
                Dim waiter = TryCast(listenerProvider.GetListener(FeatureAttribute.ErrorList), AsynchronousOperationListener)

                Dim project = workspace.CurrentSolution.Projects.First()

                Dim analyzer = New CompilationAnalyzer()
                Dim compiler = DiagnosticExtensions.GetCompilerDiagnosticAnalyzer(LanguageNames.CSharp)

                Dim diagnosticWaiter = TryCast(listenerProvider.GetListener(FeatureAttribute.DiagnosticService), AsynchronousOperationListener)

                Dim service = New Microsoft.CodeAnalysis.Diagnostics.TestDiagnosticAnalyzerService(
                    LanguageNames.CSharp,
                    New DiagnosticAnalyzer() {compiler, analyzer}.ToImmutableArray(),
                    listener:=diagnosticWaiter)

                Dim registation = service.CreateIncrementalAnalyzer(workspace)

                Dim source = New ExternalErrorDiagnosticUpdateSource(workspace, service, waiter)

                Dim diagnostic = New DiagnosticData(
                    id:="CS1002",
                    category:="Test",
                    message:="Test Message",
                    enuMessageForBingSearch:="Test Message Format",
                    severity:=DiagnosticSeverity.Error,
                    defaultSeverity:=DiagnosticSeverity.Error,
                    isEnabledByDefault:=True,
                    warningLevel:=0,
                    customTags:=ImmutableArray(Of String).Empty,
                    properties:=ImmutableDictionary(Of String, String).Empty,
                    project.Id,
                    location:=New DiagnosticDataLocation(documentId:=Nothing, sourceSpan:=Nothing, "Test.txt", 4, 4))

                AddHandler service.DiagnosticsUpdated, Sub(o, args)
                                                           Assert.Single(args.Diagnostics)
                                                           Assert.Equal(args.Diagnostics(0).Id, diagnostic.Id)
                                                       End Sub

                source.AddNewErrors(project.Id, diagnostic)
                Await waiter.CreateExpeditedWaitTask()

                source.OnSolutionBuildCompleted()
                Await waiter.CreateExpeditedWaitTask()

                Dim diagnosticServiceWaiter = TryCast(listenerProvider.GetListener(FeatureAttribute.DiagnosticService), AsynchronousOperationListener)
                Await diagnosticServiceWaiter.CreateExpeditedWaitTask()
            End Using
        End Function

        Private Class CompilationEndAnalyzer
            Inherits DiagnosticAnalyzer

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(DescriptorFactory.CreateSimpleDescriptor("CompilationEndAnalyzer"))
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

        Private Function GetDiagnosticData(projectId As ProjectId, Optional isBuildDiagnostic As Boolean = False, Optional id As String = "id") As DiagnosticData
            Dim properties = If(isBuildDiagnostic, DiagnosticData.PropertiesForBuildDiagnostic, ImmutableDictionary(Of String, String).Empty)
            Return New DiagnosticData(
                id,
                category:="Test",
                message:="Test Message",
                enuMessageForBingSearch:="Test Message Format",
                severity:=DiagnosticSeverity.Error,
                defaultSeverity:=DiagnosticSeverity.Error,
                isEnabledByDefault:=True,
                warningLevel:=0,
                projectId:=projectId,
                customTags:=ImmutableArray(Of String).Empty,
                properties:=properties)
        End Function

        Private Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService, IDiagnosticUpdateSource

            Private ReadOnly _data As ImmutableArray(Of DiagnosticData)

            Public Sub New()
            End Sub

            Public Sub New(data As ImmutableArray(Of DiagnosticData))
                Me._data = data
            End Sub

            Public ReadOnly Property SupportGetDiagnostics As Boolean Implements IDiagnosticUpdateSource.SupportGetDiagnostics
                Get
                    Return True
                End Get
            End Property

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticUpdateSource.DiagnosticsUpdated
            Public Event DiagnosticsCleared As EventHandler Implements IDiagnosticUpdateSource.DiagnosticsCleared

            Public Function GetDiagnostics(workspace As Microsoft.CodeAnalysis.Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, includeSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As ImmutableArray(Of DiagnosticData) Implements IDiagnosticUpdateSource.GetDiagnostics
                Return If(includeSuppressedDiagnostics, _data, _data.WhereAsArray(Function(d) Not d.IsSuppressed))
            End Function

            Public Sub Reanalyze(workspace As Microsoft.CodeAnalysis.Workspace, Optional projectIds As IEnumerable(Of ProjectId) = Nothing, Optional documentIds As IEnumerable(Of DocumentId) = Nothing, Optional highPriority As Boolean = False) Implements IDiagnosticAnalyzerService.Reanalyze
            End Sub

            Public Function GetDiagnosticDescriptors(projectOpt As Project) As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)) Implements IDiagnosticAnalyzerService.CreateDiagnosticDescriptorsPerReference
                Return ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)).Empty.Add("reference", ImmutableArray.Create(Of DiagnosticDescriptor)(New DiagnosticDescriptor("CS1002", "test", "test", "test", DiagnosticSeverity.Warning, True)))
            End Function

            Public Function GetDiagnosticsForSpanAsync(document As Document, range As TextSpan, Optional diagnosticId As String = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return Task.FromResult(SpecializedCollections.EmptyEnumerable(Of DiagnosticData))
            End Function

            Public Function TryAppendDiagnosticsForSpanAsync(document As Document, range As TextSpan, diagnostics As List(Of DiagnosticData), Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDiagnosticAnalyzerService.TryAppendDiagnosticsForSpanAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetSpecificCachedDiagnosticsAsync(workspace As Microsoft.CodeAnalysis.Workspace, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Microsoft.CodeAnalysis.Workspace, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetSpecificDiagnosticsAsync(solution As Solution, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificDiagnosticsAsync
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

            Public Function GetDiagnosticDescriptors(analyzer As DiagnosticAnalyzer) As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableArray(Of DiagnosticDescriptor).Empty
            End Function

            Public Function IsCompilerDiagnostic(language As String, diagnostic As DiagnosticData) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnostic
                Return False
            End Function

            Public Function GetCompilerDiagnosticAnalyzer(language As String) As DiagnosticAnalyzer Implements IDiagnosticAnalyzerService.GetCompilerDiagnosticAnalyzer
                Return Nothing
            End Function

            Public Function IsCompilerDiagnosticAnalyzer(language As String, analyzer As DiagnosticAnalyzer) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnosticAnalyzer
                Return False
            End Function

            Public Function ContainsDiagnostics(workspace As Microsoft.CodeAnalysis.Workspace, projectId As ProjectId) As Boolean Implements IDiagnosticAnalyzerService.ContainsDiagnostics
                Throw New NotImplementedException()
            End Function

            Public Function GetHostAnalyzerReferences() As IEnumerable(Of AnalyzerReference) Implements IDiagnosticAnalyzerService.GetHostAnalyzerReferences
                Throw New NotImplementedException()
            End Function

            Public Function GetDiagnosticAnalyzers(project As Project) As ImmutableArray(Of DiagnosticAnalyzer) Implements IDiagnosticAnalyzerService.GetDiagnosticAnalyzers
                Throw New NotImplementedException()
            End Function

            Public Function IsCompilationEndAnalyzer(analyzer As DiagnosticAnalyzer, project As Project, compilation As Compilation) As Boolean Implements IDiagnosticAnalyzerService.IsCompilationEndAnalyzer
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
