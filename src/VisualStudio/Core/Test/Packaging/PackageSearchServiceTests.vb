Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Packaging
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Public Class PackageSearchServiceTests
        Private Shared ReadOnly s_allButMoqExceptions As Func(Of Exception, Boolean) =
            Function(e) TypeOf e IsNot MockException

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function TestCacheFolderCreatedIfMissing() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)(MockBehavior.Strict)

            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
            ioServiceMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo))).Callback(
                AddressOf cancellationTokenSource.Cancel)

            Dim remoteControlService = New Mock(Of IPackageSearchRemoteControlService)

            Dim service = New PackageSearchService(
                remoteControlService:=remoteControlService.Object,
                logService:=TestLogService.Instance,
                delayService:=TestDelayService.Instance,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=Nothing,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await service.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlService.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function TestCacheFolderNotCreatedIfPresent() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)(MockBehavior.Strict)

            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True).Callback(
                AddressOf cancellationTokenSource.Cancel)

            Dim remoteControlService = New Mock(Of IPackageSearchRemoteControlService)

            Dim service = New PackageSearchService(
                remoteControlService:=remoteControlService.Object,
                logService:=TestLogService.Instance,
                delayService:=TestDelayService.Instance,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=Nothing,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await service.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlService.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function TestDownloadFullDatabaseWhenLocalFileMissing() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)

            Dim serviceMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)
            serviceMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                Returns(clientMock.Object).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=serviceMock.Object,
                logService:=TestLogService.Instance,
                delayService:=TestDelayService.Instance,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=Nothing,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            serviceMock.Verify()
            clientMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function TestClientDisposedAfterUse() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)(MockBehavior.Strict)
            clientMock.Setup(Sub(c) c.Dispose())

            Dim serviceMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)
            serviceMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                Returns(clientMock.Object).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=serviceMock.Object,
                logService:=TestLogService.Instance,
                delayService:=TestDelayService.Instance,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=Nothing,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            serviceMock.Verify()
            clientMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function CrashInClientRunsFailureLoopPath() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)(MockBehavior.Strict)
            clientMock.Setup(Sub(c) c.ReadFileAsync(It.IsAny(Of __VsRemoteControlBehaviorOnStale))).
                Throws(New NotImplementedException())
            clientMock.Setup(Sub(c) c.Dispose())

            Dim remoteControlMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                Returns(clientMock.Object)

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)
            delayMock.SetupGet(Function(s) s.UpdateFailedDelay).Returns(TimeSpan.Zero).Callback(
                AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=Nothing,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            delayMock.Verify()
        End Function

        Private Class TestDelayService
            Implements IPackageSearchDelayService

            Public Shared ReadOnly Instance As TestDelayService = New TestDelayService()

            Private Sub New()
            End Sub

            Public ReadOnly Property CachePollDelay As TimeSpan Implements IPackageSearchDelayService.CachePollDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property FileWriteDelay As TimeSpan Implements IPackageSearchDelayService.FileWriteDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property UpdateFailedDelay As TimeSpan Implements IPackageSearchDelayService.UpdateFailedDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property UpdateSucceededDelay As TimeSpan Implements IPackageSearchDelayService.UpdateSucceededDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property
        End Class

        Private Class TestLogService
            Implements IPackageSearchLogService

            Public Shared ReadOnly Instance As TestLogService = New TestLogService()

            Private Sub New()
            End Sub

            Public Sub LogException(e As Exception, text As String) Implements IPackageSearchLogService.LogException
            End Sub

            Public Sub LogInfo(text As String) Implements IPackageSearchLogService.LogInfo
            End Sub
        End Class
    End Class
End Namespace
