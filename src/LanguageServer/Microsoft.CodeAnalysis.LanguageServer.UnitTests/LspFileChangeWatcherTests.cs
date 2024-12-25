// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using Xunit.Abstractions;
using FileSystemWatcher = Roslyn.LanguageServer.Protocol.FileSystemWatcher;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class LspFileChangeWatcherTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    private readonly ClientCapabilities _clientCapabilitiesWithFileWatcherSupport = new ClientCapabilities
    {
        Workspace = new WorkspaceClientCapabilities
        {
            DidChangeWatchedFiles = new DidChangeWatchedFilesClientCapabilities { DynamicRegistration = true }
        }
    };

    [Fact]
    public async Task LspFileWatcherNotSupportedWithoutClientSupport()
    {
        await using var testLspServer = await TestLspServer.CreateAsync(new ClientCapabilities(), TestOutputLogger, MefCacheDirectory.Path);

        Assert.False(LspFileChangeWatcher.SupportsLanguageServerHost(testLspServer.LanguageServerHost));
    }

    [Fact]
    public async Task LspFileWatcherSupportedWithClientSupport()
    {
        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger, MefCacheDirectory.Path);

        Assert.True(LspFileChangeWatcher.SupportsLanguageServerHost(testLspServer.LanguageServerHost));
    }

    [Fact]
    public async Task CreatingDirectoryWatchRequestsDirectoryWatch()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger, MefCacheDirectory.Path);
        var lspFileChangeWatcher = new LspFileChangeWatcher(
            testLspServer.LanguageServerHost,
            testLspServer.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

        var dynamicCapabilitiesRpcTarget = new DynamicCapabilitiesRpcTarget();
        testLspServer.AddClientLocalRpcTarget(dynamicCapabilitiesRpcTarget);

        var tempDirectory = TempRoot.CreateDirectory();

        // Try creating a context and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext([new ProjectSystem.WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        await WaitForFileWatcherAsync(testLspServer);

        var watcher = GetSingleFileWatcher(dynamicCapabilitiesRpcTarget);

        Assert.Equal(tempDirectory.Path, watcher.GlobPattern.Second.BaseUri.Second.LocalPath);
        Assert.Equal("**/*", watcher.GlobPattern.Second.Pattern);

        // Get rid of the registration and it should be gone again
        context.Dispose();
        await WaitForFileWatcherAsync(testLspServer);
        Assert.Empty(dynamicCapabilitiesRpcTarget.Registrations);
    }

    [Fact]
    public async Task CreatingFileWatchRequestsFileWatch()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger, MefCacheDirectory.Path);
        var lspFileChangeWatcher = new LspFileChangeWatcher(
            testLspServer.LanguageServerHost,
            testLspServer.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

        var dynamicCapabilitiesRpcTarget = new DynamicCapabilitiesRpcTarget();
        testLspServer.AddClientLocalRpcTarget(dynamicCapabilitiesRpcTarget);

        var tempDirectory = TempRoot.CreateDirectory();

        // Try creating a single file watch and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext([]);
        var watchedFile = context.EnqueueWatchingFile("Z:\\SingleFile.txt");
        await WaitForFileWatcherAsync(testLspServer);

        var watcher = GetSingleFileWatcher(dynamicCapabilitiesRpcTarget);

        Assert.Equal("Z:\\", watcher.GlobPattern.Second.BaseUri.Second.LocalPath);
        Assert.Equal("SingleFile.txt", watcher.GlobPattern.Second.Pattern);

        // Get rid of the registration and it should be gone again
        watchedFile.Dispose();
        await WaitForFileWatcherAsync(testLspServer);
        Assert.Empty(dynamicCapabilitiesRpcTarget.Registrations);
    }

    private static async Task WaitForFileWatcherAsync(TestLspServer testLspServer)
    {
        await testLspServer.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
    }

    private static FileSystemWatcher GetSingleFileWatcher(DynamicCapabilitiesRpcTarget dynamicCapabilities)
    {
        var registrationJson = Assert.IsType<JsonElement>(Assert.Single(dynamicCapabilities.Registrations).Value.RegisterOptions);
        var registration = JsonSerializer.Deserialize<DidChangeWatchedFilesRegistrationOptions>(registrationJson, ProtocolConversions.LspJsonSerializerOptions)!;

        return Assert.Single(registration.Watchers);
    }

    private sealed class DynamicCapabilitiesRpcTarget
    {
        public readonly ConcurrentDictionary<string, Registration> Registrations = new();

        [JsonRpcMethod("client/registerCapability", UseSingleObjectParameterDeserialization = true)]
        public Task RegisterCapabilityAsync(RegistrationParams registrationParams, CancellationToken _)
        {
            foreach (var registration in registrationParams.Registrations)
                Assert.True(Registrations.TryAdd(registration.Id, registration));

            return Task.CompletedTask;
        }

        [JsonRpcMethod("client/unregisterCapability", UseSingleObjectParameterDeserialization = true)]
        public Task UnregisterCapabilityAsync(UnregistrationParams unregistrationParams, CancellationToken _)
        {
            foreach (var unregistration in unregistrationParams.Unregistrations)
                Assert.True(Registrations.TryRemove(unregistration.Id, out var _));

            return Task.CompletedTask;
        }
    }
}
