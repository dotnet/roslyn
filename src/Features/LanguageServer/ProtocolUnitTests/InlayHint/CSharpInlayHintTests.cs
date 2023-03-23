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

        [Fact]
        public async Task TestOneInlayParameterHintAsync()
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
            await RunVerifyInlayHintAsync(markup);
        }

        [Fact]
        public async Task TestMultipleInlayParameterHintsAsync()
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
            await RunVerifyInlayHintAsync(markup);
        }

        [Fact]
        public async Task TestOneInlayTypeHintAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        var {|int:|}x = 5;
    }
}";
            await RunVerifyInlayHintAsync(markup);
        }

        [Fact]
        public async Task TestMultipleInlayTypeHintsAsync()
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
            await RunVerifyInlayHintAsync(markup);
        }

        [Fact]
        public async Task TestInlayTypeHintsDeconstructAsync()
        {
            var markup =
@"class A
{
    void X((int, bool) d)
    {
        var (i, b) = d;
    }
}";
            await RunVerifyInlayHintAsync(markup, hasTextEdits: false);
        }

        private async Task RunVerifyInlayHintAsync(string markup, bool hasTextEdits = true)
        {
            await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
            await VerifyInlayHintAsync(testLspServer, hasTextEdits);
        }
    }
}
