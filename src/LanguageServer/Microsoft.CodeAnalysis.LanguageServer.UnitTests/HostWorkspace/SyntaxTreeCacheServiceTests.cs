// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
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
    public async Task ConcurrentPublicationConvergesOnOneGreenRoot()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");

        var trees = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(
                () => GetOrCreateTree(cache, text))));

        var canonicalRoot = trees[0].GetRoot();
        Assert.All(trees, tree => Assert.True(canonicalRoot.IsIncrementallyIdenticalTo(tree.GetRoot())));
    }

    [Fact]
    public void OlderLiveRootRemainsAvailableAfterNewerRootIsCollected()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var firstTree = GetOrCreateTree(cache, text);
        var firstRoot = firstTree.GetRoot();
        var secondRootReference = ObjectReference.CreateFromFactory(
            static state =>
            {
                var root = GetOrCreateTree(state.cache, state.text).GetRoot();
                Assert.True(state.expectedSharedRoot.IsIncrementallyIdenticalTo(root));
                return root;
            },
            (cache, text, expectedSharedRoot: firstRoot));

        secondRootReference.AssertReleased();

        var thirdRoot = GetOrCreateTree(cache, text).GetRoot();
        Assert.True(firstRoot.IsIncrementallyIdenticalTo(thirdRoot));
    }

    [Fact]
    public void PeriodicCleanupReleasesDeadKeys()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var firstOptionsReference = ObjectReference.CreateFromFactory(
            static state =>
            {
                // The cache key strongly retains these unique options, so their lifetime lets this test observe
                // whether periodic cleanup removed the entry after its weak roots died.
                var options = CSharpParseOptions.Default.WithPreprocessorSymbols(Guid.NewGuid().ToString());
                _ = GetOrCreateTree(state.cache, state.text, options);
                return options;
            },
            (cache, text: SourceText.From("class First { }")));
        firstOptionsReference.AssertHeld();

        cache.GetTestAccessor().TriggerCleanupOnNextAddedRoot();
        _ = GetOrCreateTree(cache, SourceText.From("class Second { }"));

        firstOptionsReference.AssertReleased();
    }

    [Fact]
    public void DeadEntryIsReplaced()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var rootReference = ObjectReference.CreateFromFactory(
            static state => GetOrCreateTree(state.cache, state.text).GetRoot(),
            (cache, text));
        rootReference.AssertReleased();

        var replacementRoot = GetOrCreateTree(cache, text).GetRoot();
        var cachedReplacementRoot = GetOrCreateTree(cache, text).GetRoot();
        Assert.True(replacementRoot.IsIncrementallyIdenticalTo(cachedReplacementRoot));
    }

    [Fact]
    public void DifferentLanguagesDoNotShareGreenNodes()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var csharpRoot = GetOrCreateTree(cache, text).GetRoot();
        var visualBasicRoot = GetOrCreateVisualBasicTree(cache, text).GetRoot();

        Assert.False(csharpRoot.IsIncrementallyIdenticalTo(visualBasicRoot));
    }

    private HostServices GetLanguageServerHostServices()
    {
        var exportProvider = LanguageServerTestComposition.GetSharedExportProvider(ServerConfigurationWithoutDevKit, LoggerFactory);
        return exportProvider.GetExportedValue<HostServicesProvider>().HostServices;
    }

    private AdhocWorkspace CreateCacheWorkspace(out SyntaxTreeCacheService cache)
    {
        var workspace = new AdhocWorkspace(GetLanguageServerHostServices(), WorkspaceKind.Host);
        cache = (SyntaxTreeCacheService)workspace.Services.GetRequiredService<ISyntaxTreeCacheService>();
        return workspace;
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

    private static SyntaxTree GetOrCreateTree(
        SyntaxTreeCacheService cache,
        SourceText text,
        CSharpParseOptions? options = null)
    {
        options ??= CSharpParseOptions.Default;
        return cache.GetOrCreateSyntaxTree(
            text,
            options,
            static (state, _) => CSharpSyntaxTree.ParseText(state.text, state.options),
            static (root, state) => CSharpSyntaxTree.Create((CSharpSyntaxNode)root, state.options),
            (text, options),
            CancellationToken.None);
    }

    private static SyntaxTree GetOrCreateVisualBasicTree(
        SyntaxTreeCacheService cache,
        SourceText text,
        VisualBasicParseOptions? options = null)
    {
        options ??= VisualBasicParseOptions.Default;
        return cache.GetOrCreateSyntaxTree(
            text,
            options,
            static (state, _) => VisualBasicSyntaxTree.ParseText(state.text, state.options),
            static (root, state) => VisualBasicSyntaxTree.Create((VisualBasicSyntaxNode)root, state.options),
            (text, options),
            CancellationToken.None);
    }

}
