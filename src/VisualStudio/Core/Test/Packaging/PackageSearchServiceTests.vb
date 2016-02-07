Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.LanguageServices.Packaging
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Public Class PackageSearchServiceTests
        Private Shared ReadOnly s_allButMoqExceptions As Func(Of Exception, Boolean) =
            Function(e) TypeOf e IsNot MockException

        <WpfFact, Trait(Traits.Feature, Traits.Features.Packaging)>
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
