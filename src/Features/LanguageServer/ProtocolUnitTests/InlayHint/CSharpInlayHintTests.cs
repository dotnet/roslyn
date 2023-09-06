// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.InlayHint
{
    public class CSharpInlayHintTests : AbstractInlayHintTests
    {
        public CSharpInlayHintTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestOneInlayParameterHintAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M(int x)
    {
    }

    void M2()
    {
        M({|x:|}5);
    }
}";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        [Theory, CombinatorialData]
        public async Task TestMultipleInlayParameterHintsAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M(int a, double b, bool c)
    {
    }

    void M2()
    {
        M({|a:|}5, {|b:|}5.5, {|c:|}true);
    }
}";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        [Theory, CombinatorialData]
        public async Task TestOneInlayTypeHintAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
        var {|int:|}x = 5;
    }
}";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        [Theory, CombinatorialData]
        public async Task TestMultipleInlayTypeHintsAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"using System;
class A
{
    void M()
    {
        var {|int:|}x = 5;
        var {|object:|}obj = new Object();
    }
}";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace);
        }

        [Theory, CombinatorialData]
        public async Task TestInlayTypeHintsDeconstructAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void X((int, bool) d)
    {
        var (i, b) = d;
    }
}";
            await RunVerifyInlayHintAsync(markup, mutatingLspWorkspace, hasTextEdits: false);
        }

        private async Task RunVerifyInlayHintAsync(string markup, bool mutatingLspWorkspace, bool hasTextEdits = true)
        {
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
            await VerifyInlayHintAsync(testLspServer, hasTextEdits);
        }
    }
}
