// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.HostWorkspace;

public sealed class SyntaxTreeCacheServiceTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerHostTests(testOutputHelper)
{
    private static readonly TestComposition s_composition = FeaturesTestCompositions.Features.AddParts(
        typeof(ServerConfigurationFactory),
        typeof(SyntaxTreeCacheServiceFactory));

    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { }", "first.cs", "second.cs")]
    [InlineData(LanguageNames.VisualBasic, "Class C\nEnd Class", "first.vb", "second.vb")]
    public async Task IdenticalDocumentsInDifferentWorkspacesShareGreenNodes(
        string language, string source, string firstPath, string secondPath)
    {
        var exportProvider = GetLanguageServerExportProvider();
        var firstText = SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha1);
        var secondText = SourceText.From(source, Encoding.Unicode, SourceHashAlgorithm.Sha256);
        using var firstWorkspace = await CreateWorkspaceAsync(exportProvider, language, firstText, firstPath);
        using var secondWorkspace = await CreateWorkspaceAsync(exportProvider, language, secondText, secondPath);

        Assert.Same(
            firstWorkspace.Services.GetRequiredService<ISyntaxTreeCacheService>(),
            secondWorkspace.Services.GetRequiredService<ISyntaxTreeCacheService>());

        var firstDocument = firstWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var secondDocument = secondWorkspace.CurrentSolution.Projects.Single().Documents.Single();

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
        Assert.Same(Encoding.UTF8, firstTree.Encoding);
        Assert.Same(Encoding.Unicode, secondTree.Encoding);
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
        var exportProvider = GetLanguageServerExportProvider();
        using var firstWorkspace = await CreateWorkspaceAsync(
            exportProvider, LanguageNames.CSharp, SourceText.From(source), "first.cs",
            CSharpParseOptions.Default.WithPreprocessorSymbols("FIRST"));
        using var secondWorkspace = await CreateWorkspaceAsync(
            exportProvider, LanguageNames.CSharp, SourceText.From(source), "second.cs",
            CSharpParseOptions.Default.WithPreprocessorSymbols("SECOND"));
        var firstDocument = firstWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        var secondDocument = secondWorkspace.CurrentSolution.Projects.Single().Documents.Single();

        var firstTree = await firstDocument.GetSyntaxTreeAsync(CancellationToken.None);
        var secondTree = await secondDocument.GetSyntaxTreeAsync(CancellationToken.None);
        Assert.NotNull(firstTree);
        Assert.NotNull(secondTree);
        var firstRoot = await firstTree.GetRootAsync(CancellationToken.None);
        var secondRoot = await secondTree.GetRootAsync(CancellationToken.None);

        // This could eventually share trees when the differing parse options do not affect the parsed result.
        Assert.False(firstRoot.IsIncrementallyIdenticalTo(secondRoot));
    }

    [Fact]
    public void NonDaemonDoesNotProvideCache()
    {
        using var workspace = new TestWorkspace(GetLanguageServerExportProvider(isDaemon: false), WorkspaceKind.Host);
        Assert.Null(workspace.Services.GetService<ISyntaxTreeCacheService>());
    }

    [Fact]
    public async Task ConcurrentPublicationConvergesOnOneGreenRoot()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");

        var trees = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => Task.Run(
                () => GetOrCreateCSharpTree(cache, text))));

        var canonicalRoot = trees[0].GetRoot();
        Assert.All(trees, tree => Assert.True(canonicalRoot.IsIncrementallyIdenticalTo(tree.GetRoot())));
    }

    [Fact]
    public void OlderLiveRootRemainsAvailableAfterNewerRootIsCollected()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var firstTree = GetOrCreateCSharpTree(cache, text);
        var firstRoot = firstTree.GetRoot();
        var secondRootReference = ObjectReference.CreateFromFactory(
            static state =>
            {
                var root = GetOrCreateCSharpTree(state.cache, state.text).GetRoot();
                Assert.True(state.expectedSharedRoot.IsIncrementallyIdenticalTo(root));
                return root;
            },
            (cache, text, expectedSharedRoot: firstRoot));

        secondRootReference.AssertReleased();

        var thirdRoot = GetOrCreateCSharpTree(cache, text).GetRoot();
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
                _ = GetOrCreateCSharpTree(state.cache, state.text, options);
                return options;
            },
            (cache, text: SourceText.From("class First { }")));
        firstOptionsReference.AssertHeld();

        cache.GetTestAccessor().TriggerCleanupOnNextAddedRoot();
        _ = GetOrCreateCSharpTree(cache, SourceText.From("class Second { }"));

        firstOptionsReference.AssertReleased();
    }

    [Fact]
    public void DeadEntryIsReplaced()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var rootReference = ObjectReference.CreateFromFactory(
            static state => GetOrCreateCSharpTree(state.cache, state.text).GetRoot(),
            (cache, text));
        rootReference.AssertReleased();

        var replacementRoot = GetOrCreateCSharpTree(cache, text).GetRoot();
        var cachedReplacementRoot = GetOrCreateCSharpTree(cache, text).GetRoot();
        Assert.True(replacementRoot.IsIncrementallyIdenticalTo(cachedReplacementRoot));
    }

    [Fact]
    public void DifferentLanguagesDoNotShareGreenNodes()
    {
        using var workspace = CreateCacheWorkspace(out var cache);
        var text = SourceText.From("class C { }");
        var csharpTree = GetOrCreateCSharpTree(cache, text);
        var visualBasicTree = GetOrCreateVisualBasicTree(cache, text);

        Assert.NotEqual(csharpTree.GetType(), visualBasicTree.GetType());
        Assert.False(csharpTree.GetRoot().IsIncrementallyIdenticalTo(visualBasicTree.GetRoot()));
    }

    private ExportProvider GetLanguageServerExportProvider(bool isDaemon = true)
    {
        var serverConfiguration = ServerConfigurationWithoutDevKit with { IsDaemon = isDaemon };
        var exportProvider = s_composition.ExportProviderFactory.CreateExportProvider();
        exportProvider.GetExportedValue<ServerConfigurationFactory>().InitializeConfiguration(serverConfiguration);
        return exportProvider;
    }

    private TestWorkspace CreateCacheWorkspace(out SyntaxTreeCacheService cache)
    {
        var workspace = new TestWorkspace(GetLanguageServerExportProvider(), WorkspaceKind.Host);
        cache = (SyntaxTreeCacheService)workspace.Services.GetRequiredService<ISyntaxTreeCacheService>();
        return workspace;
    }

    private static async Task<TestWorkspace> CreateWorkspaceAsync(
        ExportProvider exportProvider,
        string language,
        SourceText text,
        string filePath,
        ParseOptions? parseOptions = null)
    {
        var workspace = TestWorkspace.Create(
            new XElement("Workspace",
                new XElement("Project", new XAttribute("Language", language))),
            exportProvider,
            workspaceKind: WorkspaceKind.Host);

        var project = workspace.CurrentSolution.Projects.Single();
        if (parseOptions is not null)
        {
            Assert.True(workspace.TryApplyChanges(project.Solution.WithProjectParseOptions(project.Id, parseOptions)));
        }

        var documentId = DocumentId.CreateNewId(project.Id);
        var loader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), filePath));
        await workspace.AddDocumentAsync(DocumentInfo.Create(
            documentId, Path.GetFileName(filePath), loader: loader, filePath: filePath));
        return workspace;
    }

    private static SyntaxTree GetOrCreateCSharpTree(
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
