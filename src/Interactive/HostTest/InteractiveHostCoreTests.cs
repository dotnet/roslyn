// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

extern alias InteractiveHost;

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostCoreTests : AbstractInteractiveHostTests
    {
        internal override InteractiveHostPlatform DefaultPlatform => InteractiveHostPlatform.Core;
        internal override string[] ReferenceSearchPaths => Array.Empty<string>();
        internal override bool UseDefaultInitializationFile => true;

        [Fact]
        public async Task DefaultReferencesAndImports()
        {
            await Execute(@"
dynamic d = (""home"", Directory.GetCurrentDirectory(), await Task.FromResult(1));
WriteLine(d.ToString());
");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences($"(home, {HomeDir}, 1)", output);
        }
    }
}
