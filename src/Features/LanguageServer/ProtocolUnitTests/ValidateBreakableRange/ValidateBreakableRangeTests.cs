// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.ValidateBreakableRange
{
    public class ValidateBreakableRange : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task SimpleStatement()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}M();
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caret = testLspServer.GetLocations("caret").Single();

            var expected = new LSP.Range()
            {
                Start = caret.Range.Start,
                End = new LSP.Position(caret.Range.Start.Line, caret.Range.Start.Character + "M();".Length),
            };

            var result = await RunAsync(testLspServer, caret);
            AssertJsonEquals(expected, result);
        }

        [Fact]
        public async Task LineBreakpoint()
        {
            var markup =
@"class A
{
    void M()
    {
#if FALSE
        {|caret:|}M();
#endif
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caret = testLspServer.GetLocations("caret").Single();

            var expected = new LSP.Range()
            {
                Start = caret.Range.Start,
                End = caret.Range.Start,
            };

            var result = await RunAsync(testLspServer, caret);
            AssertJsonEquals(expected, result);
        }

        [Fact]
        public async Task NoBreakpointSpan()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}const int a = 1;
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caret = testLspServer.GetLocations("caret").Single();

            var result = await RunAsync(testLspServer, caret);
            Assert.Null(result);
        }

        private static async Task<LSP.Range?> RunAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.VSInternalValidateBreakableRangeParams, LSP.Range?>(
                LSP.VSInternalMethods.TextDocumentValidateBreakableRangeName,
                new LSP.VSInternalValidateBreakableRangeParams()
                {
                    TextDocument = new LSP.TextDocumentIdentifier { Uri = caret.Uri },
                    Range = caret.Range
                },
                CancellationToken.None);
        }
    }
}
