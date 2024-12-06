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

            var mefCompositions = Directory.EnumerateFiles(MefCacheDirectory.Path, "*.mef-composition", SearchOption.AllDirectories);

            Assert.Single(mefCompositions);
        }

        [Fact]
        public async Task MefCompositionIsReused()
        {
            await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

            // Second test server with the same set of assemblies.
            await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: false);

            var mefCompositions = Directory.EnumerateFiles(MefCacheDirectory.Path, "*.mef-composition", SearchOption.AllDirectories);

            Assert.Single(mefCompositions);
        }

        [Fact]
        public async Task MultipleMefCompositionsAreCached()
        {
            await using var testServer = await CreateLanguageServerAsync(includeDevKitComponents: false);

            // Second test server with a different set of assemblies.
            await using var testServer2 = await CreateLanguageServerAsync(includeDevKitComponents: true);

            var mefCompositions = Directory.EnumerateFiles(MefCacheDirectory.Path, "*.mef-composition", SearchOption.AllDirectories);

            Assert.Equal(2, mefCompositions.Count());
        }
    }
}
