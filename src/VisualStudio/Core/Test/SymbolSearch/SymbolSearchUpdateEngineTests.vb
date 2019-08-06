' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.IO.Compression
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Elfie.Model
Imports Microsoft.CodeAnalysis.SymbolSearch
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.RemoteControl
Imports Moq
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SymbolSearch
    <[UseExportProvider]>
    Public Class SymbolSearchUpdateEngineTests
        Private Shared ReadOnly s_allButMoqExceptions As Func(Of Exception, Boolean) =
            Function(e) TypeOf e IsNot MockException

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
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

                Dim remoteControlService = New Mock(Of IRemoteControlService)

                Dim service = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlService.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await service.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlService.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function DoNotCreateCacheFolderIfItIsThere() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)(MockBehavior.Strict)

                ' Simulate the cache folder being there.  We use a 'strict' mock so that 
                ' we'll throw if we get the call to create the directory.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of DirectoryInfo))).Returns(True).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim remoteControlService = New Mock(Of IRemoteControlService)

                Dim service = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlService.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await service.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlService.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function DownloadFullDatabaseWhenLocalDatabaseIsMissing() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simlute the local database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                Dim clientMock = New Mock(Of IRemoteControlClient)

                Dim serviceMock = New Mock(Of IRemoteControlService)(MockBehavior.Strict)

                ' The client should request the 'Latest' database from the server. 
                ' Cancel processing at that point so the test can complete.
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                    Returns(clientMock.Object).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=serviceMock.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                serviceMock.Verify()
                clientMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function FailureToParseFullDBAtXmlLevelTakesCatastrophicPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simlute the local database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                Dim clientMock = CreateClientMock(CreateStream(New XElement("Database",
                    New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, ""),
                    New XAttribute(SymbolSearchUpdateEngine.ChecksumAttributeName, Convert.ToBase64String(New Byte() {0, 1, 2})))))

                Dim serviceMock = New Mock(Of IRemoteControlService)(MockBehavior.Strict)

                ' The client should request the 'Latest' database from the server. 
                ' Cancel processing at that point so the test can complete.
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                    Returns(clientMock.Object)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.CatastrophicFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=serviceMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                serviceMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function TestClientDisposedAfterUse() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                Dim clientMock = New Mock(Of IRemoteControlClient)(MockBehavior.Strict)
                clientMock.Setup(Sub(c) c.Dispose())

                Dim serviceMock = New Mock(Of IRemoteControlService)(MockBehavior.Strict)
                serviceMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                    Returns(clientMock.Object).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=serviceMock.Object,
                    delayService:=TestDelayService.Instance,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                serviceMock.Verify()
                clientMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function CrashInClientRunsFailureLoopPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database not being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                Dim clientMock = New Mock(Of IRemoteControlClient)(MockBehavior.Strict)

                ' We should get a call to try to read the file. Simulate a crash in the client.
                clientMock.Setup(Sub(c) c.ReadFileAsync(It.IsAny(Of BehaviorOnStale))).
                    Throws(New NotImplementedException())

                ' Client should be disposed.
                clientMock.Setup(Sub(c) c.Dispose())

                Dim remoteControlMock = New Mock(Of IRemoteControlService)(MockBehavior.Strict)
                remoteControlMock.Setup(
                    Function(s) s.CreateClient(It.IsAny(Of String), It.IsAny(Of String), It.IsAny(Of Integer))).
                    Returns(clientMock.Object)

                ' Because the client failed we will expect to call into the 'UpdateFailedDelay' to
                ' control when we do our next loop.
                ' Cancel processing at that point so the test can complete.
                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.ExpectedFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=Nothing,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function FailureToParseFullDBAtElfieLevelTakesCatastrophicPath() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()
                'Simulate the database file not existing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                ' Get a client that will download the latest database.
                Dim clientMock = CreateFullDatabaseClientMock()
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                ' Simulate Elfie throwing when trying to make a database from the contents of that response
                factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Throws(New NotImplementedException())

                ' Because the parsing failed we will expect to call into the 'UpdateFailedDelay' to
                ' control when we do our next loop.
                ' Cancel processing at that point so the test can complete.
                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)
                delayMock.SetupGet(Function(s) s.CatastrophicFailureDelay).Returns(TimeSpan.Zero).Callback(
                    AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function SuccessParsingDBWritesToDisk() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()
                ' Simulate the local database not being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                ' Create a client that will download the latest database.
                Dim clientMock = CreateFullDatabaseClientMock()
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

                ' Successfully create a database from that response.
                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Returns(New AddReferenceDatabase())

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because writing to disk succeeded, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function WriteAgainOnIOFailure() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database being missing.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(False)

                ' Create a client that will download the latest databsae
                Dim clientMock = CreateFullDatabaseClientMock()
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=True)

                ' Create a database from the client response.
                Dim factoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                factoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Returns(New AddReferenceDatabase())

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Write the temp file out to disk.
                ioMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsAny(Of String), It.IsAny(Of Byte())))

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
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=factoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
                factoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_UpToDate_DoesNothing() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch that says things are up to date.
                Dim clientMock = CreatePatchClientMock(isUpToDate:=True)
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because everything is up to date, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_IsTooOldCausesFullDownload() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
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
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because we got the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                clientMock2.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_ContentsCausesPatching_FailureToPatchCausesFullDownload() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch with contents.
                Dim clientMock = CreatePatchClientMock(contents:="")
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

                ' Simulate a crash in the patching process.
                Dim patchService = New Mock(Of IPatchService)(MockBehavior.Strict)
                patchService.Setup(Sub(s) s.ApplyPatch(It.IsAny(Of Byte()), It.IsAny(Of Byte()))).
                    Throws(New NotImplementedException())

                ' This should cause us to want to then download the full db.  So now
                ' setup an expectation that we'll download the latest.
                Dim clientMock2 = CreateFullDatabaseClientMock()
                SetupDownloadLatest(remoteControlMock, clientMock2)

                ' Expect that we'll write the database to disk successfully.
                SetupWritesDatabaseSuccessfullyToDisk(ioMock)

                Dim delayMock = New Mock(Of IDelayService)(MockBehavior.Strict)

                ' Because we wrote the full database, we expect we'll loop on the 'UpdateSucceededDelay'.
                ' Cancel processing at that point so the test can complete.
                delayMock.SetupGet(Function(s) s.UpdateSucceededDelay).Returns(TimeSpan.Zero).
                    Callback(AddressOf cancellationTokenSource.Cancel)

                Dim searchService = New SymbolSearchUpdateEngine(
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=Nothing,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                clientMock2.Verify()
                patchService.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Async Function LocalDatabaseExistingCausesPatchToDownload_ContentsCausesPatching_SuccessfulPatchWritesToDisk() As Task
            Using workspace = TestWorkspace.CreateCSharp("")
                Dim cancellationTokenSource = New CancellationTokenSource()

                Dim ioMock = New Mock(Of IIOService)()

                ' Simulate the database being there.
                ioMock.Setup(Function(s) s.Exists(It.IsAny(Of FileSystemInfo))).Returns(True)

                ' We'll successfully read in the local database.
                Dim databaseFactoryMock = New Mock(Of IDatabaseFactoryService)(MockBehavior.Strict)
                databaseFactoryMock.Setup(Function(f) f.CreateDatabaseFromBytes(It.IsAny(Of Byte()))).
                    Returns(New AddReferenceDatabase())

                ' Create a client that will return a patch with contents.
                Dim clientMock = CreatePatchClientMock(contents:="")
                Dim remoteControlMock = CreateRemoteControlServiceMock(clientMock, latest:=False)

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
                    logService:=TestLogService.Instance,
                    progressService:=TestProgressService.Instance,
                    remoteControlService:=remoteControlMock.Object,
                    delayService:=delayMock.Object,
                    ioService:=ioMock.Object,
                    patchService:=patchMock.Object,
                    databaseFactoryService:=databaseFactoryMock.Object,
                    reportAndSwallowException:=s_allButMoqExceptions,
                    updateCancellationToken:=cancellationTokenSource.Token)

                Await searchService.UpdateContinuouslyAsync(SymbolSearchUpdateEngine.NugetOrgSource, "TestDirectory")
                ioMock.Verify()
                remoteControlMock.Verify()
                clientMock.Verify()
                patchMock.Verify()
                delayMock.Verify()
                databaseFactoryMock.Verify()
            End Using
        End Function

        Private Shared Sub SetupWritesDatabaseSuccessfullyToDisk(ioMock As Mock(Of IIOService))
            ' Expect that we'll write out the temp file.
            ioMock.Setup(Sub(s) s.WriteAndFlushAllBytes(It.IsRegex(".*tmp"), It.IsAny(Of Byte())))

            ' Expect that we'll replace the existing file with the temp file.
            ioMock.Setup(Sub(s) s.Replace(It.IsRegex(".*tmp"), It.IsRegex(".*txt"), Nothing, It.IsAny(Of Boolean)))
        End Sub

        Private Shared Function CreateRemoteControlServiceMock(
                clientMock As Mock(Of IRemoteControlClient),
                latest As Boolean) As Mock(Of IRemoteControlService)
            Dim remoteControlMock = New Mock(Of IRemoteControlService)(MockBehavior.Strict)

            If latest Then
                SetupDownloadLatest(remoteControlMock, clientMock)
            Else
                SetupDownloadPatch(clientMock, remoteControlMock)
            End If
            Return remoteControlMock
        End Function

        Private Shared Sub SetupDownloadPatch(clientMock As Mock(Of IRemoteControlClient), remoteControlMock As Mock(Of IRemoteControlService))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Patch.*"), It.IsAny(Of Integer))).
                Returns(clientMock.Object)
        End Sub

        Private Shared Sub SetupDownloadLatest(remoteControlMock As Mock(Of IRemoteControlService), clientMock As Mock(Of IRemoteControlClient))
            remoteControlMock.Setup(
                Function(s) s.CreateClient(It.IsAny(Of String), It.IsRegex(".*Latest.*"), It.IsAny(Of Integer))).
                Returns(clientMock.Object)
        End Sub

        Private Function CreateFullDatabaseClientMock() As Mock(Of IRemoteControlClient)
            Return CreateClientMock(CreateFullDownloadElementStream())
        End Function

        Private Function CreateClientMock(stream As Stream) As Mock(Of IRemoteControlClient)
            Dim clientMock = New Mock(Of IRemoteControlClient)(MockBehavior.Strict)

            ' Return a full database element when the service asks for it.
            clientMock.Setup(Function(c) c.ReadFileAsync(It.IsAny(Of BehaviorOnStale))).
                Returns(Task.FromResult(stream))
            ' Always dispose the client when we get a response.
            clientMock.Setup(Sub(c) c.Dispose())
            Return clientMock
        End Function

        Private Function CreatePatchClientMock(Optional isUpToDate As Boolean = False,
                                               Optional isTooOld As Boolean = False,
                                               Optional contents As String = Nothing) As Mock(Of IRemoteControlClient)
            Return CreateClientMock(CreatePatchElementStream(isUpToDate, isTooOld, contents))
        End Function

        Private Function CreatePatchElementStream(Optional isUpToDate As Boolean = False,
                                                  Optional isTooOld As Boolean = False,
                                                  Optional contents As String = Nothing) As Stream
            Dim element = New XElement("Patch",
                If(isUpToDate, New XAttribute(SymbolSearchUpdateEngine.UpToDateAttributeName, True), Nothing),
                If(isTooOld, New XAttribute(SymbolSearchUpdateEngine.TooOldAttributeName, True), Nothing),
                If(contents IsNot Nothing, New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, contents), Nothing))

            Return CreateStream(element)
        End Function

        Private Function CreateFullDownloadElementStream() As Stream
            Dim saveStream = New MemoryStream()
            Dim zipStream = New DeflateStream(saveStream, CompressionMode.Compress)
            zipStream.Write(New Byte() {0}, 0, 1)
            zipStream.Flush()
            Dim contents = Convert.ToBase64String(saveStream.ToArray())

            Return CreateStream(New XElement("Database",
                New XAttribute(SymbolSearchUpdateEngine.ContentAttributeName, contents)))
        End Function

        Private Function CreateStream(element As XElement) As Stream
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

        Private Class TestLogService
            Implements ISymbolSearchLogService

            Public Shared ReadOnly Instance As TestLogService = New TestLogService()

            Private Sub New()
            End Sub

            Public Function LogExceptionAsync(exception As String, text As String, cancellationToken As CancellationToken) As Task Implements ISymbolSearchLogService.LogExceptionAsync
                Return Task.CompletedTask
            End Function

            Public Function LogInfoAsync(text As String, cancellationToken As CancellationToken) As Task Implements ISymbolSearchLogService.LogInfoAsync
                Return Task.CompletedTask
            End Function
        End Class

        Private Class TestProgressService
            Implements ISymbolSearchProgressService

            Public Shared ReadOnly Instance As TestProgressService = New TestProgressService()

            Private Sub New()
            End Sub

            Public Function OnDownloadFullDatabaseStartedAsync(title As String, cancellationToken As CancellationToken) As Task Implements ISymbolSearchProgressService.OnDownloadFullDatabaseStartedAsync
                Return Task.CompletedTask
            End Function

            Public Function OnDownloadFullDatabaseSucceededAsync(cancellationToken As CancellationToken) As Task Implements ISymbolSearchProgressService.OnDownloadFullDatabaseSucceededAsync
                Return Task.CompletedTask
            End Function

            Public Function OnDownloadFullDatabaseCanceledAsync(cancellationToken As CancellationToken) As Task Implements ISymbolSearchProgressService.OnDownloadFullDatabaseCanceledAsync
                Return Task.CompletedTask
            End Function

            Public Function OnDownloadFullDatabaseFailedAsync(message As String, cancellationToken As CancellationToken) As Task Implements ISymbolSearchProgressService.OnDownloadFullDatabaseFailedAsync
                Return Task.CompletedTask
            End Function
        End Class
    End Class
End Namespace
