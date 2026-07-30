// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.HostWorkspace;

public sealed class SyntaxTreeCacheServiceTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { }", "first.cs", "second.cs")]
    public async Task IdenticalDocumentsInDifferentWorkspacesShareGreenNodes(
        string language, string source, string firstPath, string secondPath)
    {
        var hostServices = GetLanguageServerHostServices();
        using var firstWorkspace = new AdhocWorkspace(hostServices, WorkspaceKind.Host);
        using var secondWorkspace = new AdhocWorkspace(hostServices, WorkspaceKind.Host);

        Assert.Same(
            firstWorkspace.Services.GetRequiredService<ISyntaxTreeCacheService>(),
            secondWorkspace.Services.GetRequiredService<ISyntaxTreeCacheService>());

        var firstText = SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha1);
        var secondText = SourceText.From(source, Encoding.Unicode, SourceHashAlgorithm.Sha256);
        var firstDocument = AddDocument(firstWorkspace, language, firstText, firstPath);
        var secondDocument = AddDocument(secondWorkspace, language, secondText, secondPath);

        var firstTree = await firstDocument.GetSyntaxTreeAsync(CancellationToken.None);
        var secondTree = await secondDocument.GetSyntaxTreeAsync(CancellationToken.None);
        Assert.NotNull(firstTree);
        Assert.NotNull(secondTree);
        var firstRoot = await firstTree.GetRootAsync(CancellationToken.None);
        var secondRoot = await secondTree.GetRootAsync(CancellationToken.None);

        Assert.NotSame(firstTree, secondTree);
        Assert.NotSame(firstRoot, secondRoot);
        Assert.True(firstRoot.IsIncrementallyIdenticalTo(secondRoot));
        Assert.Equal(firstPath, firstTree.FilePath);
        Assert.Equal(secondPath, secondTree.FilePath);
        Assert.Same(firstText, firstTree.GetText());
        Assert.Same(secondText, secondTree.GetText());
        Assert.NotEqual(firstDocument.Id, secondDocument.Id);

        var changedDocument = secondDocument.WithText(SourceText.From(source + "\r\nclass D { }"));
        var changedTree = await changedDocument.GetSyntaxTreeAsync(CancellationToken.None);
        Assert.NotNull(changedTree);
        var changedRoot = await changedTree.GetRootAsync(CancellationToken.None);
        Assert.False(firstRoot.IsIncrementallyIdenticalTo(changedRoot));
        Assert.Equal(source, firstRoot.ToFullString());
    }

    [Fact]
    public async Task DifferentParseOptionsDoNotShareGreenNodes()
    {
        const string source = "class C { }";
        var hostServices = GetLanguageServerHostServices();
        using var firstWorkspace = new AdhocWorkspace(hostServices, WorkspaceKind.Host);
        using var secondWorkspace = new AdhocWorkspace(hostServices, WorkspaceKind.Host);

        var firstDocument = AddDocument(
            firstWorkspace, LanguageNames.CSharp, SourceText.From(source), "first.cs",
            CSharpParseOptions.Default.WithPreprocessorSymbols("FIRST"));
        var secondDocument = AddDocument(
            secondWorkspace, LanguageNames.CSharp, SourceText.From(source), "second.cs",
            CSharpParseOptions.Default.WithPreprocessorSymbols("SECOND"));

        var firstTree = await firstDocument.GetSyntaxTreeAsync(CancellationToken.None);
        var secondTree = await secondDocument.GetSyntaxTreeAsync(CancellationToken.None);
        Assert.NotNull(firstTree);
        Assert.NotNull(secondTree);
        var firstRoot = await firstTree.GetRootAsync(CancellationToken.None);
        var secondRoot = await secondTree.GetRootAsync(CancellationToken.None);

        Assert.False(firstRoot.IsIncrementallyIdenticalTo(secondRoot));
    }

    [Fact]
    public async Task ConcurrentPublicationConvergesOnOneRoot()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 10);
        var text = SourceText.From("class C { }");
        var key = cache.CreateKey(LanguageNames.CSharp, text, CSharpParseOptions.Default);

        var roots = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(
                () => cache.GetOrAddRoot(key, CSharpSyntaxTree.ParseText(text).GetRoot()))));

        var canonicalRoot = roots[0];
        Assert.All(roots, root => Assert.Same(canonicalRoot, root));
        Assert.Equal(1, cache.GetTestAccessor().EntryCount);
        Assert.True(cache.GetTestAccessor().PublicationRaceCount > 0);
    }

    [Fact]
    public void DeadRootsDoNotConsumeCapacity()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 1);
        var firstText = SourceText.From("class First { }");
        var firstKey = cache.CreateKey(LanguageNames.CSharp, firstText, CSharpParseOptions.Default);
        var weakRoot = AddRootWithoutRetainingIt(cache, firstKey, firstText);

        CollectGarbage();
        Assert.False(weakRoot.TryGetTarget(out _));

        var secondText = SourceText.From("class Second { }");
        var secondKey = cache.CreateKey(LanguageNames.CSharp, secondText, CSharpParseOptions.Default);
        var secondRoot = CSharpSyntaxTree.ParseText(secondText).GetRoot();

        Assert.Same(secondRoot, cache.GetOrAddRoot(secondKey, secondRoot));
        Assert.False(cache.TryGetRoot(firstKey, out _));
        Assert.True(cache.TryGetRoot(secondKey, out var cachedSecondRoot));
        Assert.Same(secondRoot, cachedSecondRoot);
        Assert.Equal(1, cache.GetTestAccessor().EntryCount);
    }

    [Fact]
    public void DeadLookupReleasesCapacity()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 1);
        var text = SourceText.From("class C { }");
        var key = cache.CreateKey(LanguageNames.CSharp, text, CSharpParseOptions.Default);
        var weakRoot = AddRootWithoutRetainingIt(cache, key, text);

        CollectGarbage();
        Assert.False(weakRoot.TryGetTarget(out _));

        Assert.False(cache.TryGetRoot(key, out _));
        Assert.Equal(0, cache.GetTestAccessor().EntryCount);
    }

    [Fact]
    public void LiveRootsEnforceCapacityLimit()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 1);
        var firstText = SourceText.From("class First { }");
        var secondText = SourceText.From("class Second { }");
        var firstKey = cache.CreateKey(LanguageNames.CSharp, firstText, CSharpParseOptions.Default);
        var secondKey = cache.CreateKey(LanguageNames.CSharp, secondText, CSharpParseOptions.Default);
        var firstRoot = CSharpSyntaxTree.ParseText(firstText).GetRoot();
        var secondRoot = CSharpSyntaxTree.ParseText(secondText).GetRoot();

        Assert.Same(firstRoot, cache.GetOrAddRoot(firstKey, firstRoot));
        Assert.Same(secondRoot, cache.GetOrAddRoot(secondKey, secondRoot));

        Assert.True(cache.TryGetRoot(firstKey, out var cachedFirstRoot));
        Assert.Same(firstRoot, cachedFirstRoot);
        Assert.False(cache.TryGetRoot(secondKey, out _));
        Assert.Equal(1, cache.GetTestAccessor().EntryCount);
        Assert.Equal(1, cache.GetTestAccessor().AdmissionBypassCount);
    }

    [Fact]
    public void SaturatedCacheDoesNotScanForEveryAdmission()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 1);
        var firstText = SourceText.From("class First { }");
        var firstKey = cache.CreateKey(LanguageNames.CSharp, firstText, CSharpParseOptions.Default);
        var firstRoot = CSharpSyntaxTree.ParseText(firstText).GetRoot();
        Assert.Same(firstRoot, cache.GetOrAddRoot(firstKey, firstRoot));

        for (var i = 0; i < 10; i++)
        {
            var text = SourceText.From($"class C{i} {{ }}");
            var key = cache.CreateKey(LanguageNames.CSharp, text, CSharpParseOptions.Default);
            var root = CSharpSyntaxTree.ParseText(text).GetRoot();
            Assert.Same(root, cache.GetOrAddRoot(key, root));
        }

        var accessor = cache.GetTestAccessor();
        Assert.Equal(1, accessor.CleanupCount);
        Assert.Equal(10, accessor.AdmissionBypassCount);
    }

    [Fact]
    public void CacheKeysIncludeLanguage()
    {
        var cache = new SyntaxTreeCacheService(maximumEntryCount: 2);
        var text = SourceText.From("class C { }");
        var csharpKey = cache.CreateKey(LanguageNames.CSharp, text, CSharpParseOptions.Default);
        var otherLanguageKey = cache.CreateKey("Other", text, CSharpParseOptions.Default);
        var csharpRoot = CSharpSyntaxTree.ParseText(text).GetRoot();
        var otherRoot = CSharpSyntaxTree.ParseText(text).GetRoot();

        Assert.Same(csharpRoot, cache.GetOrAddRoot(csharpKey, csharpRoot));
        Assert.Same(otherRoot, cache.GetOrAddRoot(otherLanguageKey, otherRoot));
        Assert.NotSame(csharpRoot, otherRoot);
    }

    private HostServices GetLanguageServerHostServices()
    {
        var exportProvider = LanguageServerTestComposition.GetSharedExportProvider(ServerConfigurationWithoutDevKit, LoggerFactory);
        return exportProvider.GetExportedValue<HostServicesProvider>().HostServices;
    }

    private static Document AddDocument(
        AdhocWorkspace workspace,
        string language,
        SourceText text,
        string filePath,
        ParseOptions? parseOptions = null)
    {
        var project = workspace.AddProject(Guid.NewGuid().ToString(), language);
        if (parseOptions is not null)
        {
            Assert.True(workspace.TryApplyChanges(project.Solution.WithProjectParseOptions(project.Id, parseOptions)));
            project = workspace.CurrentSolution.GetProject(project.Id);
            Assert.NotNull(project);
        }

        var documentId = DocumentId.CreateNewId(project.Id);
        var loader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), filePath));
        return workspace.AddDocument(DocumentInfo.Create(
            documentId, Path.GetFileName(filePath), loader: loader, filePath: filePath));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<SyntaxNode> AddRootWithoutRetainingIt(
        SyntaxTreeCacheService cache, SyntaxTreeCacheKey key, SourceText text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        Assert.Same(root, cache.GetOrAddRoot(key, root));
        return new(root);
    }

    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
