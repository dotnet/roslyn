// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

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

        [Theory, CombinatorialData]
        public async Task TestDoesNotShutdownServerIfCacheEntryMissing(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
        var {|int:|}x = 5;
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
            var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
            var textDocument = CreateTextDocumentIdentifier(document.GetURI());
            var sourceText = await document.GetTextAsync();
            var span = TextSpan.FromBounds(0, sourceText.Length);

            var inlayHintParams = new LSP.InlayHintParams
            {
                TextDocument = textDocument,
                Range = ProtocolConversions.TextSpanToRange(span, sourceText)
            };

            var actualInlayHints = await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
            var firstInlayHint = actualInlayHints.First();
            var data = JsonConvert.DeserializeObject<InlayHintResolveData>(firstInlayHint.Data!.ToString());
            AssertEx.NotNull(data);
            var firstResultId = data.ResultId;

            // Verify the inlay hint item is in the cache.
            var cache = testLspServer.GetRequiredLspService<InlayHintCache>();
            Assert.NotNull(cache.GetCachedEntry(firstResultId));

            // Execute a few more requests to ensure the first request is removed from the cache.
            await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
            await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
            var lastInlayHints = await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
            Assert.True(lastInlayHints.Any());

            // Assert that the first result id is no longer in the cache.
            Assert.Null(cache.GetCachedEntry(firstResultId));

            // Assert that the request throws because the item no longer exists in the cache.
            await Assert.ThrowsAsync<RemoteInvocationException>(async () => await testLspServer.ExecuteRequestAsync<LSP.InlayHint, LSP.InlayHint>(LSP.Methods.InlayHintResolveName, firstInlayHint, CancellationToken.None));

            // Assert that the server did not shutdown and that we can resolve the latest inlay hint request we made.
            var lastInlayHint = await testLspServer.ExecuteRequestAsync<LSP.InlayHint, LSP.InlayHint>(LSP.Methods.InlayHintResolveName, lastInlayHints.First(), CancellationToken.None);
            Assert.NotNull(lastInlayHint?.ToolTip);
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
