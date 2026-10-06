// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LspFileChangeWatcherTests : AbstractLanguageServerHostTests
{
    private readonly ClientCapabilities _clientCapabilitiesWithFileWatcherSupport = new()
    {
        Workspace = new WorkspaceClientCapabilities
        {
            DidChangeWatchedFiles = new DidChangeWatchedFilesClientCapabilities { DynamicRegistration = true }
        }
    };

    public LspFileChangeWatcherTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);
    }

    [Fact]
    public async Task LspFileWatcherNotSupportedWithoutClientSupport()
    {
        await using var testLspServer = await CreateLanguageServerAsync();

        AssertFileWatcherKind<DefaultFileChangeWatcher>(testLspServer);
    }

    [Fact]
    public async Task LspFileWatcherSupportedWithClientSupport()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);

        AssertFileWatcherKind<LspFileChangeWatcher>(testLspServer);
    }

    [Fact]
    public async Task CreatingDirectoryWatchRequestsDirectoryWatch()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        // Try creating a context and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext([new ProjectSystem.WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var watcher = await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync();

        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(tempDirectory.Path), watcher.GlobPattern.Second.BaseUri.Second);
        Assert.Equal("**/*", watcher.GlobPattern.Second.Pattern);

        // Get rid of the registration and it should be gone again
        context.Dispose();
        Assert.Empty(await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync());
    }

    [Fact]
    public async Task CreatingFileWatchRequestsFileWatch()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        // Try creating a single file watch and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext([]);
        var filePath = Path.Combine(tempDirectory.Path, "SingleFile.txt");
        var watchedFile = context.EnqueueWatchingFile(filePath);
        var watcher = await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync();

        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(tempDirectory.Path), watcher.GlobPattern.Second.BaseUri.Second);
        // We watch all .txt files in this directory so that watching another .txt file can reuse the same watch.
        // The context still only reports changes to the files we've asked it to watch.
        Assert.Equal("*.txt", watcher.GlobPattern.Second.Pattern);

        // Get rid of the registration and it should be gone again
        watchedFile.Dispose();
        context.Dispose();
        Assert.Empty(await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync());
    }

    [Theory]
    [InlineData((int)FileChangeType.Created, (int)FileChangeKind.Created)]
    [InlineData((int)FileChangeType.Changed, (int)FileChangeKind.Changed)]
    [InlineData((int)FileChangeType.Deleted, (int)FileChangeKind.Deleted)]
    public async Task FileChangeNotificationIncludesChangeKind(int fileChangeType, int expectedChangeKind)
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, _) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "File.cs");

        using var context = lspFileChangeWatcher.CreateContext([new ProjectSystem.WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangedSource = new TaskCompletionSource<FileChangedEventArgs>();
        context.FileChanged += (_, e) => fileChangedSource.TrySetResult(e);

        await SendFileChangesAsync(testLspServer, (FileChangeType)fileChangeType, filePath);

        var eventArgs = await fileChangedSource.Task;
        Assert.Equal(filePath, eventArgs.FilePath, ignoreCase: true);
        Assert.Equal((FileChangeKind)expectedChangeKind, eventArgs.ChangeKind);
    }

    [Fact]
    public async Task MultipleFileWatchesUseSingleCombinedGlob()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        using var context = lspFileChangeWatcher.CreateContext([]);
        using var csharpFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "File.cs"));
        using var visualBasicFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "File.vb"));

        var watcher = await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync();
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(tempDirectory.Path), watcher.GlobPattern.Second.BaseUri.Second);
        Assert.Equal("{*.cs,*.vb}", watcher.GlobPattern.Second.Pattern);
    }

    [Fact]
    public async Task MultipleDirectoryFiltersUseSingleCombinedGlob()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        using var context = lspFileChangeWatcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs", ".vb"])]);

        var watcher = await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync();
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(tempDirectory.Path), watcher.GlobPattern.Second.BaseUri.Second);
        Assert.Equal("{**/*.cs,**/*.vb}", watcher.GlobPattern.Second.Pattern);
    }

    [Theory]
    [InlineData(false, "*")]
    [InlineData(true, "**/*")]
    public async Task ExtensionlessFileWatchExpandsGlobToMatchAll(bool includeSubdirectories, string expectedPattern)
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        using var context = lspFileChangeWatcher.CreateContext(includeSubdirectories
            ? [new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]
            : []);
        using var csharpFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "File.cs"));
        using var extensionlessFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "README"));

        Assert.Equal(expectedPattern, (await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync()).GlobPattern.Second.Pattern);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnchangedConfigurationDoesNotSendRequests(bool includeSubdirectories)
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();

        using var context = lspFileChangeWatcher.CreateContext(includeSubdirectories
            ? [new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs", ".vb"])]
            : []);
        using var csharpFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "File.cs"));
        using var visualBasicFile = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "File.vb"));

        var registrationId = Assert.Single(await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync()).Id;
        await dynamicCapabilitiesRpcTarget.ClearRequestsAsync();

        using var secondContext = lspFileChangeWatcher.CreateContext(includeSubdirectories
            ? [new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs", ".vb"])]
            : []);
        using var anotherCsharpFile = secondContext.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "AnotherFile.cs"));

        await dynamicCapabilitiesRpcTarget.AssertNoRequestsAsync();
        Assert.Equal(registrationId, Assert.Single(await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync()).Id);
    }

    [Fact]
    public async Task FileChangesInSiblingDirectoriesAreDeliveredOnce()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();
        var firstDirectory = Path.Combine(tempDirectory.Path, "First");
        var secondDirectory = Path.Combine(tempDirectory.Path, "FirstSibling");
        var firstFilePath = Path.Combine(firstDirectory, "File.cs");
        var secondFilePath = Path.Combine(secondDirectory, "File.cs");

        using var context = lspFileChangeWatcher.CreateContext(
            [new WatchedDirectory(firstDirectory, extensionFilters: [".cs"]), new WatchedDirectory(secondDirectory, extensionFilters: [".cs"])]);
        var fileChanges = new ConcurrentQueue<string>();
        context.FileChanged += (_, e) => fileChanges.Enqueue(e.FilePath);
        Assert.Equal(2, (await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync()).Length);

        await SendFileChangesAsync(testLspServer, FileChangeType.Changed, firstFilePath, secondFilePath, Path.Combine(tempDirectory.Path, "Unrelated.cs"));

        Assert.Equal(2, fileChanges.Count);
        Assert.Contains(firstFilePath, fileChanges, PathUtilities.Comparer);
        Assert.Contains(secondFilePath, fileChanges, PathUtilities.Comparer);
    }

    [Fact]
    public async Task FileChangesInParentAndChildDirectoriesAreDeliveredOnce()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();
        var childDirectory = Path.Combine(tempDirectory.Path, "Child");
        var parentFilePath = Path.Combine(tempDirectory.Path, "Parent.cs");
        var childFilePath = Path.Combine(childDirectory, "Child.cs");

        using var context = lspFileChangeWatcher.CreateContext([]);
        using var parentFile = context.EnqueueWatchingFile(parentFilePath);
        using var childFile = context.EnqueueWatchingFile(childFilePath);
        var fileChanges = new ConcurrentQueue<string>();
        context.FileChanged += (_, e) => fileChanges.Enqueue(e.FilePath);
        Assert.Equal(2, (await dynamicCapabilitiesRpcTarget.GetRegistrationsAsync()).Length);

        await SendFileChangesAsync(testLspServer, FileChangeType.Changed, parentFilePath, childFilePath,
            Path.Combine(tempDirectory.Path, "Unrequested.cs"), Path.Combine(childDirectory, "Unrequested.cs"));

        Assert.Equal(2, fileChanges.Count);
        Assert.Contains(parentFilePath, fileChanges, PathUtilities.Comparer);
        Assert.Contains(childFilePath, fileChanges, PathUtilities.Comparer);
    }

    [Fact]
    public async Task FileChangesRespectContextFilters()
    {
        await using var testLspServer = await CreateLanguageServerAsync(_clientCapabilitiesWithFileWatcherSupport);
        var (lspFileChangeWatcher, dynamicCapabilitiesRpcTarget) = GetWatcherAndRpcTarget(testLspServer);
        var tempDirectory = TempRoot.CreateDirectory();
        var csharpFilePath = Path.Combine(tempDirectory.Path, "File.cs");
        var visualBasicFilePath = Path.Combine(tempDirectory.Path, "File.vb");

        using var filteredContext = lspFileChangeWatcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        using var unfilteredContext = lspFileChangeWatcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var filteredChanges = new ConcurrentQueue<string>();
        var unfilteredChanges = new ConcurrentQueue<string>();
        filteredContext.FileChanged += (_, e) => filteredChanges.Enqueue(e.FilePath);
        unfilteredContext.FileChanged += (_, e) => unfilteredChanges.Enqueue(e.FilePath);
        Assert.Equal("**/*", (await dynamicCapabilitiesRpcTarget.GetSingleFileWatcherAsync()).GlobPattern.Second.Pattern);

        await SendFileChangesAsync(testLspServer, FileChangeType.Changed, csharpFilePath, visualBasicFilePath,
            Path.Combine(TempRoot.CreateDirectory().Path, "Outside.cs"));

        Assert.Equal(csharpFilePath, Assert.Single(filteredChanges), PathUtilities.Comparer);
        Assert.Equal(2, unfilteredChanges.Count);
        Assert.Contains(csharpFilePath, unfilteredChanges, PathUtilities.Comparer);
        Assert.Contains(visualBasicFilePath, unfilteredChanges, PathUtilities.Comparer);
    }

    private static (LspFileChangeWatcher watcher, FileWatcherRegistrationRpcTarget rpcTarget) GetWatcherAndRpcTarget(TestLspServer testLspServer)
    {
        var watcher = AssertFileWatcherKind<LspFileChangeWatcher>(testLspServer);
        var rpcTarget = new FileWatcherRegistrationRpcTarget(testLspServer);
        testLspServer.AddClientLocalRpcTarget(rpcTarget);
        return (watcher, rpcTarget);
    }

    private static async Task SendFileChangesAsync(TestLspServer testLspServer, FileChangeType fileChangeType, params string[] filePaths)
    {
        // The test server accepts notification methods as requests, so we can await handler completion.
        // ExecuteNotificationAsync only waits for the message to be sent, not for the handler to finish.
        await testLspServer.ExecuteRequestAsync<DidChangeWatchedFilesParams, object>(
            Methods.WorkspaceDidChangeWatchedFilesName,
            new DidChangeWatchedFilesParams
            {
                Changes = filePaths.Select(filePath => new FileEvent
                {
                    Uri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath),
                    FileChangeType = fileChangeType,
                }).ToArray(),
            },
            CancellationToken.None);
    }

    private static T AssertFileWatcherKind<T>(TestLspServer server) where T : IFileChangeWatcher
    {
        var lspFileWatcher = server.GetRequiredLspService<IFileChangeWatcher>();
        var delegatingWatcher = Assert.IsType<DelegatingFileChangeWatcher>(lspFileWatcher);
        return Assert.IsType<T>(delegatingWatcher.GetTestAccessor().UnderlyingFileWatcher);
    }

    private sealed class FileWatcherRegistrationRpcTarget(TestLspServer testLspServer)
    {
        private readonly ConcurrentDictionary<string, Registration> _registrations = new();
        private readonly ConcurrentQueue<RegistrationParams> _registrationRequests = new();
        private readonly ConcurrentQueue<UnregistrationParams> _unregistrationRequests = new();

        private Task WaitForFileWatcherAsync()
            => testLspServer.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        public async Task<Registration[]> GetRegistrationsAsync()
        {
            await WaitForFileWatcherAsync();
            return _registrations.Values.ToArray();
        }

        public async Task<Roslyn.LanguageServer.Protocol.FileSystemWatcher> GetSingleFileWatcherAsync()
        {
            var registrationJson = Assert.IsType<JsonElement>(Assert.Single(await GetRegistrationsAsync()).RegisterOptions);
            var registration = JsonSerializer.Deserialize<DidChangeWatchedFilesRegistrationOptions>(registrationJson, ProtocolConversions.LspJsonSerializerOptions)!;
            return Assert.Single(registration.Watchers);
        }

        public async Task ClearRequestsAsync()
        {
            await WaitForFileWatcherAsync();
            _registrationRequests.Clear();
            _unregistrationRequests.Clear();
        }

        public async Task AssertNoRequestsAsync()
        {
            await WaitForFileWatcherAsync();
            Assert.Empty(_registrationRequests);
            Assert.Empty(_unregistrationRequests);
        }

        [JsonRpcMethod("client/registerCapability", UseSingleObjectParameterDeserialization = true)]
        public async Task RegisterCapabilityAsync(RegistrationParams registrationParams, CancellationToken _)
        {
            var registrations = registrationParams.Registrations.Where(
                static registration => registration.Method == Methods.WorkspaceDidChangeWatchedFilesName).ToArray();
            if (registrations.Length > 0)
                _registrationRequests.Enqueue(new RegistrationParams { Registrations = registrations });

            foreach (var registration in registrations)
                Assert.True(_registrations.TryAdd(registration.Id, registration));
        }

        [JsonRpcMethod("client/unregisterCapability", UseSingleObjectParameterDeserialization = true)]
        public async Task UnregisterCapabilityAsync(UnregistrationParams unregistrationParams, CancellationToken _)
        {
            var unregistrations = unregistrationParams.Unregistrations.Where(
                static unregistration => unregistration.Method == Methods.WorkspaceDidChangeWatchedFilesName).ToArray();
            if (unregistrations.Length > 0)
                _unregistrationRequests.Enqueue(new UnregistrationParams { Unregistrations = unregistrations });

            foreach (var unregistration in unregistrations)
                Assert.True(_registrations.TryRemove(unregistration.Id, out var _));
        }
    }
}
