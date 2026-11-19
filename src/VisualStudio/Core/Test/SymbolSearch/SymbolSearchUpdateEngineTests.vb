' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.IO.Compression
Imports System.Threading
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Elfie.Model
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Moq

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SymbolSearch
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Packaging)>
    Public Class SymbolSearchUpdateEngineTests
        Private Shared ReadOnly s_allButMoqExceptions As Func(Of Exception, CancellationToken, Boolean) =
            Function(e, cancellationToken) TypeOf e IsNot MockException

        <Fact>
        Public Async Function CreateCacheFolderIfMissing() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the cache folder being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                ' Expect that the cache directory is created.  Cancel processing at that point so 
                ' the test can complete.
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo))).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim fileDownloaderFactory = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)

                Dim service = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=fileDownloaderFactory.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await service.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                fileDownloaderFactory.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function DoNotCreateCacheFolderIfItIsThere() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the cache folder being there.  We use a 'strict' mock so that 
                ' we'll throw if we get the call to create the directory.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of DirectoryInfo))).Returns(True).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim fileDownloaderFactory = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)

                Dim service = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=fileDownloaderFactory.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await service.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                fileDownloaderFactory.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function DownloadFullDatabaseWhenLocalDatabaseIsMissing() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the local database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))

                Dim downloaderMock = New Mock(Of IFileDownloader)(MockBehavior.Strict)
                downloaderMock.Setup(Sub(s) s.Dispose())

                Dim serviceMock = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)

                ' The client should request the 'Latest' database from the server. 
                ' Cancel processing at that point so the test can complete.
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                    Returns(downloaderMock.Object).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=serviceMock.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                serviceMock.Verify()
                downloaderMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function FailureToParseFullDBAtXmlLevelTakesCatastrophicPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the local database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))

                Dim downloaderMock = CreateDownloaderMock(CreateStream(New XElement("Database",
                    New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, ""),
                    New XAttribute(SymbolSearchUpdateEngine.ChecksumAttributeName, Convert.ToBase64String(New Byte() {0, 1, 2})))))

                Dim serviceMock = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)

                ' The client should request the 'Latest' database from the server. 
                ' Cancel processing at that point so the test can complete.
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                    Returns(downloaderMock.Object)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.CatastrophicFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=serviceMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                serviceMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function TestClientDisposedAfterUse() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))

                Dim downloaderMock = New Mock(Of IFileDownloader)(MockBehavior.Strict)
                downloaderMock.Setup(Sub(c) c.Dispose())

                Dim serviceMock = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                    Returns(downloaderMock.Object).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=serviceMock.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                serviceMock.Verify()
                downloaderMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function CrashInClientRunsFailureLoopPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database not being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))

                Dim downloaderMock = New Mock(Of IFileDownloader)(MockBehavior.Strict)

                ' We should get a call to try to read the file. Simulate a crash in the client.
                downloaderMock.Setup(Sub(c) c.ReadFileAsync()).
                    Throws(New NotImplementedException())

                ' Client should be disposed.
                downloaderMock.Setup(Sub(c) c.Dispose())

                Dim remoteControlMock = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)
                remoteControlMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                    Returns(downloaderMock.Object)

                ' Because the client failed we will expect to call into the 'UpdateFailedDelay' to
                ' control when we do our next loop.
                ' Cancel processing at that point so the test can complete.
                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.ExpectedFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function FailureToParseFullDBAtElfieLevelTakesCatastrophicPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)
                'Simulate the database file not existing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))

                ' Get a client that will download the latest database.
                Dim downloaderMock = CreateFullDatabasedownloaderMock()
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=True)

                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                ' Simulate Elfie throwing when trying to make a database from the contents of that response
                factoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Throws(New NotImplementedException())

                ' Because the parsing failed we will expect to call into the 'UpdateFailedDelay' to
                ' control when we do our next loop.
                ' Cancel processing at that point so the test can complete.
                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.CatastrophicFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function SuccessParsingDBWritesToDisk() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)
                ' Simulate the local database not being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))
                ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo)))
                ioMock.Setup(Sub(s) s.Move(It.IsAny(Of String), It.IsAny(Of String)))

                ' Create a client that will download the latest database.
                Dim downloaderMock = CreateFullDatabasedownloaderMock()
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=True)

                ' Successfully create a database from that response.
                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                factoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because writing to disk succeeded, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function WriteAgainOnIOFailure() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)
                ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))
                ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo)))
                ioMock.Setup(Sub(s) s.Move(It.IsAny(Of String), It.IsAny(Of String)))

                ' Create a client that will download the latest database
                Dim downloaderMock = CreateFullDatabasedownloaderMock()
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=True)

                ' Create a database from the client response.
                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                factoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Write the temp file out to disk.
                ioMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsAny(Of String), It.IsAny(Of ArraySegment(Of Byte))))

                ' Simulate a failure doing the first 'replace' of the database file.
                ioMock.Setup(Sub(s) s.Replace(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Boolean))).
                    Throws(New IOException())

                ' We'll expect to have to replay the write.  So we should get a call to 'FileWriteDelay'
                delayMock.SetupGet(Function(s) s.FileWriteDelay).Returns(TimeSpan.Zero)

                ' Succeed on the second write attempt.
                ioMock.Setup(Sub(s) s.Replace(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Boolean)))

                ' Because writing to disk succeeded, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_UpToDate_DoesNothing() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)
                ioMock.Setup(Function(s) s.ReadAllBytes(It.IsAny(Of String))).Returns({})
                ioMock.Setup(Function(s) s.OpenRead(It.IsAny(Of String))).Returns(New MemoryStream())

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch that says things are up to date.
                Dim downloaderMock = CreatePatchdownloaderMock(isUpToDate:=True)
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=False)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because everything is up to date, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_IsTooOldCausesFullDownload() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)
                ioMock.Setup(Function(s) s.ReadAllBytes(It.IsAny(Of String))).Returns({})
                ioMock.Setup(Function(s) s.OpenRead(It.IsAny(Of String))).Returns(New MemoryStream())
                ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo)))

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch that says things are too old.
                Dim downloaderMock = CreatePatchdownloaderMock(isTooOld:=True)
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=False)

                ' This should cause us to want to then download the full db.  So now
                ' setup an expectation that we'll download the latest.
                Dim downloaderMock2 = CreateFullDatabasedownloaderMock()
                SetupDownloadLatest(remoteControlMock, downloaderMock2)

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because we got the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                downloaderMock2.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_ContentsCausesPatching_FailureToPatchCausesFullDownload() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)
                ioMock.Setup(Function(s) s.ReadAllBytes(It.IsAny(Of String))).Returns({})
                ioMock.Setup(Function(s) s.OpenRead(It.IsAny(Of String))).Returns(New MemoryStream())
                ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo)))

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch with contents.
                Dim downloaderMock = CreatePatchdownloaderMock(contents:="")
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=False)

                ' Simulate a crash in the patching process.
                Dim patchService = New Mock(Of IPatchService)(MockBehavior.Strict)
                patchService.Setup(Sub(s) s.ApplyPatch(It.IsAny(Of Byte()), It.IsAny(Of Byte()))).
                    Throws(New NotImplementedException())

                ' This should cause us to want to then download the full db.  So now
                ' setup an expectation that we'll download the latest.
                Dim downloaderMock2 = CreateFullDatabasedownloaderMock()
                SetupDownloadLatest(remoteControlMock, downloaderMock2)

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because we wrote the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                downloaderMock2.Verify()
                patchService.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_ContentsCausesPatching_SuccessfulPatchWritesToDisk() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)
                ioMock.Setup(Function(s) s.ReadAllBytes(It.IsAny(Of String))).Returns({})
                ioMock.Setup(Function(s) s.OpenRead(It.IsAny(Of String))).Returns(New MemoryStream())
                ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo)))

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromStream(It.IsAny(Of Stream), It.IsAny(Of Boolean))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch with contents.
                Dim downloaderMock = CreatePatchdownloaderMock(contents:="")
                Dim remoteControlMock = CreatefileDownloaderFactoryMock(downloaderMock, latest:=False)

                ' Simulate a crash in the patching process.
                Dim patchMock = New Mock(Of IPatchService)(MockBehavior.Strict)
                patchMock.Setup(Function(s) s.ApplyPatch(It.IsAny(Of Byte()), It.IsAny(Of Byte()))).
                    Returns(New Byte() {0})

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because we wrote the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=patchMock.Object,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowExceptionUnlessCanceled:=s_allButMoqExceptions)

                Await searchService.UpdateContinuouslyAsync(PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)
                ioMock.Verify()
                remoteControlMock.Verify()
                downloaderMock.Verify()
                patchMock.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function InterruptedDatabasePublicationPreservesBinaryCache(isPatch As Boolean) As Task
            Using cancellationTokenSource As New CancellationTokenSource()
                Dim files As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase)
                Dim textPath = GetCacheFilePath(".txt")
                Dim binaryPath = GetCacheFilePath(".bin")
                Dim previousDatabase = New AddReferenceDatabase(ArdbVersion.V1) With {.DatabaseVersion = 41}
                Dim updatedDatabase = New AddReferenceDatabase(ArdbVersion.V1) With {.DatabaseVersion = 42}
                Dim updatedText = CreateDatabaseContent(updatedDatabase, isBinary:=False)

                If isPatch Then
                    files(textPath) = CreateDatabaseContent(previousDatabase, isBinary:=False)
                    files(binaryPath) = CreateDatabaseContent(previousDatabase, isBinary:=True)
                End If

                Dim publishedFiles As New List(Of String)
                Dim ioMock = CreateFileCacheMock(
                    files,
                    Sub(path)
                        publishedFiles.Add(path)
                        cancellationTokenSource.Cancel()
                    End Sub)

                Dim downloaderMock = If(
                    isPatch,
                    CreatePatchdownloaderMock(contents:=""),
                    CreateDownloaderMock(CreateFullDownloadElementStream(updatedText)))
                Dim downloaderFactory = CreatefileDownloaderFactoryMock(downloaderMock, latest:=Not isPatch)
                Dim patchService = New Mock(Of IPatchService)(MockBehavior.Strict)
                patchService.Setup(Function(s) s.ApplyPatch(It.IsAny(Of Byte()), It.IsAny(Of Byte()))).
                    Returns(updatedText)

                Dim service = New SymbolSearchUpdateEngine(
                    fileDownloaderFactory:=downloaderFactory.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=patchService.Object,
                    databaseFactoryService:=New TestDatabaseFactory(),
                    reportAndSwallowExceptionUnlessCanceled:=AddressOf ReportCacheTestException)

                Await service.UpdateContinuouslyAsync(
                    PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token)

                Assert.Equal(binaryPath, Assert.Single(publishedFiles))
                Assert.Equal(
                    42,
                    New TestDatabaseFactory().CreateDatabaseFromStream(
                        New MemoryStream(files(binaryPath)), isBinary:=True).DatabaseVersion)

                Dim requestPath = Await ReadCacheBeforeRemoteResponseAsync(files)
                Assert.EndsWith("/42_Patch.xml", requestPath)
            End Using
        End Function

        <Fact>
        Public Async Function BinaryCacheDoesNotRequireTextCache() As Task
            Dim database = New AddReferenceDatabase(ArdbVersion.V1) With {.DatabaseVersion = 42}
            Dim binaryPath = GetCacheFilePath(".bin")
            Dim binaryContent = CreateDatabaseContent(database, isBinary:=True)
            Dim files As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase) From {
                {binaryPath, binaryContent}
            }

            Dim requestPath = Await ReadCacheBeforeRemoteResponseAsync(files)

            Assert.EndsWith("/42_Patch.xml", requestPath)
            Assert.Same(binaryContent, files(binaryPath))
            Assert.False(files.ContainsKey(GetCacheFilePath(".txt")))
        End Function

        <Fact>
        Public Async Function BinaryOnlyCacheRetainsReferenceAssemblyResults() As Task
            Using tempRoot As New TempRoot(), cancellationTokenSource As New CancellationTokenSource()
                Dim settingsDirectory = tempRoot.CreateDirectory().Path
                Dim cacheDirectory = Directory.CreateDirectory(Path.Combine(settingsDirectory, "PackageCache", "Format1"))
                Dim database As New AddReferenceDatabase(ArdbVersion.V1)
                database.ReadText(New StringReader(String.Join(vbLf, {
                    "Elfie V1",
                    "42",
                    "P MicrosoftAssemblyReferences",
                    vbTab & "R 30",
                    New String(ChrW(9), 2) & "A CacheAssembly",
                    New String(ChrW(9), 3) & "N CacheNamespace",
                    New String(ChrW(9), 4) & "C CacheType",
                    ""
                })))
                database.ConvertToImmutable()
                File.WriteAllBytes(
                    Path.Combine(cacheDirectory.FullName, "_58__58_nuget_58__58_.bin"),
                    CreateDatabaseContent(database, isBinary:=True))

                Dim requestPath As String = Nothing
                Dim downloader As New Mock(Of IFileDownloader)(MockBehavior.Strict)
                downloader.Setup(Sub(s) s.Dispose())
                Dim downloaderFactory As New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)
                downloaderFactory.Setup(Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                    Callback(Sub(host As String, path As String, pollingMinutes As Integer)
                                 requestPath = path
                                 cancellationTokenSource.Cancel()
                             End Sub).
                    Returns(downloader.Object)
                Dim service As New SymbolSearchUpdateEngine(downloaderFactory.Object)

                Await service.UpdateContinuouslyAsync(
                    PackageSourceHelper.NugetOrgSourceName, settingsDirectory, cancellationTokenSource.Token)
                Assert.EndsWith("/42_Patch.xml", requestPath)

                Dim result = Assert.Single(Await service.FindReferenceAssembliesAsync(
                    New TypeQuery("CacheType", Arity:=0), NamespaceQuery.Default, CancellationToken.None))
                Assert.Equal("CacheAssembly", result.AssemblyName)
                Assert.Equal("CacheType", result.TypeName)
                Assert.Equal({"CacheNamespace"}, result.ContainingNamespaceNames)
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function TextCacheIsConvertedBeforeRemoteResponse(returnsNoData As Boolean) As Task
            Dim database = New AddReferenceDatabase(ArdbVersion.V1) With {.DatabaseVersion = 42}
            Dim textPath = GetCacheFilePath(".txt")
            Dim binaryPath = GetCacheFilePath(".bin")
            Dim files As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase) From {
                {textPath, CreateDatabaseContent(database, isBinary:=False)}
            }

            Dim requestPath = Await ReadCacheBeforeRemoteResponseAsync(files, returnsNoData)

            Assert.EndsWith("/42_Patch.xml", requestPath)
            Assert.True(files.ContainsKey(binaryPath))
            Assert.Equal(
                42,
                New TestDatabaseFactory().CreateDatabaseFromStream(
                    New MemoryStream(files(binaryPath)), isBinary:=True).DatabaseVersion)

            files.Remove(textPath)
            Assert.EndsWith("/42_Patch.xml", Await ReadCacheBeforeRemoteResponseAsync(files))
        End Function

        <Fact>
        Public Async Function CorruptBinaryCacheWithoutTextDownloadsFullDatabase() As Task
            Dim files As New Dictionary(Of String, Byte())(StringComparer.OrdinalIgnoreCase) From {
                {GetCacheFilePath(".bin"), {0}}
            }

            Dim requestPath = Await ReadCacheBeforeRemoteResponseAsync(files)

            Assert.EndsWith("/Latest.xml", requestPath)
        End Function

        Private Shared Function GetCacheFilePath(extension As String) As String
            Return Path.GetFullPath(Path.Combine(
                "TestDirectory", "PackageCache", "Format1", "_58__58_nuget_58__58_" & extension))
        End Function

        Private Shared Function CreateDatabaseContent(database As AddReferenceDatabase, isBinary As Boolean) As Byte()
            Using stream As New MemoryStream()
                If isBinary Then
                    Using writer As New BinaryWriter(stream)
                        database.WriteBinary(writer)
                    End Using
                Else
                    Using writer As New StreamWriter(stream)
                        database.WriteText(writer)
                    End Using
                End If

                Return stream.ToArray()
            End Using
        End Function

        Private Shared Function CreateFullDownloadElementStream(content As Byte()) As Stream
            Using compressed As New MemoryStream()
                Using deflate As New DeflateStream(compressed, CompressionMode.Compress, leaveOpen:=True)
                    deflate.Write(content, 0, content.Length)
                End Using

                Return CreateStream(New XElement(
                    "Database",
                    New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, Convert.ToBase64String(compressed.ToArray()))))
            End Using
        End Function

        Private Shared Function CreateFileCacheMock(
                files As Dictionary(Of String, Byte()),
                Optional filePublished As Action(Of String) = Nothing) As Mock(Of IIOService)
            Dim ioMock As New Mock(Of IIOService)(MockBehavior.Strict)
            ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).
                Returns(Function(info As FileSystemInfo) TypeOf info Is DirectoryInfo OrElse files.ContainsKey(info.FullName))
            ioMock.Setup(Sub(s) s.Create(It.IsAny(Of DirectoryInfo)))
            ioMock.Setup(Sub(s) s.Delete(It.IsAny(Of FileInfo))).
                Callback(Sub(info As FileInfo) files.Remove(info.FullName))
            ioMock.Setup(Function(s) s.OpenRead(It.IsAny(Of String))).
                Returns(
                    Function(path As String)
                        If Not files.ContainsKey(path) Then
                            Throw New FileNotFoundException(path)
                        End If

                        Return New MemoryStream(files(path), writable:=False)
                    End Function)
            ioMock.Setup(Function(s) s.ReadAllBytes(It.IsAny(Of String))).
                Returns(Function(path As String) files(path))
            ioMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsAny(Of String), It.IsAny(Of ArraySegment(Of Byte)))).
                Callback(Sub(path As String, content As ArraySegment(Of Byte)) files(path) = content.ToArray())

            Dim publish As Action(Of String, String) =
                Sub(source, destination)
                    files(destination) = files(source)
                    files.Remove(source)
                    filePublished?.Invoke(destination)
                End Sub
            ioMock.Setup(Sub(s) s.Move(It.IsAny(Of String), It.IsAny(Of String))).Callback(publish)
            ioMock.Setup(Sub(s) s.Replace(It.IsAny(Of String), It.IsAny(Of String), Nothing, It.IsAny(Of Boolean))).
                Callback(Sub(source As String, destination As String, backup As String, ignoreMetadataErrors As Boolean)
                             publish(source, destination)
                         End Sub)
            Return ioMock
        End Function

        Private Shared Async Function ReadCacheBeforeRemoteResponseAsync(
                files As Dictionary(Of String, Byte()),
                Optional returnsNoData As Boolean = False) As Task(Of String)
            Using cancellationTokenSource As New CancellationTokenSource(TimeSpan.FromSeconds(30))
                Dim request As New TaskCompletionSource(Of String)(TaskCreationOptions.RunContinuationsAsynchronously)
                Dim response As New TaskCompletionSource(Of Stream)(TaskCreationOptions.RunContinuationsAsynchronously)
                Using registration = cancellationTokenSource.Token.Register(
                    Sub()
                        request.TrySetCanceled()
                        response.TrySetCanceled()
                    End Sub)
                    Dim requestPath As String = Nothing
                    Dim downloader As New Mock(Of IFileDownloader)(MockBehavior.Strict)
                    downloader.Setup(Sub(s) s.Dispose())
                    downloader.Setup(Function(s) s.ReadFileAsync()).
                        Returns(
                            Function()
                                request.TrySetResult(requestPath)
                                If returnsNoData Then
                                    cancellationTokenSource.Cancel()
                                    Return Task.FromResult(Of Stream)(Nothing)
                                End If

                                Return response.Task
                            End Function)

                    Dim downloaderFactory As New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)
                    downloaderFactory.Setup(Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                        Callback(Sub(host As String, path As String, pollingMinutes As Integer) requestPath = path).
                        Returns(downloader.Object)
                    Dim service As New SymbolSearchUpdateEngine(
                        fileDownloaderFactory:=downloaderFactory.Object,
                        delayService:=TestDelayService.Instance,
                        ioService:=CreateFileCacheMock(files).Object,
                        patchService:=Nothing,
                        databaseFactoryService:=New TestDatabaseFactory(),
                        reportAndSwallowExceptionUnlessCanceled:=AddressOf ReportCacheTestException)

                    Dim update = service.UpdateContinuouslyAsync(
                        PackageSourceHelper.NugetOrgSourceName, "TestDirectory", cancellationTokenSource.Token).AsTask()
                    Await Task.WhenAny(request.Task, update)
                    If Not request.Task.IsCompleted Then
                        Await update
                        Assert.True(request.Task.IsCompleted, "The updater exited without requesting a remote file.")
                    End If

                    Dim result = Await request.Task
                    cancellationTokenSource.Cancel()
                    Await update
                    Return result
                End Using
            End Using
        End Function

        Private Shared Function ReportCacheTestException(exception As Exception, cancellationToken As CancellationToken) As Boolean
            Return TypeOf exception IsNot MockException AndAlso TypeOf exception IsNot OperationCanceledException
        End Function

        Private NotInheritable Class TestDatabaseFactory
            Implements IDatabaseFactoryService

            Public Function CreateDatabaseFromStream(stream As Stream, isBinary As Boolean) As AddReferenceDatabase Implements IDatabaseFactoryService.CreateDatabaseFromStream
                Dim database As New AddReferenceDatabase(ArdbVersion.V1)
                If isBinary Then
                    Using reader As New BinaryReader(stream)
                        database.ReadBinary(reader)
                    End Using
                Else
                    Using reader As New StreamReader(stream)
                        database.ReadText(reader)
                    End Using
                End If

                Return database
            End Function
        End Class

        Private Shared Sub SetupWritesDatabaseSuccessfullyToDisk(ioMock As Mock(Of IIOService))
            ' Expect that we'll write out the temp file.
            ioMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsRegex(".*tmp"), It.IsAny(Of ArraySegment(Of Byte))))

            ' Expect that we'll replace the existing file with the temp file.
            ioMock.Setup(Sub(s) s.Replace(It.IsRegex(".*tmp"), It.IsRegex(".*(txt|bin)"), Nothing, It.IsAny(Of Boolean)))
        End Sub

        Private Shared Function CreatefileDownloaderFactoryMock(
                downloaderMock As Mock(Of IFileDownloader),
                latest As Boolean) As Mock(Of IFileDownloaderFactory)
            Dim remoteControlMock = New Mock(Of IFileDownloaderFactory)(MockBehavior.Strict)

            If latest Then
                SetupDownloadLatest(remoteControlMock, downloaderMock)
            Else
                SetupDownloadPatch(downloaderMock, remoteControlMock)
            End If

            Return remoteControlMock
        End Function

        Private Shared Sub SetupDownloadPatch(downloaderMock As Mock(Of IFileDownloader), remoteControlMock As Mock(Of IFileDownloaderFactory))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Patch.*"), It.IsAny(Of Integer))).
                Returns(downloaderMock.Object)
        End Sub

        Private Shared Sub SetupDownloadLatest(remoteControlMock As Mock(Of IFileDownloaderFactory), downloaderMock As Mock(Of IFileDownloader))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                Returns(downloaderMock.Object)
        End Sub

        Private Shared Function CreateFullDatabasedownloaderMock() As Mock(Of IFileDownloader)
            Return CreatedownloaderMock(CreateFullDownloadElementStream())
        End Function

        Private Shared Function CreateDownloaderMock(stream As Stream) As Mock(Of IFileDownloader)
            Dim downloaderMock = New Mock(Of IFileDownloader)(MockBehavior.Strict)

            ' Return a full database element when the service asks for it.
            downloaderMock.Setup(Function(c) c.ReadFileAsync()).
                Returns(Task.FromResult(stream))
            ' Always dispose the client when we get a response.
            downloaderMock.Setup(Sub(c) c.Dispose())
            Return downloaderMock
        End Function

        Private Shared Function CreatePatchdownloaderMock(Optional isUpToDate As Boolean = False,
                                               Optional isTooOld As Boolean = False,
                                               Optional contents As String = Nothing) As Mock(Of IFileDownloader)
            Return CreatedownloaderMock(CreatePatchElementStream(isUpToDate, isTooOld, contents))
        End Function

        Private Shared Function CreatePatchElementStream(Optional isUpToDate As Boolean = False,
                                                  Optional isTooOld As Boolean = False,
                                                  Optional contents As String = Nothing) As Stream
            Dim element = New XElement("Patch",
                If(isUpToDate, New XAttribute(SymbolSearchUpdateEngine.UpToDateAttributeName, True), Nothing),
                If(isTooOld, New XAttribute(SymbolSearchUpdateEngine.TooOldAttributeName, True), Nothing),
                If(contents IsNot Nothing, New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, contents), Nothing))

            Return CreateStream(element)
        End Function

        Private Shared Function CreateFullDownloadElementStream() As Stream
            Dim saveStream = New MemoryStream()
            Dim zipStream = New DeflateStream(saveStream, CompressionMode.Compress)
            zipStream.Write(New Byte() {0}, 0, 1)
            zipStream.Flush()
            Dim contents = Convert.ToBase64String(saveStream.ToArray())

            Return CreateStream(New XElement("Database",
                New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, contents)))
        End Function

        Private Shared Function CreateStream(element As XElement) As Stream
            Dim stream = New MemoryStream()
            element.Save(stream)
            stream.Position = 0
            Return stream
        End Function

        Private Class TestDelayService
            Implements IDelayService

            Public Shared ReadOnly Instance As TestDelayService = New TestDelayService()

            Private Sub New()
            End Sub

            Public ReadOnly Property CachePollDelay As TimeSpan Implements IDelayService.CachePollDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property FileWriteDelay As TimeSpan Implements IDelayService.FileWriteDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property ExpectedFailureDelay As TimeSpan Implements IDelayService.ExpectedFailureDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property UpdateSucceededDelay As TimeSpan Implements IDelayService.UpdateSucceededDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property

            Public ReadOnly Property CatastrophicFailureDelay As TimeSpan Implements IDelayService.CatastrophicFailureDelay
                Get
                    Return TimeSpan.Zero
                End Get
            End Property
        End Class
    End Class
End Namespace
