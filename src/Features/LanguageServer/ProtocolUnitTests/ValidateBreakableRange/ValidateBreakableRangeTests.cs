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
        {|expected:{|caret:|}M();|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var caret = testLspServer.GetLocations("caret").Single();

            var expected = testLspServer.GetLocations("expected").Single().Range;

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

            var expected = caret.Range;

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

        [Fact]
        public async Task SplitBreakpoint()
        {
            var markup =
@"class A
{
    void M()
    {
        {|breakpoint:{|expected:int a = 1;|} Console.WriteLine(""hello"");|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var breakpoint = testLspServer.GetLocations("breakpoint").Single();

            var expected = testLspServer.GetLocations("expected").Single().Range;

            var result = await RunAsync(testLspServer, breakpoint);
            AssertJsonEquals(expected, result);
        }

        [Fact]
        [WorkItem(1501785, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1501785")]
        public async Task InvalidExistingBreakpoint1()
        {
            var markup =
@"class A
{
    void M()
    {
        {|breakpoint:int a Console.WriteLine(""hello"");|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var breakpoint = testLspServer.GetLocations("breakpoint").Single();

            var expected = breakpoint.Range;

            var result = await RunAsync(testLspServer, breakpoint);
            AssertJsonEquals(expected, result);
        }

        [Fact]
        [WorkItem(1501882, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1501882")]
        public async Task InvalidExistingBreakpoint2()
        {
            var markup =
@"class A
{
    void M()
    {
        int b=
     {|breakpoint:int a = 1;|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var breakpoint = testLspServer.GetLocations("breakpoint").Single();

            var expected = breakpoint.Range;

            var result = await RunAsync(testLspServer, breakpoint);
            AssertJsonEquals(expected, result);
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
