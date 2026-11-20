// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class AnalyzerAssetScopeTests
{
    [Fact]
    public async Task HostGeneratorPresenceDoesNotPrepareUnrelatedProjectAssets()
    {
        using var workspace = TestWorkspace.CreateCSharp(
            "class C {}",
            composition: FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess));
        var metadata = new CountingMetadataReference();
        var analyzer = new AnalyzerFileReference(typeof(CSharpCompilation).Assembly.Location, new UnusedAnalyzerLoader());
        var project = workspace.CurrentSolution.Projects.Single()
            .AddMetadataReference(metadata)
            .AddAnalyzerReference(analyzer);

        var presence = await project.Solution.CompilationState.GetProjectGeneratorPresenceAsync(project.Id, CancellationToken.None);

        Assert.Equal(SourceGeneratorPresence.NoSourceGenerators, presence);
        Assert.Equal(0, metadata.AccessCount);
        Assert.False(project.State.TryGetStateChecksums(out _));
    }

    [Fact]
    public async Task AnalyzerScopeDoesNotPrepareProjectAssets()
    {
        using var workspace = new AdhocWorkspace();
        var metadata = new CountingMetadataReference();
        var analyzer = new AnalyzerFileReference(typeof(AnalyzerAssetScopeTests).Assembly.Location, new UnusedAnalyzerLoader());
        var project = workspace.AddProject("Project", LanguageNames.CSharp)
            .AddMetadataReference(metadata)
            .AddAnalyzerReference(analyzer);
        var storage = new SolutionAssetStorage();

        using var scope = storage.StoreAnalyzerReferences(project.Solution.CompilationState, project.Id, CancellationToken.None);
        var checksums = project.State.GetAnalyzerReferenceChecksums(CancellationToken.None);
        var asset = await scope.GetTestAccessor().GetAssetAsync(checksums.Children[0], CancellationToken.None);

        Assert.Same(analyzer, asset);
        Assert.Equal(0, metadata.AccessCount);
        Assert.False(project.State.TryGetStateChecksums(out _));
    }

    [Fact]
    public async Task ConcurrentScopesRetainAnalyzerAssetsUntilLastRelease()
    {
        using var workspace = new AdhocWorkspace();
        var analyzer = new AnalyzerFileReference(typeof(AnalyzerAssetScopeTests).Assembly.Location, new UnusedAnalyzerLoader());
        var project = workspace.AddProject("Project", LanguageNames.CSharp).AddAnalyzerReference(analyzer);
        var storage = new SolutionAssetStorage();
        using var first = storage.StoreAnalyzerReferences(project.Solution.CompilationState, project.Id, CancellationToken.None);
        using (var second = storage.StoreAnalyzerReferences(project.Solution.CompilationState, project.Id, CancellationToken.None))
            Assert.Same(first, second);

        var checksum = project.State.GetAnalyzerReferenceChecksums(CancellationToken.None).Children[0];
        Assert.Same(analyzer, await first.GetTestAccessor().GetAssetAsync(checksum, CancellationToken.None));
        Assert.Equal(1, first.RefCount);
    }

    [Fact]
    public void ProjectsWithTheSameAnalyzerAssetsShareTheirScope()
    {
        using var workspace = new AdhocWorkspace();
        var analyzer = new AnalyzerFileReference(typeof(AnalyzerAssetScopeTests).Assembly.Location, new UnusedAnalyzerLoader());
        var firstProject = workspace.AddProject("First", LanguageNames.CSharp).AddAnalyzerReference(analyzer);
        var secondProject = firstProject.Solution.AddProject("Second", "Second", LanguageNames.CSharp).AddAnalyzerReference(analyzer);
        var storage = new SolutionAssetStorage();

        using var first = storage.StoreAnalyzerReferences(firstProject.Solution.CompilationState, firstProject.Id, CancellationToken.None);
        using var second = storage.StoreAnalyzerReferences(secondProject.Solution.CompilationState, secondProject.Id, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(2, first.RefCount);
        Assert.False(firstProject.State.TryGetStateChecksums(out _));
        Assert.False(secondProject.State.TryGetStateChecksums(out _));
    }

    [Fact]
    public async Task ChangedAnalyzerReferencesGetADifferentScope()
    {
        using var workspace = new AdhocWorkspace();
        var analyzer = new AnalyzerFileReference(typeof(AnalyzerAssetScopeTests).Assembly.Location, new UnusedAnalyzerLoader());
        var changedAnalyzer = new AnalyzerFileReference(typeof(CSharpCompilation).Assembly.Location, new UnusedAnalyzerLoader());
        var project = workspace.AddProject("Project", LanguageNames.CSharp).AddAnalyzerReference(analyzer);
        var changedProject = project.AddAnalyzerReference(changedAnalyzer);
        var storage = new SolutionAssetStorage();

        using var original = storage.StoreAnalyzerReferences(project.Solution.CompilationState, project.Id, CancellationToken.None);
        using var changed = storage.StoreAnalyzerReferences(changedProject.Solution.CompilationState, changedProject.Id, CancellationToken.None);

        Assert.NotEqual(original.SolutionChecksum, changed.SolutionChecksum);
        var checksum = changedProject.State.GetAnalyzerReferenceChecksums(CancellationToken.None).Children[1];
        Assert.Same(changedAnalyzer, await changed.GetTestAccessor().GetAssetAsync(checksum, CancellationToken.None));
        Assert.Equal(1, original.RefCount);
    }

    [Fact]
    public void CanceledRequestDoesNotPrepareAssets()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Project", LanguageNames.CSharp);
        var storage = new SolutionAssetStorage();

        Assert.Throws<OperationCanceledException>(() =>
            storage.StoreAnalyzerReferences(project.Solution.CompilationState, project.Id, new CancellationToken(canceled: true)));
        Assert.False(project.State.TryGetStateChecksums(out _));
    }

    private sealed class UnusedAnalyzerLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
            => throw new InvalidOperationException("Pinning analyzer assets must not load analyzer assemblies.");
    }

    private sealed class CountingMetadataReference : PortableExecutableReference
    {
        private readonly Counter _counter;

        public CountingMetadataReference()
            : this(new Counter(), MetadataReferenceProperties.Assembly)
        {
        }

        private CountingMetadataReference(Counter counter, MetadataReferenceProperties properties)
            : base(properties)
        {
            _counter = counter;
        }

        public int AccessCount => _counter.Value;

        protected override DocumentationProvider CreateDocumentationProvider()
            => DocumentationProvider.Default;

        protected override Metadata GetMetadataImpl()
        {
            _counter.Value++;
            throw new InvalidOperationException("Analyzer-only requests must not access PE references.");
        }

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => new CountingMetadataReference(_counter, properties);

        private sealed class Counter
        {
            public int Value;
        }
    }
}
