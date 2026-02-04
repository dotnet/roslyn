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

public sealed class VisualBasicCallHierarchyTests : AbstractCallHierarchyTests
{
    public VisualBasicCallHierarchyTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchyAsync(bool mutatingLspWorkspace)
    {
        var markup = """
            Class C
                Sub {|caret:|}M()
                End Sub
            End Class
            """;

        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
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
            Class C
                Sub {|caret:|}M()
                End Sub

                Sub Caller()
                    M()
                End Sub
            End Class
            """;

        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
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
            Class C
                Sub {|caret:|}M()
                    N()
                End Sub

                Sub N()
                End Sub
            End Class
            """;

        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
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

    private async Task<TestLspServer> CreateVisualBasicTestLspServerAsync(string markup, bool mutatingLspWorkspace)
    {
        return await CreateVisualBasicLspServerAsync(markup, mutatingLspWorkspace, new LSP.VSInternalClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                CallHierarchy = new LSP.DynamicRegistrationSetting { DynamicRegistration = true }
            }
        });
    }
}
