// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class RemoteMefCompositionTest(ITestOutputHelper testOutputHelper) : ToolingTestBase(testOutputHelper)
{
    [Fact]
    public async Task CompositionIsCached()
    {
        using var tempRoot = new TempRoot();
        var cacheDirectory = tempRoot.CreateDirectory().Path;
        var exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory, DisposalToken);

        Assert.NotNull(RemoteMefComposition.TestAccessor.SaveCacheFileTask);
        await RemoteMefComposition.TestAccessor.SaveCacheFileTask;

        Assert.Single(Directory.GetFiles(cacheDirectory));
    }

    [Fact]
    public async Task CacheFileIsUsed()
    {
        using var tempRoot = new TempRoot();
        var cacheDirectory = tempRoot.CreateDirectory().Path;
        var exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory, DisposalToken);

        Assert.NotNull(RemoteMefComposition.TestAccessor.SaveCacheFileTask);
        await RemoteMefComposition.TestAccessor.SaveCacheFileTask;

        Assert.Single(Directory.GetFiles(cacheDirectory));

        RemoteMefComposition.TestAccessor.ClearSaveCacheFileTask();

        exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory, DisposalToken);

        Assert.Null(RemoteMefComposition.TestAccessor.SaveCacheFileTask);
    }

    [Fact]
    public async Task CorruptCacheFileIsOverwritten()
    {
        using var tempRoot = new TempRoot();
        var cacheDirectory = tempRoot.CreateDirectory().Path;
        var cacheFile = RemoteMefComposition.TestAccessor.GetCacheCompositionFile(cacheDirectory);

        File.WriteAllText(cacheFile, "This is not a valid cache file.");

        var exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory, DisposalToken);

        Assert.NotNull(RemoteMefComposition.TestAccessor.SaveCacheFileTask);
        await RemoteMefComposition.TestAccessor.SaveCacheFileTask;

        Assert.Single(Directory.GetFiles(cacheDirectory));
        Assert.True(new FileInfo(cacheFile).Length > 35);
    }

    [Fact]
    public async Task CleansOldCacheFiles()
    {
        using var tempRoot = new TempRoot();
        var cacheDirectory = tempRoot.CreateDirectory().Path;
        Directory.CreateDirectory(cacheDirectory);

        File.WriteAllText(Path.Combine(cacheDirectory, Path.GetRandomFileName()), "");
        File.WriteAllText(Path.Combine(cacheDirectory, Path.GetRandomFileName()), "");
        File.WriteAllText(Path.Combine(cacheDirectory, Path.GetRandomFileName()), "");
        File.WriteAllText(Path.Combine(cacheDirectory, Path.GetRandomFileName()), "");

        Assert.Equal(4, Directory.GetFiles(cacheDirectory).Length);

        var cacheFile = RemoteMefComposition.TestAccessor.GetCacheCompositionFile(cacheDirectory);

        var exportProvider = await RemoteMefComposition.CreateExportProviderAsync(cacheDirectory, DisposalToken);

        Assert.NotNull(RemoteMefComposition.TestAccessor.SaveCacheFileTask);
        await RemoteMefComposition.TestAccessor.SaveCacheFileTask;

        Assert.Single(Directory.GetFiles(cacheDirectory));
        Assert.True(new FileInfo(cacheFile).Length > 35);
    }
}
