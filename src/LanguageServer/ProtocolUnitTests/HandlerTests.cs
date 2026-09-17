// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.IO;
using System.Composition;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class HandlerTests : AbstractLanguageServerProtocolTests
{
    public HandlerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(
        typeof(TestDocumentHandler),
        typeof(TestNonMutatingDocumentHandler),
        typeof(TestRequestHandlerWithNoParams),
        typeof(TestNotificationHandlerFactory),
        typeof(TestNotificationWithoutParamsHandlerFactory),
        typeof(TestLanguageSpecificHandler),
        typeof(TestLanguageSpecificHandlerWithDifferentParams),
        typeof(TestFSharpOnlyDocumentHandler),
        typeof(TestFSharpOnlyNotificationHandler),
        typeof(TestConfigurableDocumentHandler),
        typeof(TestOnDemandProjectLoaderFactory));

    [Fact]
    public void WorkspaceFolderTrackerPreservesSetForEquivalentUpdate()
    {
        var tracker = new WorkspaceFolderTracker();
        var workspaceFolder = new WorkspaceFolder { DocumentUri = new("file:///Workspace"), Name = "Workspace" };
        var equivalentWorkspaceFolder = new WorkspaceFolder { DocumentUri = new("file:///Workspace/"), Name = "Workspace" };
        var eventCount = 0;
        tracker.WorkspaceFoldersChanged += (_, _) => eventCount++;

        tracker.Update([workspaceFolder], removedFolders: null);
        var workspaceFolders = tracker.GetRequiredWorkspaceFolderPaths();
        tracker.Update([equivalentWorkspaceFolder], [workspaceFolder]);

        Assert.Equal(1, eventCount);
        Assert.Same(workspaceFolders, tracker.GetRequiredWorkspaceFolderPaths());
        Assert.Equal(Path.GetFullPath(workspaceFolder.DocumentUri.GetDocumentFilePathFromUri()), Assert.Single(workspaceFolders));
    }

    [Theory, CombinatorialData]
    public async Task ExistingDocumentContextUsesRequestTimeText(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var document = server.GetCurrentSolution().Projects.Single().Documents.Single();
        var documentUri = document.GetURI();
        await server.OpenDocumentAsync(documentUri, "request text");
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = documentUri },
            mutatesSolutionState: false,
            loadSource.Task);

        var requestDocumentTask = context.GetRequiredDocumentAsync(CancellationToken.None).AsTask();
        Assert.True(requestDocumentTask.IsCompleted);
        await server.InsertTextAsync(documentUri, (0, 0, "later "));
        Assert.False(loadSource.Task.IsCompleted);
        loadSource.SetResult(true);

        var requestDocument = await requestDocumentTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Same(await context.GetRequiredWorkspaceAsync(CancellationToken.None), requestDocument.Project.Solution.Workspace);
        Assert.Equal("request text", (await requestDocument.GetTextAsync(CancellationToken.None)).ToString());
    }

    [Fact]
    public async Task AsyncContextReusesInitialSolutionWhenLoadDoesNotChangeWorkspace()
    {
        await using var server = await CreateTestLspServerAsync(
            "workspace text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var documentUri = server.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        await server.OpenDocumentAsync(documentUri, "request text");
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var didStartLoading = false;
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = documentUri },
            mutatesSolutionState: false,
            loadSource.Task,
            onStartLoading: () => didStartLoading = true);
        var initialSolution = (await server.GetManager().GetLspDocumentInfoAsync(
            new TextDocumentIdentifier { DocumentUri = documentUri }, CancellationToken.None)).Solution;

        var solutionTask = context.GetRequiredSolutionAsync(CancellationToken.None).AsTask();
        Assert.True(solutionTask.IsCompleted);
        Assert.False(didStartLoading);
        loadSource.SetResult(true);

        Assert.Same(initialSolution, await solutionTask.WithTimeout(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task AsyncContextRetainsRequestTimeMiscellaneousDocumentAfterDidClose()
    {
        var composition = Composition.AddParts(typeof(TestLspMiscellaneousFilesWorkspaceProviderFactory));
        await using var server = await CreateTestLspServerAsync(
            [],
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition);
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(TestHelpers.CreateAbsolutePath("Loose.cs"));
        await server.OpenDocumentAsync(documentUri, "request text");

        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = documentUri },
            mutatesSolutionState: false,
            loadSource.Task);
        var requestDocumentTask = context.GetRequiredDocumentAsync(CancellationToken.None).AsTask();

        await server.CloseDocumentAsync(documentUri);
        Assert.Empty(server.GetTrackedTexts());
        loadSource.SetResult(true);

        var requestDocument = await requestDocumentTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, requestDocument.Project.Solution.WorkspaceKind);
        Assert.Equal("request text", (await requestDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Empty(await server.GetManagerAccessor()
            .GetMiscellaneousDocumentsAsync(static project => project.Documents)
            .ToImmutableArrayAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AsyncContextDoesNotRemoveReopenedMiscellaneousDocument()
    {
        var composition = Composition.AddParts(typeof(TestLspMiscellaneousFilesWorkspaceProviderFactory));
        await using var server = await CreateTestLspServerAsync(
            [],
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition);
        var documentPath = TestHelpers.CreateAbsolutePath("Loose.cs");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        var identifier = new TextDocumentIdentifier { DocumentUri = documentUri };
        await server.OpenDocumentAsync(documentUri, "request text");

        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            identifier,
            mutatesSolutionState: false,
            loadSource.Task);

        await server.CloseDocumentAsync(documentUri);
        await server.OpenDocumentAsync(documentUri, "later text");
        var reopenedDocument = (await server.GetManager().GetLspDocumentInfoAsync(
            identifier, CancellationToken.None)).Document;
        Assert.NotNull(reopenedDocument);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, reopenedDocument.Project.Solution.WorkspaceKind);

        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        await server.TestWorkspace.ChangeSolutionAsync(
            server.TestWorkspace.CurrentSolution
                .AddProject(projectId, "Loaded", "Loaded", LanguageNames.CSharp)
                .AddDocument(documentId, "Loose.cs", SourceText.From("request text"), filePath: documentPath));
        loadSource.SetResult(true);

        var requestDocument = await context.GetRequiredDocumentAsync(CancellationToken.None)
            .AsTask().WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(WorkspaceKind.Host, requestDocument.Project.Solution.WorkspaceKind);

        var miscellaneousDocument = Assert.Single(await server.GetManagerAccessor()
            .GetMiscellaneousDocumentsAsync(static project => project.Documents)
            .ToImmutableArrayAsync(CancellationToken.None));
        Assert.Equal("later text", (await miscellaneousDocument.GetTextAsync(CancellationToken.None)).ToString());
    }

    [Fact]
    public async Task AsyncContextLeavesMiscellaneousDocumentCleanupToDidClose()
    {
        var composition = Composition.AddParts(typeof(TestLspMiscellaneousFilesWorkspaceProviderFactory));
        await using var server = await CreateTestLspServerAsync(
            [],
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition);
        var documentPath = TestHelpers.CreateAbsolutePath("Loose.cs");
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        await server.OpenDocumentAsync(documentUri, "request text");

        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = documentUri },
            mutatesSolutionState: false,
            loadSource.Task);
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        await server.TestWorkspace.ChangeSolutionAsync(
            server.TestWorkspace.CurrentSolution
                .AddProject(projectId, "Loaded", "Loaded", LanguageNames.CSharp)
                .AddDocument(documentId, "Loose.cs", SourceText.From("request text"), filePath: documentPath));

        var requestDocumentTask = context.GetRequiredDocumentAsync(CancellationToken.None).AsTask();
        loadSource.SetResult(true);

        var requestDocument = await requestDocumentTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(WorkspaceKind.Host, requestDocument.Project.Solution.WorkspaceKind);
        Assert.Single(await server.GetManagerAccessor()
            .GetMiscellaneousDocumentsAsync(static project => project.Documents)
            .ToImmutableArrayAsync(CancellationToken.None));

        await server.CloseDocumentAsync(documentUri);
        Assert.Empty(await server.GetManagerAccessor()
            .GetMiscellaneousDocumentsAsync(static project => project.Documents)
            .ToImmutableArrayAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OnDemandLoadingStartsForDidOpenAndInitialLookupMisses()
    {
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var loader = (TestOnDemandProjectLoader)server.GetServerAccessor().GetLspServices()
            .GetRequiredService<IOnDemandProjectLoader>();
        var documentUri = server.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();

        await server.OpenDocumentAsync(documentUri, "request text");
        Assert.Equal(1, loader.StartLoadingCount);

        await server.InsertTextAsync(documentUri, (0, 0, "later "));
        Assert.Equal(1, loader.StartLoadingCount);

        await server.ExecuteRequestAsync<TestRequestTypeOne, string>(
            TestNonMutatingDocumentHandler.MethodName,
            new TestRequestTypeOne(new TextDocumentIdentifier { DocumentUri = documentUri }),
            CancellationToken.None);
        Assert.Equal(1, loader.StartLoadingCount);

        var missingDocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(TestHelpers.CreateAbsolutePath("Missing.cs"));
        await server.ExecuteRequestAsync<TestRequestTypeOne, string>(
            TestDocumentHandler.MethodName,
            new TestRequestTypeOne(new TextDocumentIdentifier { DocumentUri = missingDocumentUri }),
            CancellationToken.None);
        Assert.Equal(1, loader.StartLoadingCount);

        await server.ExecuteRequestAsync<TestRequestTypeOne, string>(
            TestNonMutatingDocumentHandler.MethodName,
            new TestRequestTypeOne(new TextDocumentIdentifier { DocumentUri = missingDocumentUri }),
            CancellationToken.None);
        Assert.Equal(2, loader.StartLoadingCount);

        await server.CloseDocumentAsync(documentUri);
        Assert.Equal(2, loader.StartLoadingCount);
    }

    [Fact]
    public async Task WorkspaceLoadCapturePrecedesLaterChangesWithoutBlockingOnCompletion()
    {
        var composition = Composition.AddParts(typeof(TestWorkspaceSnapshotHandler));
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition);
        var documentUri = server.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        await server.OpenDocumentAsync(documentUri, "request text");
        var loader = (TestOnDemandProjectLoader)server.GetServerAccessor().GetLspServices()
            .GetRequiredService<IOnDemandProjectLoader>();
        var captureStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCapture = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        loader.CaptureWorkspaceLoadSnapshot = async () =>
        {
            captureStarted.SetResult(true);
            await releaseCapture.Task;
            return new ProjectLoadSnapshot(loadCompletion.Task);
        };

        var request = server.ExecuteRequestAsync<TestRequestTypeThree, string>(
            TestWorkspaceSnapshotHandler.MethodName, new TestRequestTypeThree("request"), CancellationToken.None);
        try
        {
            await captureStarted.Task.WithTimeout(TestHelpers.HangMitigatingTimeout);
            var laterChange = server.InsertTextAsync(documentUri, (0, 0, "later "));
            Assert.False(laterChange.IsCompleted);
            releaseCapture.SetResult(true);
            await laterChange.WithTimeout(TestHelpers.HangMitigatingTimeout);

            var unrelatedResponse = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(
                TestNonMutatingDocumentHandler.MethodName,
                new TestRequestTypeOne(new TextDocumentIdentifier { DocumentUri = documentUri }),
                CancellationToken.None).WithTimeout(TestHelpers.HangMitigatingTimeout);
            Assert.Equal(nameof(TestNonMutatingDocumentHandler), unrelatedResponse);
            Assert.False(request.IsCompleted);

            loadCompletion.SetResult(true);
            Assert.Equal("request text", await request.WithTimeout(TestHelpers.HangMitigatingTimeout));
        }
        finally
        {
            releaseCapture.TrySetResult(true);
            loadCompletion.TrySetResult(true);
        }
    }

    [Fact]
    public async Task CancelingAccessorWaitDoesNotCancelSharedLoading()
    {
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            textDocumentIdentifier: null,
            mutatesSolutionState: false,
            loadSource.Task);

        using var cancellationSource = new CancellationTokenSource();
        var canceledRequest = context.GetRequiredSolutionAsync(cancellationSource.Token).AsTask();
        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await canceledRequest.WithTimeout(TestHelpers.HangMitigatingTimeout));
        Assert.False(loadSource.Task.IsCompleted);

        var successfulRequest = context.GetRequiredSolutionAsync(CancellationToken.None).AsTask();
        Assert.False(successfulRequest.IsCompleted);
        loadSource.SetResult(true);

        var requestSolution = await successfulRequest.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Same(server.TestWorkspace, requestSolution.Workspace);
    }

    [Fact]
    public async Task CanceledProjectLoadFallsBackWithoutReportingFatalError()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var didReportCancellation = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception is OperationCanceledException)
                didReportCancellation = true;
        });
        var context = await CreateRequestContextAsync(
            server,
            textDocumentIdentifier: null,
            mutatesSolutionState: false,
            Task.FromCanceled(new CancellationToken(canceled: true)));
        var initialSolution = server.GetCurrentSolution();

        var solution = await context.GetRequiredSolutionAsync(CancellationToken.None);

        Assert.Same(initialSolution, solution);
        Assert.False(didReportCancellation);
    }

    [Theory, CombinatorialData]
    public async Task CompletedLoadUsesCapturedContextWithoutDeferringResolution(bool cancelAccessor)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var initialSolution = server.GetCurrentSolution();
        var context = await CreateRequestContextAsync(
            server, textDocumentIdentifier: null, mutatesSolutionState: false, Task.CompletedTask);
        await server.TestWorkspace.ChangeSolutionAsync(initialSolution.AddProject("Later", "Later", LanguageNames.CSharp).Solution);

        using var cancellationSource = new CancellationTokenSource();
        if (cancelAccessor)
            cancellationSource.Cancel();

        var expectedSolution = await Task.FromResult(initialSolution).WithCancellation(cancellationSource.Token);
        var solutionAccess = context.GetRequiredSolutionAsync(cancellationSource.Token);
        Assert.True(solutionAccess.IsCompleted);
        Assert.Same(expectedSolution, await solutionAccess);
    }

    [Theory, CombinatorialData]
    public async Task CapturedContextsStoreSynchronousResultsInline(bool documentRequest, bool hasLoader)
    {
        var composition = hasLoader ? Composition : Composition.RemoveParts(typeof(TestOnDemandProjectLoaderFactory));
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer },
            composition);
        var manager = server.GetManager();
        var documentUri = server.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        var capturedContext = documentRequest
            ? await manager.CaptureLspDocumentContextAsync(
                new TextDocumentIdentifier { DocumentUri = documentUri }, manager.GetTrackedLspText(),
                allowProjectLoading: true, CancellationToken.None)
            : await manager.CaptureLspSolutionContextAsync(
                manager.GetTrackedLspText(), allowProjectLoading: true, CancellationToken.None);

        Assert.True(capturedContext.HasValue);
        Assert.True(capturedContext.Value.IsCompletedSuccessfully);
        // ValueTask equality distinguishes an inline value from a Task-backed result.
        Assert.Equal(new ValueTask<LspWorkspaceManager.LspContext>(capturedContext.Value.Result), capturedContext.Value);
    }

    [Fact]
    public async Task ConcurrentContextAccessorsShareDeferredResolution()
    {
        await using var server = await CreateTestLspServerAsync(
            "workspace text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var initialDocument = server.GetCurrentSolution().Projects.Single().Documents.Single();
        await server.OpenDocumentAsync(initialDocument.GetURI(), "request text");
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var context = await CreateRequestContextAsync(server, textDocumentIdentifier: null, mutatesSolutionState: false, loadSource.Task);
            var contextCopy = context;
            var firstAccess = context.GetRequiredSolutionAsync(CancellationToken.None).AsTask();
            var secondAccess = contextCopy.GetRequiredSolutionAsync(CancellationToken.None).AsTask();
            var workspaceAccess = context.GetRequiredWorkspaceAsync(CancellationToken.None).AsTask();
            Assert.False(firstAccess.IsCompleted);
            Assert.False(secondAccess.IsCompleted);
            Assert.False(workspaceAccess.IsCompleted);

            await server.TestWorkspace.ChangeSolutionAsync(
                server.TestWorkspace.CurrentSolution.AddProject("Loaded", "Loaded", LanguageNames.CSharp).Solution);
            loadSource.SetResult(true);

            var solutions = await Task.WhenAll(firstAccess, secondAccess).WithTimeout(TestHelpers.HangMitigatingTimeout);
            Assert.Same(solutions[0], solutions[1]);
            Assert.Same(solutions[0].Workspace, await workspaceAccess.WithTimeout(TestHelpers.HangMitigatingTimeout));
            Assert.Equal(2, solutions[0].ProjectIds.Count);
            Assert.Equal("request text", (await solutions[0].GetRequiredDocument(initialDocument.Id).GetTextAsync()).ToString());
            Assert.Equal(1, ((TestOnDemandProjectLoader)server.GetServerAccessor().GetLspServices()
                .GetRequiredService<IOnDemandProjectLoader>()).CaptureWorkspaceLoadCount);

            var repeatedAccess = context.GetRequiredSolutionAsync(CancellationToken.None);
            Assert.True(repeatedAccess.IsCompletedSuccessfully);
            Assert.Same(solutions[0], await repeatedAccess);
        }
        finally
        {
            loadSource.TrySetResult(true);
        }
    }

    [Fact]
    public async Task UnexpectedLoadFailureReachesContextAccessor()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server, textDocumentIdentifier: null, mutatesSolutionState: false, loadSource.Task);
        var expected = new InvalidOperationException("Unexpected load failure");
        loadSource.SetException(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.GetRequiredSolutionAsync(CancellationToken.None).AsTask()));
    }

    [Fact]
    public async Task QueueCancellationStopsResolutionWithoutCancelingSharedLoading()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationSource = new CancellationTokenSource();
        var context = await CreateRequestContextAsync(
            server, textDocumentIdentifier: null, mutatesSolutionState: false, loadSource.Task,
            cancellationToken: cancellationSource.Token);

        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.GetRequiredSolutionAsync(CancellationToken.None).AsTask());
        Assert.False(loadSource.Task.IsCompleted);
        loadSource.SetResult(true);
    }

    [Fact]
    public async Task RequiredAsyncContextAccessorsThrowWhenContextIsUnavailable()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var context = await CreateRequestContextAsync(
            server,
            textDocumentIdentifier: null,
            mutatesSolutionState: false,
            Task.CompletedTask,
            requiresLspSolution: false);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.GetRequiredWorkspaceAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.GetRequiredSolutionAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.GetRequiredTextDocumentAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.GetRequiredDocumentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MutatingContextAccessDoesNotWaitForLoading()
    {
        await using var server = await CreateTestLspServerAsync(
            "request text",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var documentUri = server.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        await server.OpenDocumentAsync(documentUri, "request text");
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = documentUri },
            mutatesSolutionState: true,
            loadSource.Task);

        var requestDocument = await context.GetRequiredDocumentAsync(CancellationToken.None);
        Assert.Equal("request text", (await requestDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.False(loadSource.Task.IsCompleted);
        loadSource.SetResult(true);
    }

    [Fact]
    public async Task MissingDocumentAccessRetainsInitialSolution()
    {
        await using var server = await CreateTestLspServerAsync(
            "class C { }",
            mutatingLspWorkspace: false,
            new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initialSolution = server.GetCurrentSolution();
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(TestHelpers.CreateAbsolutePath("Missing.cs")) },
            mutatesSolutionState: false,
            loadSource.Task);
        var solutionTask = context.GetRequiredSolutionAsync(CancellationToken.None).AsTask();

        await server.TestWorkspace.ChangeSolutionAsync(
            server.TestWorkspace.CurrentSolution.AddProject("Later", "Later", LanguageNames.CSharp).Solution);
        loadSource.SetResult(true);

        var solution = await solutionTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Same(initialSolution, solution);
        Assert.Null(await context.GetDocumentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AsyncContextSupportsNonSourceTextDocuments()
    {
        var additionalDocumentPath = TestHelpers.CreateAbsolutePath("File.razor");
        await using var server = await CreateXmlTestLspServerAsync(
            $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document>class C { }</Document>
                    <AdditionalDocument FilePath="{{additionalDocumentPath}}">request text</AdditionalDocument>
                </Project>
            </Workspace>
            """,
            mutatingLspWorkspace: false,
            initializationOptions: new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            new TextDocumentIdentifier { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(additionalDocumentPath) },
            mutatesSolutionState: false,
            loadSource.Task);
        var textDocumentTask = context.GetRequiredTextDocumentAsync(CancellationToken.None).AsTask();
        loadSource.SetResult(true);

        var textDocument = await textDocumentTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.GetDocumentAsync(CancellationToken.None).ConfigureAwait(false));
        Assert.Equal("request text", (await textDocument.GetTextAsync(CancellationToken.None)).ToString());
    }

    [Theory, CombinatorialData]
    public async Task ClearSolutionContextClearsSharedState(bool loadPending)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);

        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            textDocumentIdentifier: null,
            mutatesSolutionState: false,
            loadPending ? loadSource.Task : Task.CompletedTask);
        var contextCopy = context;
        var pendingAccess = loadPending ? contextCopy.GetSolutionAsync(CancellationToken.None).AsTask() : null;
        context.ClearSolutionContext();
        loadSource.SetResult(true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await contextCopy.GetSolutionAsync(CancellationToken.None).ConfigureAwait(false));
        if (pendingAccess is not null)
            await Assert.ThrowsAsync<InvalidOperationException>(() => pendingAccess);
    }

    [Fact]
    public async Task WorkspaceContextRefreshesWhenLoadFinishesBeforeResolutionStarts()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var initialSolution = server.GetCurrentSolution();
        await server.TestWorkspace.ChangeSolutionAsync(
            initialSolution.AddProject("Loaded", "Loaded", LanguageNames.CSharp).Solution);

        var contextTask = await server.GetManager().CaptureLspSolutionContextAsync(
            server.GetManager().GetTrackedLspText(),
            allowProjectLoading: true,
            CancellationToken.None);
        var context = await contextTask!.Value;

        Assert.Equal(2, context.Solution.ProjectIds.Count);
    }

    [Fact]
    public async Task WorkspaceContextRefreshesSolutionAfterLoadCompletes()
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace: false);
        var loadSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = await CreateRequestContextAsync(
            server,
            textDocumentIdentifier: null,
            mutatesSolutionState: false,
            loadSource.Task);
        var solutionTask = context.GetRequiredSolutionAsync(CancellationToken.None).AsTask();
        Assert.False(solutionTask.IsCompleted);

        await server.TestWorkspace.ChangeSolutionAsync(
            server.TestWorkspace.CurrentSolution.AddProject("Later", "Later", LanguageNames.CSharp).Solution);
        loadSource.SetResult(true);

        var solution = await solutionTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(2, solution.ProjectIds.Count);
    }

    private static Task<RequestContext> CreateRequestContextAsync(
        TestLspServer server,
        TextDocumentIdentifier? textDocumentIdentifier,
        bool mutatesSolutionState,
        Task projectLoadTask,
        bool requiresLspSolution = true,
        Action? onStartLoading = null,
        CancellationToken cancellationToken = default)
    {
        var lspServices = server.GetServerAccessor().GetLspServices();
        var loader = (TestOnDemandProjectLoader)lspServices.GetRequiredService<IOnDemandProjectLoader>();
        loader.StartLoading = () =>
        {
            onStartLoading?.Invoke();
            return projectLoadTask;
        };
        loader.CaptureWorkspaceLoadSnapshot = () =>
        {
            onStartLoading?.Invoke();
            return new(new ProjectLoadSnapshot(projectLoadTask));
        };

        return RequestContext.CreateAsync(
            mutatesSolutionState,
            requiresLSPSolution: requiresLspSolution,
            textDocumentIdentifier,
            WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            server.ClientCapabilities,
            [LanguageNames.CSharp, LanguageNames.VisualBasic],
            lspServices,
            lspServices.GetRequiredService<ILspLogger>(),
            method: nameof(CreateRequestContextAsync),
            allowProjectLoading: !mutatesSolutionState,
            cancellationToken);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteRequestHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteRequestHandlerWithNoParams(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var response = await server.ExecuteRequest0Async<string>(TestRequestHandlerWithNoParams.MethodName, CancellationToken.None);
        Assert.Equal(typeof(TestRequestHandlerWithNoParams).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteNotificationHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        await server.ExecuteNotificationAsync(TestNotificationHandler.MethodName, request);
        var response = await server.GetRequiredLspService<TestNotificationHandler>().ResultSource.Task;
        Assert.Equal(typeof(TestNotificationHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteNotificationHandlerWithNoParams(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        await server.ExecuteNotification0Async(TestNotificationWithoutParamsHandler.MethodName);
        var response = await server.GetRequiredLspService<TestNotificationWithoutParamsHandler>().ResultSource.Task;
        Assert.Equal(typeof(TestNotificationWithoutParamsHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteLanguageSpecificHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.fs")
        });
        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestLanguageSpecificHandler).Name, response);
    }

    [Theory, CombinatorialData]
    public async Task CanExecuteLanguageSpecificHandlerWithDifferentRequestTypes(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeTwo(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.vb")
        });
        var response = await server.ExecuteRequestAsync<TestRequestTypeTwo, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestLanguageSpecificHandlerWithDifferentParams).Name, response);
    }

    [Theory, CombinatorialData]
    public Task ThrowsOnInvalidLanguageSpecificHandler(bool mutatingLspWorkspace)
        => Assert.ThrowsAsync<InvalidOperationException>(async () => await CreateTestLspServerAsync("", mutatingLspWorkspace,
            composition: Composition.AddParts(typeof(TestDuplicateLanguageSpecificHandler))));

    [Theory, CombinatorialData]
    public async Task ThrowsIfDeserializationFails(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeThree("value");
        await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(async () => await server.ExecuteRequestAsync<TestRequestTypeThree, string>(TestNonMutatingDocumentHandler.MethodName, request, CancellationToken.None));
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
    }

    [Theory, CombinatorialData]
    public async Task ShutsdownIfDeserializationFailsOnMutatingRequest(bool mutatingLspWorkspace)
    {
        var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        try
        {
            var request = new TestRequestTypeThree("value");
            await Assert.ThrowsAnyAsync<Exception>(async () => await server.ExecuteRequestAsync<TestRequestTypeThree, string>(TestDocumentHandler.MethodName, request, CancellationToken.None));
            await server.AssertServerShuttingDownAsync();
        }
        finally
        {
            await Assert.ThrowsAsync<JsonException>(async () => await server.DisposeAsync());
        }
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerExceptionNFWIsReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new InvalidOperationException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.True(didReport);
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerExceptionNFWIsNotReportedForLocalRpcException(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new StreamJsonRpc.LocalRpcException(nameof(HandlerTests)) { ErrorCode = LspErrorCodes.ContentModified });
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task MutatingHandlerExceptionNFWIsReported(bool mutatingLspWorkspace)
    {
        var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new InvalidOperationException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: true, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        await server.AssertServerShuttingDownAsync();

        Assert.True(didReport);
    }

    [Theory, CombinatorialData]
    public async Task NonMutatingHandlerCancellationExceptionNFWIsNotReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new OperationCanceledException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: false, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task MutatingHandlerCancellationExceptionNFWIsNotReported(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestWithDocument(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var didReport = false;
        FatalError.OverwriteHandler((exception, severity, dumps) =>
        {
            if (exception.Message == nameof(HandlerTests) || exception.InnerException?.Message == nameof(HandlerTests))
            {
                didReport = true;
            }
        });

        var response = Task.FromException<TestConfigurableResponse>(new OperationCanceledException(nameof(HandlerTests)));
        TestConfigurableDocumentHandler.ConfigureHandler(server, mutatesSolutionState: true, requiresLspSolution: true, response);

        await Assert.ThrowsAnyAsync<Exception>(async ()
            => await server.ExecuteRequestAsync<TestRequestWithDocument, TestConfigurableResponse>(TestConfigurableDocumentHandler.MethodName, request, CancellationToken.None));

        Assert.False(didReport);
    }

    [Theory, CombinatorialData]
    public async Task TestMutatingHandlerCrashesIfUnableToDetermineLanguage(bool mutatingLspWorkspace)
    {
        var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        try
        {
            // Run a mutating request against a file which we have no saved languageId for
            // and where the language cannot be determined from the URI.
            // This should crash the server.
            var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"untitled:untitledFile");
            var request = new TestRequestTypeOne(new TextDocumentIdentifier
            {
                DocumentUri = looseFileUri
            });

            await Assert.ThrowsAnyAsync<Exception>(async () => await testLspServer.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None)).ConfigureAwait(false);
            await testLspServer.AssertServerShuttingDownAsync();
        }
        finally
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await testLspServer.DisposeAsync());
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task DoesNotCrashOnRequestForMissingHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var exception = await Assert.ThrowsAsync<RemoteMethodNotFoundException>(async ()
            => await server.ExecuteRequestAsync<TestRequestTypeOne, string>("nonExistentMethod", request, CancellationToken.None));
        Assert.Equal(JsonRpcErrorCode.MethodNotFound, (JsonRpcErrorCode)exception.ErrorCode);
        Assert.Equal("nonExistentMethod", exception.TargetMethod);
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
        Assert.False(server.GetQueueAccessor()!.Value.IsComplete());

        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task DoesNotCrashOnNotificationForMissingHandler(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        await server.ExecuteNotificationAsync("nonExistentMethod", request);

        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
        Assert.False(server.GetQueueAccessor()!.Value.IsComplete());
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task DoesNotCrashOnRegisteredRequestForUnsupportedLanguage(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        var exception = await Assert.ThrowsAsync<RemoteMethodNotFoundException>(async ()
            => await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestFSharpOnlyDocumentHandler.MethodName, request, CancellationToken.None));
        Assert.Equal(JsonRpcErrorCode.MethodNotFound, (JsonRpcErrorCode)exception.ErrorCode);
        Assert.Equal(TestFSharpOnlyDocumentHandler.MethodName, exception.TargetMethod);
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
        Assert.False(server.GetQueueAccessor()!.Value.IsComplete());

        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/84890")]
    public async Task DoesNotCrashOnRegisteredNotificationForUnsupportedLanguage(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new TestRequestTypeOne(new TextDocumentIdentifier
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\test.cs")
        });

        await server.ExecuteNotificationAsync(TestFSharpOnlyNotificationHandler.MethodName, request);

        var response = await server.ExecuteRequestAsync<TestRequestTypeOne, string>(TestDocumentHandler.MethodName, request, CancellationToken.None);
        Assert.Equal(typeof(TestDocumentHandler).Name, response);
        Assert.False(server.GetServerAccessor().HasShutdownStarted());
        Assert.False(server.GetQueueAccessor()!.Value.IsComplete());
    }

    internal sealed record TestRequestTypeOne([property: JsonPropertyName("textDocument"), JsonRequired] TextDocumentIdentifier TextDocumentIdentifier);

    internal sealed record TestRequestTypeTwo([property: JsonPropertyName("textDocument"), JsonRequired] TextDocumentIdentifier TextDocumentIdentifier);

    internal sealed record TestRequestTypeThree([property: JsonPropertyName("someValue")] string SomeValue);

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestDocumentHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public const string MethodName = nameof(TestDocumentHandler);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestNonMutatingDocumentHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNonMutatingDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public const string MethodName = nameof(TestNonMutatingDocumentHandler);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestRequestHandlerWithNoParams)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestRequestHandlerWithNoParams() : ILspServiceRequestHandler<string>
    {
        public const string MethodName = nameof(TestRequestHandlerWithNoParams);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestWorkspaceSnapshotHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestWorkspaceSnapshotHandler() : ILspServiceRequestHandler<TestRequestTypeThree, string>
    {
        public const string MethodName = nameof(TestWorkspaceSnapshotHandler);

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public async Task<string> HandleRequestAsync(TestRequestTypeThree request, RequestContext context, CancellationToken cancellationToken)
        {
            var solution = await context.GetRequiredSolutionAsync(cancellationToken);
            var document = solution.Projects.Single().Documents.Single();
            return (await document.GetTextAsync(cancellationToken)).ToString();
        }
    }

    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    internal sealed class TestNotificationHandler() : ILspServiceNotificationHandler<TestRequestTypeOne>
    {
        public const string MethodName = nameof(TestNotificationHandler);
        public readonly TaskCompletionSource<string> ResultSource = new();

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task HandleNotificationAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            ResultSource.SetResult(this.GetType().Name);
        }
    }

    /// <summary>
    /// Exported via a factory as we need a new instance for each server (the task completion result should be unique per server).
    /// </summary>
    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestNotificationHandler)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNotificationHandlerFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new TestNotificationHandler();
        }
    }

    [LanguageServerEndpoint(MethodName, LanguageServerConstants.DefaultLanguageName)]
    internal sealed class TestNotificationWithoutParamsHandler() : ILspServiceNotificationHandler
    {
        public const string MethodName = nameof(TestNotificationWithoutParamsHandler);
        public readonly TaskCompletionSource<string> ResultSource = new();

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task HandleNotificationAsync(RequestContext context, CancellationToken cancellationToken)
        {
            ResultSource.SetResult(this.GetType().Name);
        }
    }

    /// <summary>
    /// Exported via a factory as we need a new instance for each server (the task completion result should be unique per server).
    /// </summary>
    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestNotificationWithoutParamsHandler)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestNotificationWithoutParamsHandlerFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new TestNotificationWithoutParamsHandler();
        }
    }

    [ExportCSharpVisualBasicLspServiceFactory(typeof(IOnDemandProjectLoader)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestOnDemandProjectLoaderFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
            => new TestOnDemandProjectLoader();
    }

    internal sealed class TestOnDemandProjectLoader : IOnDemandProjectLoader
    {
        public int StartLoadingCount { get; private set; }
        public int CaptureWorkspaceLoadCount { get; private set; }
        public Func<Task> StartLoading { get; set; } = static () => Task.CompletedTask;
        public Func<ValueTask<ProjectLoadSnapshot>> CaptureWorkspaceLoadSnapshot { get; set; }
            = static () => new(new ProjectLoadSnapshot(Task.CompletedTask));

        public Task StartLoadingAsync(DocumentUri uri)
        {
            StartLoadingCount++;
            return StartLoading();
        }

        public ValueTask<ProjectLoadSnapshot> CaptureWorkspaceLoadSnapshotAsync()
        {
            CaptureWorkspaceLoadCount++;
            return CaptureWorkspaceLoadSnapshot();
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method as <see cref="TestDocumentHandler"/>
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestLanguageSpecificHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestLanguageSpecificHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method as <see cref="TestDocumentHandler"/>
    /// but using different request and response types.
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestLanguageSpecificHandlerWithDifferentParams)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.VisualBasic)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestLanguageSpecificHandlerWithDifferentParams() : ILspServiceDocumentRequestHandler<TestRequestTypeTwo, string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeTwo request)
        {
            return request.TextDocumentIdentifier;
        }

        public async Task<string> HandleRequestAsync(TestRequestTypeTwo request, RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestFSharpOnlyDocumentHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestFSharpOnlyDocumentHandler() : ILspServiceDocumentRequestHandler<TestRequestTypeOne, string>
    {
        public const string MethodName = nameof(TestFSharpOnlyDocumentHandler);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TestRequestTypeOne request)
        {
            return request.TextDocumentIdentifier;
        }

        public Task<string> HandleRequestAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.GetType().Name);
        }
    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestFSharpOnlyNotificationHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestFSharpOnlyNotificationHandler() : ILspServiceNotificationHandler<TestRequestTypeOne>
    {
        public const string MethodName = nameof(TestFSharpOnlyNotificationHandler);

        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public Task HandleNotificationAsync(TestRequestTypeOne request, RequestContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Defines a language specific handler with the same method and language as <see cref="TestLanguageSpecificHandler"/>
    /// but with different params (an error)
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(TestDuplicateLanguageSpecificHandler)), PartNotDiscoverable, Shared]
    [LanguageServerEndpoint(TestDocumentHandler.MethodName, LanguageNames.FSharp)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestDuplicateLanguageSpecificHandler() : ILspServiceRequestHandler<string>
    {
        public bool MutatesSolutionState => true;
        public bool RequiresLSPSolution => true;

        public async Task<string> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return this.GetType().Name;
        }
    }
}
