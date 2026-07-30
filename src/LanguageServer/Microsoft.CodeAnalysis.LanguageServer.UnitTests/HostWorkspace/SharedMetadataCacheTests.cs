// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SharedMetadataCacheTests : TestBase
{
    private static readonly TestMetadataProvider s_metadataProvider = new();

    [Fact]
    public async Task ConcurrentRequests_ReturnSameMetadata()
    {
        var cache = new SharedMetadataCache();
        var mscorlibPath = typeof(object).Assembly.Location;

        var metadata = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(
                () => GetMetadata(cache, mscorlibPath, MetadataImageKind.Assembly))));

        Assert.All(metadata, item => Assert.Same(metadata[0], item));
        Assert.NotEmpty(((AssemblyMetadata)metadata[0]).GetModules());
    }

    [Fact]
    public void CacheHit_DoesNotInvokeProvider()
    {
        var cache = new SharedMetadataCache();
        var mscorlibPath = typeof(object).Assembly.Location;
        var providerCallCount = 0;

        var metadata1 = cache.GetMetadata(mscorlibPath, MetadataImageKind.Assembly, GetMetadataFromProvider).Metadata;
        var metadata2 = cache.GetMetadata(mscorlibPath, MetadataImageKind.Assembly, GetMetadataFromProvider).Metadata;

        Assert.Same(metadata1, metadata2);
        Assert.Equal(1, providerCallCount);

        MetadataProviderResult GetMetadataFromProvider(string path, MetadataImageKind kind)
        {
            providerCallCount++;
            return s_metadataProvider.GetMetadata(path, kind);
        }
    }

    [Fact]
    public void ChangedTimestamp_DoesNotShareMetadata()
    {
        var cache = new SharedMetadataCache();
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var metadata1 = GetMetadata(cache, path, MetadataImageKind.Assembly);

        File.Copy(typeof(Enumerable).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        var metadata2 = GetMetadata(cache, path, MetadataImageKind.Assembly);

        Assert.NotSame(metadata1.Id, metadata2.Id);
    }

    [Fact]
    public void CacheDoesNotKeepMetadataAlive()
    {
        var cache = new SharedMetadataCache();
        var metadataReference = ObjectReference.CreateFromFactory(
            () => GetMetadata(cache, typeof(object).Assembly.Location, MetadataImageKind.Assembly));

        metadataReference.AssertReleased();
    }

    [Fact]
    public void DeadMetadataIsReloaded()
    {
        var cache = new SharedMetadataCache();
        var path = typeof(object).Assembly.Location;
        var metadataReference = ObjectReference.CreateFromFactory(
            () => GetMetadata(cache, path, MetadataImageKind.Assembly));
        metadataReference.AssertReleased();

        var reloadedMetadata = GetMetadata(cache, path, MetadataImageKind.Assembly);
        Assert.Same(reloadedMetadata, GetMetadata(cache, path, MetadataImageKind.Assembly));

        GC.KeepAlive(reloadedMetadata);
    }

    [Fact]
    public void CleanupRemovesDeadEntries()
    {
        var cache = new SharedMetadataCache(cleanupThreshold: 2);
        var metadataReference = ObjectReference.CreateFromFactory(
            () => GetMetadata(cache, typeof(object).Assembly.Location, MetadataImageKind.Assembly));
        metadataReference.AssertReleased();

        var liveMetadata = GetMetadata(cache, typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly);

        Assert.Equal(1, cache.GetTestAccessor().EntryCount);
        Assert.NotEmpty(((AssemblyMetadata)liveMetadata).GetModules());
        GC.KeepAlive(liveMetadata);
    }

    [Fact]
    public void ChangedFileReplacesPreviousVersion()
    {
        var cache = new SharedMetadataCache();
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var firstMetadata = GetMetadata(cache, path, MetadataImageKind.Assembly);
        var otherMetadata = GetMetadata(cache, typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly);
        Assert.Same(firstMetadata, GetMetadata(cache, path, MetadataImageKind.Assembly));

        File.Copy(typeof(Uri).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        Assert.NotSame(firstMetadata, GetMetadata(cache, path, MetadataImageKind.Assembly));
        Assert.Same(otherMetadata, GetMetadata(cache, typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly));
    }

    [Fact]
    public void MultiModuleAssemblyIsNotCached()
    {
        var cache = new SharedMetadataCache();
        var directory = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        var modulePath = Path.Combine(directory, "mod.netmodule");
        var assemblyPath = Path.Combine(directory, "MultiModule.dll");

        var coreLibrary = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var moduleCompilation = CSharpCompilation.Create(
            "mod",
            [CSharpSyntaxTree.ParseText("public class ModuleType { }")],
            [coreLibrary],
            new CSharpCompilationOptions(OutputKind.NetModule));
        Assert.True(moduleCompilation.Emit(modulePath).Success);

        var assemblyCompilation = CSharpCompilation.Create(
            "MultiModule",
            [CSharpSyntaxTree.ParseText("public class AssemblyType { }")],
            [coreLibrary, MetadataReference.CreateFromFile(modulePath, MetadataReferenceProperties.Module)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.True(assemblyCompilation.Emit(assemblyPath).Success);

        var metadata1 = (AssemblyMetadata)GetMetadata(cache, assemblyPath, MetadataImageKind.Assembly);
        var metadata2 = (AssemblyMetadata)GetMetadata(cache, assemblyPath, MetadataImageKind.Assembly);

        Assert.NotSame(metadata1.Id, metadata2.Id);
        Assert.Equal(2, metadata1.GetModules().Length);
        Assert.Equal(2, metadata2.GetModules().Length);
    }

    [Fact]
    public void NonExistentFile_DoesNotPoisonCache()
    {
        var cache = new SharedMetadataCache();
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");

        Assert.Throws<FileNotFoundException>(
            () => GetMetadata(cache, path, MetadataImageKind.Assembly));

        File.Copy(typeof(object).Assembly.Location, path);

        Assert.NotNull(GetMetadata(cache, path, MetadataImageKind.Assembly));
    }

    private static Metadata GetMetadata(SharedMetadataCache cache, string path, MetadataImageKind kind)
        => cache.GetMetadata(path, kind, s_metadataProvider.GetMetadata).Metadata;

    private sealed class TestMetadataProvider : AbstractMetadataProviderService;
}
