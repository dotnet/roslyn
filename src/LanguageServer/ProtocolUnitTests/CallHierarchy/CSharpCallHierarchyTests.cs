// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CallHierarchy;

public sealed class CSharpCallHierarchyTests : AbstractCallHierarchyTests
{
    public CSharpCallHierarchyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchyAsync(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateCSharpTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var items = await PrepareCallHierarchyAsync(testLspServer, caretLocation.Range.Start);

        AssertEx.NotNull(items);
        Assert.Single(items);

        var item = items[0];
        Assert.Equal("M()", item.Name);
        Assert.Equal(LSP.SymbolKind.Method, item.Kind);
        AssertEx.NotNull(item.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCallsAsync(bool mutatingLspWorkspace)
    {
        var markup = """
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

        await using var testLspServer = await CreateCSharpTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var items = await PrepareCallHierarchyAsync(testLspServer, caretLocation.Range.Start);

        AssertEx.NotNull(items);
        var incomingCalls = await GetIncomingCallsAsync(testLspServer, items[0]);

        AssertEx.NotNull(incomingCalls);
        Assert.Single(incomingCalls);

        var incomingCall = incomingCalls[0];
        Assert.Equal("Caller()", incomingCall.From.Name);
        Assert.Single(incomingCall.FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCallsAsync(bool mutatingLspWorkspace)
    {
        var markup = """
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

        await using var testLspServer = await CreateCSharpTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var items = await PrepareCallHierarchyAsync(testLspServer, caretLocation.Range.Start);

        AssertEx.NotNull(items);
        var outgoingCalls = await GetOutgoingCallsAsync(testLspServer, items[0]);

        AssertEx.NotNull(outgoingCalls);
        Assert.Single(outgoingCalls);

        var outgoingCall = outgoingCalls[0];
        Assert.Equal("N()", outgoingCall.To.Name);
        Assert.Single(outgoingCall.FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleIncomingCallsAsync(bool mutatingLspWorkspace)
    {
        var markup = """
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
                }
            }
            """;

        await using var testLspServer = await CreateCSharpTestLspServerAsync(markup, mutatingLspWorkspace);
        var caretLocation = testLspServer.GetLocations("caret").Single();
        var items = await PrepareCallHierarchyAsync(testLspServer, caretLocation.Range.Start);

        AssertEx.NotNull(items);
        var incomingCalls = await GetIncomingCallsAsync(testLspServer, items[0]);

        AssertEx.NotNull(incomingCalls);
        Assert.Equal(2, incomingCalls.Length);

        var callerNames = incomingCalls.Select(c => c.From.Name).OrderBy(n => n).ToArray();
        Assert.Equal(["Caller1()", "Caller2()"], callerNames);
    }

    private async Task<TestLspServer> CreateCSharpTestLspServerAsync(string markup, bool mutatingLspWorkspace)
    {
        return await CreateCSharpLspServerAsync(markup, mutatingLspWorkspace, new LSP.VSInternalClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                CallHierarchy = new LSP.DynamicRegistrationSetting { DynamicRegistration = true }
            }
        });
    }
}
