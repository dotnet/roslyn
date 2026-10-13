// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests;

public sealed class RemoteExportProviderBuilderTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/85135")]
    public async Task MefCompositionBuildsAndIsCached()
    {
        using var tempRoot = new TempRoot();
        var localSettingsDirectory = tempRoot.CreateDirectory().Path;
        var missingAssemblyNames = RemoteExportProviderBuilder.AdditionalRemoteHostAssemblyNames
            .Where(name => MefHostServicesHelpers.TryFindNearbyAssemblyLocation(name) is null);
        Assert.Empty(missingAssemblyNames);

        var traceSource = new TraceSource(nameof(RemoteExportProviderBuilderTests));
        try
        {
            var errors = await RemoteExportProviderBuilder.InitializeAsync(localSettingsDirectory, traceSource, CancellationToken.None);
            Assert.Null(errors);

            var cacheWriteTask = RemoteExportProviderBuilder.TestAccessor.GetCacheWriteTask();
            Assert.NotNull(cacheWriteTask);
            await cacheWriteTask;

            Assert.Single(Directory.EnumerateFiles(localSettingsDirectory, "*.mef-composition", SearchOption.AllDirectories));
        }
        finally
        {
            traceSource.Close();
        }
    }
}
