Imports System.IO
Imports System.IO.Compression
Imports System.Threading
Imports System.Threading.Tasks
Imports Elfie.Model
Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Packaging
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Public Class PackageSearchServiceTests
        Private Shared ReadOnly s_allButMoqExceptions As Func(Of Exception, Boolean) =
            Function(e) TypeOf e IsNot MockException

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function CreateCacheFolderIfMissing() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)(MockBehavior.Strict)

            ' Simulate the cache folder being missing.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            ' Expect that the cache directory is created.  Cancel processing at that point so 
            ' the test can complete.
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
        Public Async Function DoNotCreateCacheFolderIfItIsThere() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)(MockBehavior.Strict)

            ' Simulate the cache folder being there.  We use a 'strict' mock so that 
            ' we'll throw if we get the call to create the directory.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of DirectoryInfo))).Returns(True).Callback(
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
        Public Async Function DownloadFullDatabaseWhenLocalDatabaseIsMissing() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()

            ' Simlute the local database being missing.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)

            Dim serviceMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)

            ' The client should request the 'Latest' database from the server. 
            ' Cancel processing at that point so the test can complete.
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

            ' Simulate the database not being there.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)(MockBehavior.Strict)

            ' We should get a call to try to read the file. Simulate a crash in the client.
            clientMock.Setup(Sub(c) c.ReadFileAsync(It.IsAny(Of __VsRemoteControlBehaviorOnStale))).
                Throws(New NotImplementedException())

            ' Client should be disposed.
            clientMock.Setup(Sub(c) c.Dispose())

            Dim remoteControlMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                Returns(clientMock.Object)

            ' Because the client failed we will expect to call into the 'UpdateFailedDelay' to
            ' control when we do our next loop.
            ' Cancel processing at that point so the test can complete.
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

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function FailureToParseDBRunsFailureLoopPath() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()
            'Simulate the database file not existing.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            ' Get a client that will download the latest database.
            Dim clientMock = CreateFullDatabaseClientMock()
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

            Dim factoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            ' Simulate Elfie throwing when trying to make a database from the contents of that response
            factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Throws(New NotImplementedException())

            ' Because the parsing failed we will expect to call into the 'UpdateFailedDelay' to
            ' control when we do our next loop.
            ' Cancel processing at that point so the test can complete.
            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)
            delayMock.SetupGet(Function(s) s.UpdateFailedDelay).Returns(TimeSpan.Zero).Callback(
                AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=factoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            delayMock.Verify()
            factoryMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function SuccessParsingDBWritesToDisk() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()
            ' Simulate the local database not being there.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            ' Create a client that will download the latest database.
            Dim clientMock = CreateFullDatabaseClientMock()
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

            ' Successfully create a database from that response.
            Dim factoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Returns(New AddReferenceDatabase())

            ' Expect that we'll write the database to disk successfully.
            SetupWritesDatabaseSuccessfullyToDisk(ioServiceMock)

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)

            ' Because writing to disk succeeded, we expect we'll loop on the 'UpdateSucceededDelay'.
            ' Cancel processing at that point so the test can complete.
            delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=factoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            delayMock.Verify()
            factoryMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function WriteAgainOnIOFailure() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()

            ' Simulate the database being missing.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

            ' Create a client that will download the latest databsae
            Dim clientMock = CreateFullDatabaseClientMock()
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

            ' Create a database from the client response.
            Dim factoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Returns(New AddReferenceDatabase())

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)

            ' Write the temp file out to disk.
            ioServiceMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsAny(Of String), It.IsAny(Of Byte())))

            ' Simulate a failure doing the first 'replace' of the database file.
            ioServiceMock.Setup(Sub(s) s.Replace(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Boolean))).
                Throws(New IOException())

            ' We'll expect to have to replay the write.  So we should get a call to 'FileWriteDelay'
            delayMock.SetupGet(Function(s) s.FileWriteDelay).Returns(TimeSpan.Zero)

            ' Succeed on the second write attempt.
            ioServiceMock.Setup(Sub(s) s.Replace(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Boolean)))

            ' Because writing to disk succeeded, we expect we'll loop on the 'UpdateSucceededDelay'.
            ' Cancel processing at that point so the test can complete.
            delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=factoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            delayMock.Verify()
            factoryMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_UpToDate_DoesNothing() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()

            ' Simulate the database being there.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

            ' We'll successfully read in the local database.
            Dim databaseFactoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Returns(New AddReferenceDatabase())

            ' Create a client that will return a patch that says things are up to date.
            Dim clientMock = CreatePatchClientMock(isUpToDate:=True)
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)

            ' Because everything is up to date, we expect we'll loop on the 'UpdateSucceededDelay'.
            ' Cancel processing at that point so the test can complete.
            delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=databaseFactoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            delayMock.Verify()
            databaseFactoryMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_IsTooOldCausesFullDownload() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()

            ' Simulate the database being there.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

            ' We'll successfully read in the local database.
            Dim databaseFactoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Returns(New AddReferenceDatabase())

            ' Create a client that will return a patch that says things are too old.
            Dim clientMock = CreatePatchClientMock(isTooOld:=True)
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

            ' This should cause us to want to then download the full db.  So now
            ' setup an expectation that we'll download the latest.
            Dim clientMock2 = CreateFullDatabaseClientMock()
            SetupDownloadLatest(remoteControlMock, clientMock2)

            ' Expect that we'll write the database to disk successfully.
            SetupWritesDatabaseSuccessfullyToDisk(ioServiceMock)

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)

            ' Because we got the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
            ' Cancel processing at that point so the test can complete.
            delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=databaseFactoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            clientMock2.Verify()
            delayMock.Verify()
            databaseFactoryMock.Verify()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_ContentsCausesPatching_FailureToPatchCausesFullDownload() As Task
            Dim cancellationTokenSource = New CancellationTokenSource()

            Dim ioServiceMock = New Mock(Of IPackageSearchIOService)()

            ' Simulate the database being there.
            ioServiceMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

            ' We'll successfully read in the local database.
            Dim databaseFactoryMock = New Mock(Of IPackageSearchDatabaseFactoryService)(MockBehavior.Strict)
            databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                Returns(New AddReferenceDatabase())

            ' Create a client that will return a patch with contents.
            Dim clientMock = CreatePatchClientMock(contents:="")
            Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

            ' Simulate a crash in the patching process.
            Dim patchService = New Mock(Of IPackageSearchPatchService)(MockBehavior.Strict)
            patchService.Setup(Sub(s) s.ApplyPatch(It.IsAny(Of Byte()), It.IsAny(Of Byte()))).
                Throws(New NotImplementedException())

            ' This should cause us to want to then download the full db.  So now
            ' setup an expectation that we'll download the latest.
            Dim clientMock2 = CreateFullDatabaseClientMock()
            SetupDownloadLatest(remoteControlMock, clientMock2)

            ' Expect that we'll write the database to disk successfully.
            SetupWritesDatabaseSuccessfullyToDisk(ioServiceMock)

            Dim delayMock = New Mock(Of IPackageSearchDelayService)(MockBehavior.Strict)

            ' Because we wrote the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
            ' Cancel processing at that point so the test can complete.
            delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                Callback(AddressOf cancellationTokenSource.Cancel)

            Dim searchService = New PackageSearchService(
                remoteControlService:=remoteControlMock.Object,
                logService:=TestLogService.Instance,
                delayService:=delayMock.Object,
                ioService:=ioServiceMock.Object,
                patchService:=Nothing,
                databaseFactoryService:=databaseFactoryMock.Object,
                localSettingsDirectory:="TestDirectory",
                swallowException:=s_allButMoqExceptions,
                cancellationTokenSource:=cancellationTokenSource)

            Await searchService.UpdateDatabaseInBackgroundAsync()
            ioServiceMock.Verify()
            remoteControlMock.Verify()
            clientMock.Verify()
            clientMock2.Verify()
            patchService.Verify()
            delayMock.Verify()
            databaseFactoryMock.Verify()
        End Function

        Private Shared Sub SetupWritesDatabaseSuccessfullyToDisk(ioServiceMock As Mock(Of IPackageSearchIOService))
            ' Expect that we'll write out the temp file.
            ioServiceMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsRegex(".*tmp"), It.IsAny(Of Byte())))

            ' Expect that we'll replace the existing file with the temp file.
            ioServiceMock.Setup(Sub(s) s.Replace(It.IsRegex(".*tmp"), It.IsRegex(".*txt"), It.IsRegex(".*bak"), It.IsAny(Of Boolean)))
        End Sub

        Private Shared Function CreateRemoteControlServiceMock(
                clientMock As Mock(Of IPackageSearchRemoteControlClient),
                latest As Boolean) As Mock(Of IPackageSearchRemoteControlService)
            Dim remoteControlMock = New Mock(Of IPackageSearchRemoteControlService)(MockBehavior.Strict)

            If latest Then
                SetupDownloadLatest(remoteControlMock, clientMock)
            Else
                SetupDownloadPatch(clientMock, remoteControlMock)
            End If
            Return remoteControlMock
        End Function

        Private Shared Sub SetupDownloadPatch(clientMock As Mock(Of IPackageSearchRemoteControlClient), remoteControlMock As Mock(Of IPackageSearchRemoteControlService))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Patch.*"), It.IsAny(Of Integer))).
                Returns(clientMock.Object)
        End Sub

        Private Shared Sub SetupDownloadLatest(remoteControlMock As Mock(Of IPackageSearchRemoteControlService), clientMock As Mock(Of IPackageSearchRemoteControlClient))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                Returns(clientMock.Object)
        End Sub

        Private Function CreateFullDatabaseClientMock() As Mock(Of IPackageSearchRemoteControlClient)
            Return CreateClientMock(CreateFullDownloadElementStream())
        End Function

        Private Function CreateClientMock(stream As Stream) As Mock(Of IPackageSearchRemoteControlClient)
            Dim clientMock = New Mock(Of IPackageSearchRemoteControlClient)(MockBehavior.Strict)

            ' Return a full database element when the service asks for it.
            clientMock.Setup(Function(c) c.ReadFileAsync(It.IsAny(Of __VsRemoteControlBehaviorOnStale))).
                Returns(Task.FromResult(stream))
            ' Always dispose the client when we get a response.
            clientMock.Setup(Sub(c) c.Dispose())
            Return clientMock
        End Function

        Private Function CreatePatchClientMock(Optional isUpToDate As Boolean = False,
                                               Optional isTooOld As Boolean = False,
                                               Optional contents As String = Nothing) As Mock(Of IPackageSearchRemoteControlClient)
            Return CreateClientMock(CreatePatchElementStream(isUpToDate, isTooOld, contents))
        End Function

        Private Function CreatePatchElementStream(Optional isUpToDate As Boolean = False,
                                                  Optional isTooOld As Boolean = False,
                                                  Optional contents As String = Nothing) As Stream
            Dim element = New XElement("Patch",
                If(isUpToDate, New XAttribute(PackageSearchService.IsUpToDateAttributeName, True), Nothing),
                If(isTooOld, New XAttribute(PackageSearchService.IsTooOldAttributeName, True), Nothing),
                If(contents IsNot Nothing, New XAttribute(PackageSearchService.ContentsAttributeName, contents), Nothing))

            Return CreateStream(element)
        End Function

        Private Function CreateFullDownloadElementStream() As Stream
            Dim saveStream = New MemoryStream()
            Dim zipStream = New DeflateStream(saveStream, CompressionMode.Compress)
            zipStream.Write(New Byte() {0}, 0, 1)
            zipStream.Flush()
            Dim contents = Convert.ToBase64String(saveStream.ToArray())

            Return CreateStream(New XElement("Database",
                New XAttribute(PackageSearchService.ContentsAttributeName, contents)))
        End Function

        Private Function CreateStream(element As XElement) As Stream
            Dim stream = New MemoryStream()
            element.Save(Stream)
            stream.Position = 0
            Return stream
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
