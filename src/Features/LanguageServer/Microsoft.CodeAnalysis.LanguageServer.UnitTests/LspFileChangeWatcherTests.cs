﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using Xunit.Abstractions;
using FileSystemWatcher = Microsoft.VisualStudio.LanguageServer.Protocol.FileSystemWatcher;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class LspFileChangeWatcherTests : AbstractLanguageServerHostTests
{
    private readonly ClientCapabilities _clientCapabilitiesWithFileWatcherSupport = new ClientCapabilities
    {
        Workspace = new WorkspaceClientCapabilities
        {
            DidChangeWatchedFiles = new DynamicRegistrationSetting { DynamicRegistration = true }
        }
    };

    public LspFileChangeWatcherTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task LspFileWatcherNotSupportedWithoutClientSupport()
    {
        await using var testLspServer = await TestLspServer.CreateAsync(new ClientCapabilities(), TestOutputLogger);

        Assert.False(LspFileChangeWatcher.SupportsLanguageServerHost(testLspServer.LanguageServerHost));
    }

    [Fact]
    public async Task LspFileWatcherSupportedWithClientSupport()
    {
        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger);

        Assert.True(LspFileChangeWatcher.SupportsLanguageServerHost(testLspServer.LanguageServerHost));
    }

    [Fact]
    public async Task CreatingDirectoryWatchRequestsDirectoryWatch()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger);
        var lspFileChangeWatcher = new LspFileChangeWatcher(
            testLspServer.LanguageServerHost,
            testLspServer.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

        var dynamicCapabilitiesRpcTarget = new DynamicCapabilitiesRpcTarget();
        testLspServer.AddClientLocalRpcTarget(dynamicCapabilitiesRpcTarget);

        using var tempRoot = new TempRoot();
        var tempDirectory = tempRoot.CreateDirectory();

        // Try creating a context and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext(new ProjectSystem.WatchedDirectory(tempDirectory.Path, extensionFilter: null));
        await WaitForFileWatcherAsync(testLspServer);

        var watcher = GetSingleFileWatcher(dynamicCapabilitiesRpcTarget);

        Assert.Equal(tempDirectory.Path + Path.DirectorySeparatorChar, watcher.GlobPattern.BaseUri.LocalPath);
        Assert.Equal("**/*", watcher.GlobPattern.Pattern);

        // Get rid of the registration and it should be gone again
        context.Dispose();
        await WaitForFileWatcherAsync(testLspServer);
        Assert.Empty(dynamicCapabilitiesRpcTarget.Registrations);
    }

    [Fact]
    public async Task CreatingFileWatchRequestsFileWatch()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        await using var testLspServer = await TestLspServer.CreateAsync(_clientCapabilitiesWithFileWatcherSupport, TestOutputLogger);
        var lspFileChangeWatcher = new LspFileChangeWatcher(
            testLspServer.LanguageServerHost,
            testLspServer.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());

        var dynamicCapabilitiesRpcTarget = new DynamicCapabilitiesRpcTarget();
        testLspServer.AddClientLocalRpcTarget(dynamicCapabilitiesRpcTarget);

        using var tempRoot = new TempRoot();
        var tempDirectory = tempRoot.CreateDirectory();

        // Try creating a single file watch and ensure we created the registration
        var context = lspFileChangeWatcher.CreateContext();
        var watchedFile = context.EnqueueWatchingFile("Z:\\SingleFile.txt");
        await WaitForFileWatcherAsync(testLspServer);

        var watcher = GetSingleFileWatcher(dynamicCapabilitiesRpcTarget);

        Assert.Equal("Z:\\", watcher.GlobPattern.BaseUri.LocalPath);
        Assert.Equal("SingleFile.txt", watcher.GlobPattern.Pattern);

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
        var registrationJson = Assert.IsType<JObject>(Assert.Single(dynamicCapabilities.Registrations).Value.RegisterOptions);
        var registration = registrationJson.ToObject<DidChangeWatchedFilesRegistrationOptions>()!;

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
        public Task UnregisterCapabilityAsync(UnregistrationParamsWithMisspelling unregistrationParams, CancellationToken _)
        {
            foreach (var unregistration in unregistrationParams.Unregistrations)
                Assert.True(Registrations.TryRemove(unregistration.Id, out var _));

            return Task.CompletedTask;
        }
    }
}
