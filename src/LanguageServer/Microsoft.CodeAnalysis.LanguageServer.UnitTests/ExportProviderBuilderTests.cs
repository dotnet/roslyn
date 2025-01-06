// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    public sealed class ExportProviderBuilderTests(ITestOutputHelper testOutputHelper)
        : AbstractLanguageServerHostTests(testOutputHelper)
    {
        [Fact]
        public async Task MefCompositionIsCached()
        {
            await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

            await AssertCacheWriteWasAttemptedAsync();

            AssertCachedCompositionCountEquals(expectedCount: 1);
        }

        [Fact]
        public async Task MefCompositionIsReused()
        {
            await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

            await AssertCacheWriteWasAttemptedAsync();

            // Second test server with the same set of assemblies.
            await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: false);

            AssertNoCacheWriteWasAttempted();

            AssertCachedCompositionCountEquals(expectedCount: 1);
        }

        [Fact]
        public async Task MultipleMefCompositionsAreCached()
        {
            await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

            await AssertCacheWriteWasAttemptedAsync();

            // Second test server with a different set of assemblies.
            await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: true);

            await AssertCacheWriteWasAttemptedAsync();

            AssertCachedCompositionCountEquals(expectedCount: 2);
        }

        private async Task AssertCacheWriteWasAttemptedAsync()
        {
            var cacheWriteTask = ExportProviderBuilder.TestAccessor.GetCacheWriteTask();
            Assert.NotNull(cacheWriteTask);

            await cacheWriteTask;
        }

        private void AssertNoCacheWriteWasAttempted()
        {
            var cacheWriteTask2 = ExportProviderBuilder.TestAccessor.GetCacheWriteTask();
            Assert.Null(cacheWriteTask2);
        }

        private void AssertCachedCompositionCountEquals(int expectedCount)
        {
            var mefCompositions = Directory.EnumerateFiles(MefCacheDirectory.Path, "*.mef-composition", SearchOption.AllDirectories);

            Assert.Equal(expectedCount, mefCompositions.Count());
        }
    }
}
