// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SharedMetadataCacheTests : TestBase
{
    [Fact]
    public async Task ConcurrentRequests_ReturnSameMetadata()
    {
        var cache = new SharedMetadataCache();
        var mscorlibPath = typeof(object).Assembly.Location;

        var metadata = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(
                () => cache.GetMetadata(mscorlibPath, MetadataImageKind.Assembly))));

        Assert.All(metadata, item => Assert.Same(metadata[0], item));
    }

    [Fact]
    public void ChangedTimestamp_DoesNotShareMetadata()
    {
        var cache = new SharedMetadataCache();
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var metadata1 = cache.GetMetadata(path, MetadataImageKind.Assembly);

        File.Copy(typeof(Enumerable).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        var metadata2 = cache.GetMetadata(path, MetadataImageKind.Assembly);

        Assert.NotSame(metadata1.Id, metadata2.Id);
    }

    [Fact]
    public void EvictionDoesNotDisposeActiveMetadata()
    {
        var cache = new SharedMetadataCache(capacity: 1);
        var firstMetadata = (AssemblyMetadata)cache.GetMetadata(
            typeof(object).Assembly.Location, MetadataImageKind.Assembly);

        _ = cache.GetMetadata(typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly);
        var reloadedMetadata = cache.GetMetadata(
            typeof(object).Assembly.Location, MetadataImageKind.Assembly);

        Assert.NotSame(firstMetadata, reloadedMetadata);
        Assert.NotEmpty(firstMetadata.GetModules());
    }

    [Fact]
    public void ChangedFileReplacesPreviousVersion()
    {
        var cache = new SharedMetadataCache(capacity: 2);
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var firstMetadata = cache.GetMetadata(path, MetadataImageKind.Assembly);
        var otherMetadata = cache.GetMetadata(typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly);
        Assert.Same(firstMetadata, cache.GetMetadata(path, MetadataImageKind.Assembly));

        File.Copy(typeof(Uri).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        Assert.NotSame(firstMetadata, cache.GetMetadata(path, MetadataImageKind.Assembly));
        Assert.Same(otherMetadata, cache.GetMetadata(typeof(Enumerable).Assembly.Location, MetadataImageKind.Assembly));
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

        var metadata1 = (AssemblyMetadata)cache.GetMetadata(assemblyPath, MetadataImageKind.Assembly);
        var metadata2 = (AssemblyMetadata)cache.GetMetadata(assemblyPath, MetadataImageKind.Assembly);

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
            () => cache.GetMetadata(path, MetadataImageKind.Assembly));

        File.Copy(typeof(object).Assembly.Location, path);

        Assert.NotNull(cache.GetMetadata(path, MetadataImageKind.Assembly));
    }
}
