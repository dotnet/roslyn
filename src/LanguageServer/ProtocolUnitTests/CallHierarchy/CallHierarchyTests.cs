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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CallHierarchy;

public sealed class CallHierarchyTests : AbstractLanguageServerProtocolTests
{
    public CallHierarchyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_Method(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var result = await PrepareCallHierarchyAsync(testLspServer, caretLocation);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("M()", result[0].Name);
        Assert.Equal(LSP.SymbolKind.Method, result[0].Kind);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_Property(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                int {|caret:|}P { get; set; }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var result = await PrepareCallHierarchyAsync(testLspServer, caretLocation);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("P", result[0].Name);
        Assert.Equal(LSP.SymbolKind.Property, result[0].Kind);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_Field(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                int {|caret:|}field;
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var result = await PrepareCallHierarchyAsync(testLspServer, caretLocation);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("field", result[0].Name);
        Assert.Equal(LSP.SymbolKind.Field, result[0].Kind);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_NoValidSymbol(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                {|caret:|}
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var result = await PrepareCallHierarchyAsync(testLspServer, caretLocation);

        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCalls_Simple(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                }

                void Caller()
                {
                    M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);
        Assert.Single(prepareResult);

        var incomingCalls = await GetIncomingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(incomingCalls);
        Assert.Single(incomingCalls);
        Assert.Equal("Caller()", incomingCalls[0].From.Name);
        Assert.Single(incomingCalls[0].FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCalls_MultipleCalls(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                }

                void Caller1()
                {
                    M();
                }

                void Caller2()
                {
                    M();
                    M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var incomingCalls = await GetIncomingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(incomingCalls);
        Assert.Equal(2, incomingCalls.Length);

        var caller1 = incomingCalls.First(c => c.From.Name == "Caller1()");
        var caller2 = incomingCalls.First(c => c.From.Name == "Caller2()");

        Assert.Single(caller1.FromRanges);
        Assert.Equal(2, caller2.FromRanges.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCalls_NoCalls(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var incomingCalls = await GetIncomingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(incomingCalls);
        Assert.Empty(incomingCalls);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCalls_Simple(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                    N();
                }

                void N()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var outgoingCalls = await GetOutgoingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(outgoingCalls);
        Assert.Single(outgoingCalls);
        Assert.Equal("N()", outgoingCalls[0].To.Name);
        Assert.Single(outgoingCalls[0].FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCalls_MultipleCalls(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                    N();
                    O();
                    O();
                }

                void N()
                {
                }

                void O()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var outgoingCalls = await GetOutgoingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(outgoingCalls);
        Assert.Equal(2, outgoingCalls.Length);

        var callToN = outgoingCalls.First(c => c.To.Name == "N()");
        var callToO = outgoingCalls.First(c => c.To.Name == "O()");

        Assert.Single(callToN.FromRanges);
        Assert.Equal(2, callToO.FromRanges.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCalls_NoCalls(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var outgoingCalls = await GetOutgoingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(outgoingCalls);
        Assert.Empty(outgoingCalls);
    }

    [Theory, CombinatorialData]
    public async Task TestCallHierarchy_PropertyAccess(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                int {|caret:|}P { get; set; }

                void M()
                {
                    var x = P;
                    P = 42;
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var prepareResult = await PrepareCallHierarchyAsync(testLspServer, caretLocation);
        Assert.NotNull(prepareResult);

        var incomingCalls = await GetIncomingCallsAsync(testLspServer, prepareResult[0]);
        Assert.NotNull(incomingCalls);
        Assert.Single(incomingCalls);
        Assert.Equal("M()", incomingCalls[0].From.Name);
        Assert.Equal(2, incomingCalls[0].FromRanges.Length);
    }

    private static async Task<LSP.CallHierarchyItem[]?> PrepareCallHierarchyAsync(
        TestLspServer testLspServer,
        LSP.Location caretLocation)
    {
        var request = new LSP.CallHierarchyPrepareParams
        {
            TextDocument = CreateTextDocumentIdentifier(caretLocation.DocumentUri),
            Position = caretLocation.Range.Start
        };

        return await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            request,
            CancellationToken.None);
    }

    private static async Task<LSP.CallHierarchyIncomingCall[]?> GetIncomingCallsAsync(
        TestLspServer testLspServer,
        LSP.CallHierarchyItem item)
    {
        var request = new LSP.CallHierarchyIncomingCallsParams
        {
            Item = item
        };

        return await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>(
            LSP.Methods.CallHierarchyIncomingCallsName,
            request,
            CancellationToken.None);
    }

    private static async Task<LSP.CallHierarchyOutgoingCall[]?> GetOutgoingCallsAsync(
        TestLspServer testLspServer,
        LSP.CallHierarchyItem item)
    {
        var request = new LSP.CallHierarchyOutgoingCallsParams
        {
            Item = item
        };

        return await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>(
            LSP.Methods.CallHierarchyOutgoingCallsName,
            request,
            CancellationToken.None);
    }
}
