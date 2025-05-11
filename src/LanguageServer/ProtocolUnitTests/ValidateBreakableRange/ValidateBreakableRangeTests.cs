// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.ValidateBreakableRange;

public sealed class ValidateBreakableRange(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task SimpleStatement(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|expected:{|caret:|}M();|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, caret);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task LineBreakpoint(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
            #if FALSE
                    {|caret:|}M();
            #endif
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        var expected = caret.Range;

        var result = await RunAsync(testLspServer, caret);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task NoBreakpointSpan(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|caret:|}const int a = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        var result = await RunAsync(testLspServer, caret);
        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task SplitBreakpoint(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|breakpoint:int a = 1; {|expected:Console.WriteLine("hello");|}|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1501785")]
    public async Task InvalidExistingBreakpoint1(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|breakpoint:int a Console.WriteLine("hello");|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = breakpoint.Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1501882")]
    public async Task InvalidExistingBreakpoint2(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    int b=
                 {|breakpoint:int a = 1;|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = breakpoint.Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task TypingInMultilineBreakpoint(bool mutatingLspWorkspace)
    {
        // This simulates the request we get just after the user types the semi-colon on the first line
        var markup =
            """
            class A
            {
                void M()
                {
                    {|breakpoint:int a = 1;
                    {|expected:Console.WriteLine("hello");|}|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task TypingInMultilineBreakpoint2(bool mutatingLspWorkspace)
    {
        // This simulates the request we get just after the user types the semi-colon on the third line
        var markup =
            """
            class A
            {
                void M()
                {
                    {|breakpoint:int a 
                            = 
                            1;
                    {|expected:Console.WriteLine(
                            "hello"
                        );|}|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task ExpandToMultilineBreakpoint(bool mutatingLspWorkspace)
    {
        // This simulates the request we get just after the user types the equals sign on the first line
        var markup =
            """
            class A
            {
                void M()
                {
                    {|expected:int a =
                    {|breakpoint:GetSomeValue();|}|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    [Theory, CombinatorialData]
    public async Task DoNotShrinkValidMultilineBreakpoints(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|breakpoint:{|expected:int a =
                    GetSomeValue();|}|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var breakpoint = testLspServer.GetLocations("breakpoint").Single();

        var expected = testLspServer.GetLocations("expected").Single().Range;

        var result = await RunAsync(testLspServer, breakpoint);
        AssertJsonEquals(expected, result);
    }

    private static async Task<LSP.Range?> RunAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.VSInternalValidateBreakableRangeParams, LSP.Range?>(
            LSP.VSInternalMethods.TextDocumentValidateBreakableRangeName,
            new LSP.VSInternalValidateBreakableRangeParams()
            {
                TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = caret.DocumentUri },
                Range = caret.Range
            },
            CancellationToken.None);
    }
}
