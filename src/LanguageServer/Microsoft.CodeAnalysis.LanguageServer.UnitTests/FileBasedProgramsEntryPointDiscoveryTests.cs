// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.LanguageServer.FileBasedPrograms;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class FileBasedProgramsEntryPointDiscoveryTests : AbstractLanguageServerProtocolTests, IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly TempRoot _tempRoot;
    private readonly TempDirectory _mefCacheDirectory;

    private readonly List<string> _additionalDirectoriesToDelete = [];

    public FileBasedProgramsEntryPointDiscoveryTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        _tempRoot = new();
        _mefCacheDirectory = _tempRoot.CreateDirectory();
    }

    protected override async ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            _loggerFactory,
            includeDevKitComponents: false,
            cacheDirectory: _mefCacheDirectory.Path,
            extensionPaths: []);

        return exportProvider;
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
        _loggerProvider.Dispose();
        _loggerFactory.Dispose();

        foreach (var directory in _additionalDirectoriesToDelete)
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private void DeferDeleteCacheDirectory(string workspacePath)
    {
        _additionalDirectoriesToDelete.Add(VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(workspacePath));
    }

    /// <summary>Verify that multiple invocations of 'actualFactory' result in the same 'expected' sequence.</summary>
    private void AssertSequenceEqualAndStable<T>(IEnumerable<T> expected, Func<IEnumerable<T>> actualFactory)
    {
        AssertEx.SequenceEqual(expected, actualFactory());
        AssertEx.SequenceEqual(expected, actualFactory());
    }

    [Fact]
    public async Task TestDiscovery_Simple()
    {
        // Simple case
        // tempDir/
        //   App.cs
        //   Ordinary.cs

        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var appFile = tempDir.CreateFile("App.cs").WriteAllText(appText);
        // Note: having '#:' is not enough for discovery to detect a file. The file needs to start with '#!'.
        var ordinaryText = """
            #:sdk Microsoft.NET.Sdk
            public class Ordinary { }
            """;
        var ordinaryFile = tempDir.CreateFile("Ordinary.cs").WriteAllText(ordinaryText);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        AssertSequenceEqualAndStable([appFile.Path], () => discovery.FindEntryPoints(tempDir.Path));

        // Changed but still has '#!'
        appFile.WriteAllText(appText + """

            Console.WriteLine("Additional content");
            """);
        AssertEx.SequenceEqual([appFile.Path], discovery.FindEntryPoints(tempDir.Path));

        // Deleted from disk
        File.Delete(appFile.Path);
        AssertEx.Empty(discovery.FindEntryPoints(tempDir.Path));

        // Put back on disk
        appFile.WriteAllText(appText);
        AssertEx.SequenceEqual([appFile.Path], discovery.FindEntryPoints(tempDir.Path));

        // Changed and no longer has '#!'
        appFile.WriteAllText("""
            Console.WriteLine("No more #! at start of file");
            """);
        AssertEx.Empty(discovery.FindEntryPoints(tempDir.Path));

        // Changed and again has '#!'
        appFile.WriteAllText(appText);
        AssertEx.SequenceEqual([appFile.Path], discovery.FindEntryPoints(tempDir.Path));
    }

    [Fact]
    public async Task TestDiscovery_IgnoredFolders()
    {
        // Demonstrate ignored folders behavior
        // tempDir/
        //   artifacts/App1.cs
        //   App2.cs

        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var artifactsDir = tempDir.CreateDirectory("artifacts");
        var app1Text = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var app1File = artifactsDir.CreateFile("App1.cs").WriteAllText(app1Text);

        var app2Text = app1Text;
        var app2File = tempDir.CreateFile("App2.cs").WriteAllText(app2Text);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        AssertSequenceEqualAndStable([app2File.Path], () => discovery.FindEntryPoints(tempDir.Path));
    }

    [Fact]
    public async Task TestDiscovery_CsprojInCone()
    {
        // Demonstrate csproj-in-cone behavior
        // tempDir/
        //   Project/
        //     Project.csproj
        //     Program.cs
        //   App.cs

        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var projectDir = tempDir.CreateDirectory("Project");
        var csprojFile = projectDir.CreateFile("Project.csproj");

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var programFile = projectDir.CreateFile("Program.cs").WriteAllText(appText);
        var appFile = tempDir.CreateFile("App1.cs").WriteAllText(appText);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        AssertSequenceEqualAndStable([appFile.Path], () => discovery.FindEntryPoints(tempDir.Path));

        // Delete the csproj file
        File.Delete(csprojFile.Path);
        AssertSequenceEqualAndStable([appFile.Path, programFile.Path], () => discovery.FindEntryPoints(tempDir.Path));
    }

    [Fact]
    public async Task TestDiscovery_Option_EnableFileBasedPrograms_True()
    {
        // Ensure discovery occurs when relevant options are enabled
        // Note: the option is checked in the higher level API, so we need to verify the effects in project system.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var appFile = tempDir.CreateFile("App1.cs").WriteAllText(appText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace: false, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            OptionUpdater = options => options.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, true),
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace1" }
            ]
        });

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        await discovery.FindAndLoadEntryPointsAsync();
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        var (workspace, document) = await GetRequiredLspWorkspaceAndDocumentAsync(CreateAbsoluteDocumentUri(appFile.Path), testLspServer);
        Assert.Equal(WorkspaceKind.Host, workspace.Kind);
        Assert.NotNull(document);
    }

    [Fact]
    public async Task TestDiscovery_Option_EnableFileBasedPrograms_False()
    {
        // Ensure discovery doesn't occur when 'dotnet.projects.enableFileBasedPrograms: false' is set
        // Note: the option is checked in the higher level API, so we need to verify the effects in project system.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var appFile = tempDir.CreateFile("App1.cs").WriteAllText(appText);

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace: false, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            OptionUpdater = options => options.SetGlobalOption(LanguageServerProjectSystemOptionsStorage.EnableFileBasedPrograms, false),
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = "workspace1" }
            ]
        });

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        await discovery.FindAndLoadEntryPointsAsync();
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        var (workspace, document) = await GetLspWorkspaceAndDocumentAsync(CreateAbsoluteDocumentUri(appFile.Path), testLspServer);
        Assert.Null(workspace);
        Assert.Null(document);
    }

    [Fact]
    public async Task TestDiscovery_Option_EnableAutomaticDiscovery_False()
    {
        // Ensure discovery doesn't occur when 'dotnet.fileBasedApps.enableAutomaticDiscovery: false' is set
        // Note: the option is checked in the higher level API, so we need to verify the effects in project system.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");
            """;
        var appFile = tempDir.CreateFile("App1.cs").WriteAllText(appText);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        await discovery.FindAndLoadEntryPointsAsync();
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        var (workspace, document) = await GetLspWorkspaceAndDocumentAsync(CreateAbsoluteDocumentUri(appFile.Path), testLspServer);
        Assert.Null(workspace);
        Assert.Null(document);
    }

    [Fact]
    public async Task TestDiscovery_UTF8_BOM()
    {
        // File starting with UTF-8 BOM followed by '#!' should be discovered
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        var appText = """
            #!/usr/bin/env dotnet
            #:sdk Microsoft.NET.Sdk
            Console.WriteLine("Hello World");

            """;
        var bomAppText = "\uFEFF" + appText;
        var appFile = tempDir.CreateFile("App.cs").WriteAllText(bomAppText);
        var ordinaryFile = tempDir.CreateFile("Ordinary.cs").WriteAllText("public class Ordinary { }");

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);

        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
        AssertEx.SequenceEqual([appFile.Path], discovery.FindEntryPoints(tempDir.Path));
    }

    private Task<TestLspServer> CreateDiscoveryTestServerAsync(string workspacePath)
        => CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace: false, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            // Disable background discovery so it doesn't race with direct FindEntryPoints calls.
            OptionUpdater = options => options.SetGlobalOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery, false),
            WorkspaceFolders =
            [
                new() { DocumentUri = CreateAbsoluteDocumentUri(workspacePath), Name = "workspace1" }
            ]
        });

    private static async Task<(Workspace? workspace, Document? document)> GetLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None).ConfigureAwait(false);
        return (workspace, document as Document);
    }

    private static async Task<(Workspace workspace, Document document)> GetRequiredLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, document) = await GetLspWorkspaceAndDocumentAsync(uri, testLspServer);
        Assert.NotNull(workspace);
        Assert.NotNull(document);
        return (workspace, document);
    }

    [Fact]
    public async Task Swap_ReplaceFBAWithNonFBA()
    {
        // Swap an FBA out for non-FBA at the same path 'sub1/File1.cs'.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File1.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File2.cs"), OrdinaryCsContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        File.Move(Path.Combine(tempDir.Path, @"sub1/File1.cs"), Path.Combine(tempDir.Path, @"sub1/File4.cs"));
        File.Move(Path.Combine(tempDir.Path, @"sub1/File2.cs"), Path.Combine(tempDir.Path, @"sub1/File1.cs"));

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache - should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Swap_ReplaceNonFBAWithFBA()
    {
        // Swap a non-FBA out for FBA at the same path 'sub/File1.cs'.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File1.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File2.cs"), FbaContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        File.Move(Path.Combine(tempDir.Path, @"sub1/File1.cs"), Path.Combine(tempDir.Path, @"sub1/File4.cs"));
        File.Move(Path.Combine(tempDir.Path, @"sub1/File2.cs"), Path.Combine(tempDir.Path, @"sub1/File1.cs"));

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache — should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Swap_ReplaceFBADirectoryWithNonFBADirectory()
    {
        // Swap a directory containing FBA out for a directory containing non-FBA at 'sub1/File1.cs'.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1"));
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub2"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File1.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub2/File1.cs"), OrdinaryCsContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        Directory.Move(Path.Combine(tempDir.Path, @"sub1"), Path.Combine(tempDir.Path, @"sub4"));
        Directory.Move(Path.Combine(tempDir.Path, @"sub2"), Path.Combine(tempDir.Path, @"sub1"));

        // Discovery with cache - should match
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Swap_ReplaceNonFBADirectoryWithFBADirectory()
    {
        // Swap a directory containing non-FBA out for a directory containing FBA at the same path 'sub1/File1.cs'.
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1"));
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub2"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/File1.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub2/File1.cs"), FbaContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        Directory.Move(Path.Combine(tempDir.Path, @"sub1"), Path.Combine(tempDir.Path, @"sub4"));
        Directory.Move(Path.Combine(tempDir.Path, @"sub2"), Path.Combine(tempDir.Path, @"sub1"));

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache — should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fuzz_1()
    {
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        File.WriteAllText(Path.Combine(tempDir.Path, @"Fba0.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"Fba1.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"Ordinary2.cs"), OrdinaryCsContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        File.WriteAllText(Path.Combine(tempDir.Path, @"New102.csproj"), CsprojContent);
        File.Delete(Path.Combine(tempDir.Path, @"Fba0.cs"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"NewOrd22.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"Ordinary2.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"Ordinary2.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"NewOrd5.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"New79.csproj"), CsprojContent);

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache — should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(cachedResult, uncachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fuzz_2()
    {
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        File.WriteAllText(Path.Combine(tempDir.Path, @"Fba0.cs"), FbaContent);
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"deep/nested"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"deep/nested/Fba1.cs"), FbaContent);
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"deep/nested"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"deep/nested/Project2.csproj"), CsprojContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"Project3.csproj"), CsprojContent);
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"deep/nested/sub3"));

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        File.WriteAllText(Path.Combine(tempDir.Path, @"NewOrd40.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"deep/nested/sub3/New52.csproj"), CsprojContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"deep/nested/NewOrd20.cs"), OrdinaryCsContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"deep/nested/Fba1.cs"), OrdinaryCsContent);

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache — should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(cachedResult, uncachedResult, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fuzz_3()
    {
        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        // Setup
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1"));
        Directory.CreateDirectory(Path.Combine(tempDir.Path, @"sub1/sub3"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"Project0.csproj"), CsprojContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/sub3/Fba1.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/Fba2.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/Fba3.cs"), FbaContent);
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/Ordinary4.cs"), OrdinaryCsContent);

        // First discovery (no cache)
        var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

        // Edits
        File.Delete(Path.Combine(tempDir.Path, @"Project0.csproj"));
        File.WriteAllText(Path.Combine(tempDir.Path, @"sub1/sub3/NewFba64.cs"), FbaContent);

        // Discovery with cache
        var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        // Delete cache
        var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
        Directory.Delete(cacheDirectory, recursive: true);

        // Discovery without cache — should match
        var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        AssertEx.SequenceEqual(cachedResult, uncachedResult, StringComparer.OrdinalIgnoreCase);
    }

    #region Fuzzer

    private const string FbaContent = """
        #!/usr/bin/env dotnet
        #:sdk Microsoft.NET.Sdk
        Console.WriteLine("hello");

        """;
    private const string OrdinaryCsContent = """
        public class C {}

        """;
    private const string CsprojContent = "<Project />";

    /// <summary>
    /// Describes a single filesystem operation performed during a fuzz iteration.
    /// </summary>
    private abstract record FuzzOp
    {
        protected static string NormalizeForCSharp(string relativePath) => relativePath.Replace('\\', '/');

        public abstract string ToCSharp(string tempDirVar);

        /// <summary>Creates a directory at the given relative path.</summary>
        internal sealed record CreateDir(string RelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"Directory.CreateDirectory(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(RelativePath)}\"));";
        }

        /// <summary>Writes a .cs file with file-based-app content (starts with '#!').</summary>
        internal sealed record WriteFbaFile(string RelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"File.WriteAllText(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(RelativePath)}\"), FbaContent);";
        }

        /// <summary>Writes a .cs file without file-based-app content (no '#!' at start).</summary>
        internal sealed record WriteOrdinaryCs(string RelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"File.WriteAllText(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(RelativePath)}\"), OrdinaryCsContent);";
        }

        /// <summary>Writes a .csproj file.</summary>
        internal sealed record WriteCsproj(string RelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"File.WriteAllText(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(RelativePath)}\"), CsprojContent);";
        }

        /// <summary>Deletes a file.</summary>
        internal sealed record DeleteFile(string RelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"File.Delete(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(RelativePath)}\"));";
        }

        /// <summary>Renames/moves a file.</summary>
        internal sealed record RenameFile(string OldRelativePath, string NewRelativePath) : FuzzOp
        {
            public override string ToCSharp(string tempDirVar) => $"File.Move(Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(OldRelativePath)}\"), Path.Combine({tempDirVar}.Path, @\"{NormalizeForCSharp(NewRelativePath)}\"));";
        }
    }

    /// <summary>
    /// Tracks what files exist in the virtual workspace to enable the fuzzer
    /// to generate valid operations (e.g. only delete files that exist).
    /// </summary>
    private sealed class FuzzWorkspace
    {
        private readonly string _rootPath;
        private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _files = new(StringComparer.OrdinalIgnoreCase);

        public FuzzWorkspace(string rootPath)
        {
            _rootPath = rootPath;
            _directories.Add(""); // root
        }

        public IReadOnlyCollection<string> Directories => _directories;
        public IReadOnlyCollection<string> Files => _files;

        public string FullPath(string relativePath) => Path.Combine(_rootPath, relativePath);

        public void Apply(FuzzOp op)
        {
            switch (op)
            {
                case FuzzOp.CreateDir createDir:
                    _directories.Add(createDir.RelativePath);
                    Directory.CreateDirectory(FullPath(createDir.RelativePath));
                    break;
                case FuzzOp.WriteFbaFile writeFba:
                    _files.Add(writeFba.RelativePath);
                    File.WriteAllText(FullPath(writeFba.RelativePath), FbaContent);
                    break;
                case FuzzOp.WriteOrdinaryCs writeCs:
                    _files.Add(writeCs.RelativePath);
                    File.WriteAllText(FullPath(writeCs.RelativePath), OrdinaryCsContent);
                    break;
                case FuzzOp.WriteCsproj writeCsproj:
                    _files.Add(writeCsproj.RelativePath);
                    File.WriteAllText(FullPath(writeCsproj.RelativePath), CsprojContent);
                    break;
                case FuzzOp.DeleteFile deleteFile:
                    _files.Remove(deleteFile.RelativePath);
                    File.Delete(FullPath(deleteFile.RelativePath));
                    break;
                case FuzzOp.RenameFile rename:
                    _files.Remove(rename.OldRelativePath);
                    _files.Add(rename.NewRelativePath);
                    File.Move(FullPath(rename.OldRelativePath), FullPath(rename.NewRelativePath));
                    break;
            }
        }
    }

    private static readonly string[] s_dirNames = ["sub1", "sub2", "sub3", "deep" + Path.DirectorySeparatorChar + "nested"];

    /// <summary>
    /// Generates a random "setup" operation (creating directories and files).
    /// </summary>
    private static FuzzOp GenerateSetupOp(Random random, FuzzWorkspace workspace)
    {
        // Weighted: create dirs early, then files
        var dirList = workspace.Directories.ToArray();
        if (dirList.Length < 4 && random.Next(3) == 0)
        {
            // Create a subdirectory
            var parentDir = dirList[random.Next(dirList.Length)];
            var name = s_dirNames[random.Next(s_dirNames.Length)];
            var relativePath = parentDir.Length == 0 ? name : Path.Combine(parentDir, name);
            return new FuzzOp.CreateDir(relativePath);
        }

        // Create a file in a random directory
        var dir = dirList[random.Next(dirList.Length)];
        var fileIndex = workspace.Files.Count;
        return random.Next(4) switch
        {
            0 => new FuzzOp.WriteFbaFile(Path.Combine(dir, $"Fba{fileIndex}.cs")),
            1 => new FuzzOp.WriteOrdinaryCs(Path.Combine(dir, $"Ordinary{fileIndex}.cs")),
            2 => new FuzzOp.WriteCsproj(Path.Combine(dir, $"Project{fileIndex}.csproj")),
            _ => new FuzzOp.WriteFbaFile(Path.Combine(dir, $"Fba{fileIndex}.cs")),
        };
    }

    /// <summary>
    /// Generates a random "edit" operation (modifying, deleting, renaming files, or creating/deleting csproj).
    /// </summary>
    private static FuzzOp? GenerateEditOp(Random random, FuzzWorkspace workspace)
    {
        var files = workspace.Files.ToArray();
        if (files.Length == 0)
            return null;

        var dirList = workspace.Directories.ToArray();
        var choice = random.Next(7);

        if (choice == 0)
            return new FuzzOp.DeleteFile(files[random.Next(files.Length)]);

        if (choice == 1)
        {
            var oldPath = files[random.Next(files.Length)];
            var dir = dirList[random.Next(dirList.Length)];
            var newPath = Path.Combine(dir, "moved_" + Path.GetFileName(oldPath));
            if (workspace.Files.Contains(newPath))
                return null;
            return new FuzzOp.RenameFile(oldPath, newPath);
        }

        if (choice == 2)
            return new FuzzOp.WriteFbaFile(Path.Combine(dirList[random.Next(dirList.Length)], $"NewFba{workspace.Files.Count + random.Next(100)}.cs"));

        if (choice == 3)
            return new FuzzOp.WriteOrdinaryCs(Path.Combine(dirList[random.Next(dirList.Length)], $"NewOrd{workspace.Files.Count + random.Next(100)}.cs"));

        if (choice == 4)
            return new FuzzOp.WriteCsproj(Path.Combine(dirList[random.Next(dirList.Length)], $"New{workspace.Files.Count + random.Next(100)}.csproj"));

        var csFiles = files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (csFiles.Length == 0)
            return null;

        if (choice == 5)
            return new FuzzOp.WriteFbaFile(csFiles[random.Next(csFiles.Length)]);

        if (choice == 6)
            return new FuzzOp.WriteOrdinaryCs(csFiles[random.Next(csFiles.Length)]);

        throw ExceptionUtilities.UnexpectedValue(choice);
    }

    [Fact]
    public async Task Fuzz()
    {
        // Explicitly seed the random so that if we need to manually edit and repro the fuzzing process locally, the logs will help us to do that
        var seed = Random.Shared.Next();
        var random = new Random(seed);
        _testOutputHelper.WriteLine($"Random seed: {seed}");

        var tempDir = _tempRoot.CreateDirectory();
        DeferDeleteCacheDirectory(tempDir.Path);

        await using var testLspServer = await CreateDiscoveryTestServerAsync(tempDir.Path);
        var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();

        for (var iteration = 0; iteration < 1000; iteration++)
        {
            var workspace = new FuzzWorkspace(tempDir.Path);
            var setupOps = new List<FuzzOp>();
            var editOps = new List<FuzzOp>();

            try
            {
                // Clean workspace for each iteration
                foreach (var entry in Directory.EnumerateFileSystemEntries(tempDir.Path))
                {
                    if (File.Exists(entry))
                        File.Delete(entry);
                    else if (Directory.Exists(entry))
                        Directory.Delete(entry, recursive: true);
                }

                // Delete cache from any prior iteration
                var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
                if (Directory.Exists(cacheDirectory))
                    Directory.Delete(cacheDirectory, recursive: true);

                // Step 1: Generate random initial filesystem
                var setupCount = random.Next(3, 12);
                for (var i = 0; i < setupCount; i++)
                {
                    var op = GenerateSetupOp(random, workspace);
                    setupOps.Add(op);
                    workspace.Apply(op);
                }

                // Step 2: Discover entry points without cache
                var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

                // Step 3: Random edits
                var editCount = random.Next(1, 8);
                for (var i = 0; i < editCount; i++)
                {
                    var op = GenerateEditOp(random, workspace);
                    if (op != null)
                    {
                        editOps.Add(op);
                        workspace.Apply(op);
                    }
                }

                // Step 4: Discover entry points using cache (cache was written by step 2)
                var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

                // Step 5: Delete the cache
                if (Directory.Exists(cacheDirectory))
                    Directory.Delete(cacheDirectory, recursive: true);

                // Step 6: Discover without cache — should match step 4
                var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

                AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase,
                    $"Iteration {iteration}: Cached result differs from uncached result.");
            }
            catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
            {
                // Directories can randomly fail to delete etc when we are thrashing the disk.
                // Not a big deal and not a reason to fail the test, just move on to the next iteration instead.
                _testOutputHelper.WriteLine($"IO exception during fuzz testing: {ex.Message}");
            }
            catch (Exception)
            {
                // Dump reproducible test case
                DumpFuzzReproCase(iteration, setupOps, editOps);
                throw;
            }
        }
    }

    private void DumpFuzzReproCase(int iteration, List<FuzzOp> setupOps, List<FuzzOp> editOps)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($$"""

                [Fact]
                public async Task Fuzz_{{iteration}}()
                {
                    var tempDir = _tempRoot.CreateDirectory();
                    DeferDeleteCacheDirectory(tempDir.Path);
                    sb.AppendLine();

                    await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace: false, new InitializationOptions
                    {
                        ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
                        OptionUpdater = options => options.SetGlobalOption(FileBasedAppsOptionsStorage.EnableAutomaticDiscovery, false),
                        WorkspaceFolders =
                        [
                            new() { DocumentUri = CreateAbsoluteDocumentUri(tempDir.Path), Name = \"workspace1\" }
                        ]
                    });
                    var discovery = testLspServer.GetRequiredLspService<FileBasedProgramsEntryPointDiscovery>();
                    sb.AppendLine();

                    // Setup
            """);
        foreach (var op in setupOps)
            sb.AppendLine($"        {op.ToCSharp("tempDir")}");

        sb.AppendLine("""

                    // First discovery (no cache)
                    var firstResult = discovery.FindEntryPoints(tempDir.Path).ToArray();

                    // Edits
            """);
        foreach (var op in editOps)
            sb.AppendLine($"        {op.ToCSharp("tempDir")}");

        sb.AppendLine("""

                    // Discovery with cache
                    var cachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();

                    // Delete cache
                    var cacheDirectory = VirtualProjectXmlProvider.GetDiscoveryCacheDirectory(tempDir.Path);
                    Directory.Delete(cacheDirectory, recursive: true);

                    // Discovery without cache — should match
                    var uncachedResult = discovery.FindEntryPoints(tempDir.Path).Order(StringComparer.OrdinalIgnoreCase).ToArray();
                    AssertEx.SequenceEqual(uncachedResult, cachedResult, StringComparer.OrdinalIgnoreCase);
                }
            """);

        _testOutputHelper.WriteLine(sb.ToString());
    }

    #endregion
}
