// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class MetadataServiceTests : TestBase
{
    [Fact]
    public void GetReference_ValidAssembly_ReturnsPortableExecutableReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var properties = MetadataReferenceProperties.Assembly.WithAliases(["global", "MyAlias"]).WithEmbedInteropTypes(true);

        var mscorlibPath = typeof(object).Assembly.Location;
        var reference = metadataService.GetReference(mscorlibPath, properties);

        Assert.NotNull(reference);
        Assert.Equal(mscorlibPath, reference.FilePath);
        Assert.Equal(properties, reference.Properties);

        Assert.NotNull(reference.GetMetadata());
    }

    [Fact]
    public void GetReference_SamePathAndProperties_ReturnsCachedReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var mscorlibPath = typeof(object).Assembly.Location;
        var reference1 = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);
        var reference2 = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);

        Assert.Same(reference1, reference2);
    }

    [Fact]
    public void GetReference_DifferentWorkspaces_SharesMetadataButNotReference()
    {
        var hostServices = FeaturesTestCompositions.Features.GetHostServices();
        using var workspace1 = new AdhocWorkspace(hostServices);
        using var workspace2 = new AdhocWorkspace(hostServices);

        var properties1 = MetadataReferenceProperties.Assembly;
        var properties2 = properties1.WithAliases(["global", "MyAlias"]).WithEmbedInteropTypes(true);
        var mscorlibPath = typeof(object).Assembly.Location;

        var reference1 = workspace1.Services.GetRequiredService<IMetadataService>().GetReference(mscorlibPath, properties1);
        var reference2 = workspace2.Services.GetRequiredService<IMetadataService>().GetReference(mscorlibPath, properties2);

        Assert.NotSame(reference1, reference2);
        Assert.Equal(properties1, reference1.Properties);
        Assert.Equal(properties2, reference2.Properties);
        Assert.Same(reference1.GetMetadataId(), reference2.GetMetadataId());
    }

    [Fact]
    public void GetReference_DifferentImageKinds_DoNotShareMetadata()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();
        var mscorlibPath = typeof(object).Assembly.Location;

        var assemblyReference = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly);
        var moduleReference = metadataService.GetReference(mscorlibPath, MetadataReferenceProperties.Module);

        Assert.Equal(MetadataImageKind.Assembly, assemblyReference.Properties.Kind);
        Assert.Equal(MetadataImageKind.Module, moduleReference.Properties.Kind);
        Assert.NotSame(assemblyReference.GetMetadataId(), moduleReference.GetMetadataId());
    }

    [Fact]
    public void GetReference_ChangedTimestamp_DoesNotShareMetadata()
    {
        var hostServices = FeaturesTestCompositions.Features.GetHostServices();
        using var workspace1 = new AdhocWorkspace(hostServices);
        using var workspace2 = new AdhocWorkspace(hostServices);

        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var reference1 = workspace1.Services.GetRequiredService<IMetadataService>()
            .GetReference(path, MetadataReferenceProperties.Assembly);

        File.Copy(typeof(Enumerable).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        var reference2 = workspace2.Services.GetRequiredService<IMetadataService>()
            .GetReference(path, MetadataReferenceProperties.Assembly);

        Assert.NotSame(reference1.GetMetadataId(), reference2.GetMetadataId());
    }

    [Fact]
    public async Task SharedMetadataCache_ConcurrentRequests_ReturnSameMetadata()
    {
        var cache = new SharedMetadataCache();
        var mscorlibPath = typeof(object).Assembly.Location;

        var metadata = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(
                () => cache.GetMetadata(mscorlibPath, MetadataImageKind.Assembly))));

        Assert.All(metadata, item => Assert.Same(metadata[0], item));
    }

    [Fact]
    public void SharedMetadataCache_EvictionDoesNotDisposeActiveMetadata()
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
    public void SharedMetadataCache_ChangedFileReplacesPreviousVersion()
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
    public void SharedMetadataCache_MultiModuleAssemblyIsNotCached()
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
    public void GetReference_NonExistentFile_ReturnsThrowingReference()
    {
        using var workspace = SolutionTestHelpers.CreateWorkspace();
        var metadataService = workspace.Services.GetRequiredService<IMetadataService>();

        var nonExistentPath = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");
        var reference1 = metadataService.GetReference(nonExistentPath, MetadataReferenceProperties.Assembly);
        var reference2 = metadataService.GetReference(nonExistentPath, MetadataReferenceProperties.Assembly);

        // Failure is cached:
        Assert.Same(reference1, reference2);

        // Reference is returned even for non-existent files
        Assert.NotNull(reference1);
        Assert.Equal(nonExistentPath, reference1.FilePath);

        // Accessing metadata should throw the stored IOException
        Assert.Throws<FileNotFoundException>(reference1.GetMetadata);
    }

    [Fact]
    public void GetReference_NonExistentFile_DoesNotPoisonOtherWorkspaces()
    {
        var hostServices = FeaturesTestCompositions.Features.GetHostServices();
        using var workspace1 = new AdhocWorkspace(hostServices);
        using var workspace2 = new AdhocWorkspace(hostServices);
        var path = Path.Combine(TempRoot.Root, Guid.NewGuid().ToString() + ".dll");

        var reference1 = workspace1.Services.GetRequiredService<IMetadataService>()
            .GetReference(path, MetadataReferenceProperties.Assembly);
        Assert.Throws<FileNotFoundException>(reference1.GetMetadata);

        File.Copy(typeof(object).Assembly.Location, path);

        var reference2 = workspace2.Services.GetRequiredService<IMetadataService>()
            .GetReference(path, MetadataReferenceProperties.Assembly);

        Assert.NotNull(reference2.GetMetadata());
        Assert.Same(reference1, workspace1.Services.GetRequiredService<IMetadataService>()
            .GetReference(path, MetadataReferenceProperties.Assembly));
    }
}
