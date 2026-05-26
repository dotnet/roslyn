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

public sealed class CallHierarchyTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchyIncludesContainingTypeInName(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void {|definition:M|}()
                {
                }

                void N()
                {
                    {|caret:|}M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItems = await RunPrepareCallHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        var preparedItem = Assert.Single(preparedItems);
        var definition = testLspServer.GetLocations("definition").Single();

        Assert.Equal("C.M()", preparedItem.Name);
        Assert.Equal(definition.DocumentUri, preparedItem.Uri);
        Assert.Equal(0, CompareRange(definition.Range, preparedItem.SelectionRange));
        Assert.NotNull(preparedItem.Data);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCallsIncludeImplicitConstructors(bool mutatingLspWorkspace)
    {
        var markup = """
            class {|type:C|}
            {
                public void {|targetMethod:M|}()
                {
                }
            }

            class Caller
            {
                void {|caret:N|}()
                {
                    var c = new C();
                    c.M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareCallHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var outgoingCalls = await RunOutgoingCallsAsync(testLspServer, preparedItem);

        Assert.Equal(2, outgoingCalls.Length);

        var constructorCall = Assert.Single(outgoingCalls, static call => call.To.Name == "C.C()");
        var methodCall = Assert.Single(outgoingCalls, static call => call.To.Name == "C.M()");

        var typeLocation = testLspServer.GetLocations("type").Single();
        var methodLocation = testLspServer.GetLocations("targetMethod").Single();

        Assert.Equal(typeLocation.DocumentUri, constructorCall.To.Uri);
        Assert.Equal(0, CompareRange(typeLocation.Range, constructorCall.To.SelectionRange));
        Assert.Single(constructorCall.FromRanges);

        Assert.Equal(methodLocation.DocumentUri, methodCall.To.Uri);
        Assert.Equal(0, CompareRange(methodLocation.Range, methodCall.To.SelectionRange));
        Assert.Single(methodCall.FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCallsIncludesManyCallers(bool mutatingLspWorkspace)
    {
        var markup = """
            class Target
            {
                void {|definition:M|}()
                {
                }
            }

            class Caller
            {
                void {|caller1:Caller1|}()
                {
                    var target = new Target();
                    target.{|caret:|}M();
                }

                void {|caller2:Caller2|}()
                {
                    var target = new Target();
                    target.M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareCallHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var incomingCalls = await RunIncomingCallsAsync(testLspServer, preparedItem);

        Assert.Equal(2, incomingCalls.Length);

        var firstCaller = Assert.Single(incomingCalls, static call => call.From.Name == "Caller.Caller1()");
        var secondCaller = Assert.Single(incomingCalls, static call => call.From.Name == "Caller.Caller2()");

        var firstCallerLocation = testLspServer.GetLocations("caller1").Single();
        var secondCallerLocation = testLspServer.GetLocations("caller2").Single();

        Assert.Equal(0, CompareRange(firstCallerLocation.Range, firstCaller.From.SelectionRange));
        Assert.Equal(0, CompareRange(secondCallerLocation.Range, secondCaller.From.SelectionRange));
        Assert.Single(firstCaller.FromRanges);
        Assert.Single(secondCaller.FromRanges);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCallsIncludesMethodPropertyAndField(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                int {|field:F|};

                int {|property:P|} => F;

                void {|method:M|}()
                {
                }

                void {|caret:N|}()
                {
                    M();
                    var p = P;
                    var f = F;
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var preparedItem = Assert.Single(await RunPrepareCallHierarchyAsync(testLspServer, testLspServer.GetLocations("caret").Single()));
        var outgoingCalls = await RunOutgoingCallsAsync(testLspServer, preparedItem);

        Assert.Equal(3, outgoingCalls.Length);

        var methodCall = Assert.Single(outgoingCalls, static call => call.To.Name == "C.M()");
        var propertyCall = Assert.Single(outgoingCalls, static call => call.To.Name == "C.P");
        var fieldCall = Assert.Single(outgoingCalls, static call => call.To.Name == "C.F");

        Assert.Equal(0, CompareRange(testLspServer.GetLocations("method").Single().Range, methodCall.To.SelectionRange));
        Assert.Equal(0, CompareRange(testLspServer.GetLocations("property").Single().Range, propertyCall.To.SelectionRange));
        Assert.Equal(0, CompareRange(testLspServer.GetLocations("field").Single().Range, fieldCall.To.SelectionRange));

        Assert.Single(methodCall.FromRanges);
        Assert.Single(propertyCall.FromRanges);
        Assert.Single(fieldCall.FromRanges);
    }

    private static async Task<LSP.CallHierarchyItem[]> RunPrepareCallHierarchyAsync(TestLspServer testLspServer, LSP.Location caret)
        => await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start,
            },
            CancellationToken.None) ?? [];

    private static async Task<LSP.CallHierarchyOutgoingCall[]> RunOutgoingCallsAsync(TestLspServer testLspServer, LSP.CallHierarchyItem item)
        => await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>(
            LSP.Methods.CallHierarchyOutgoingCallsName,
            new LSP.CallHierarchyOutgoingCallsParams
            {
                Item = item,
            },
            CancellationToken.None) ?? [];

    private static async Task<LSP.CallHierarchyIncomingCall[]> RunIncomingCallsAsync(TestLspServer testLspServer, LSP.CallHierarchyItem item)
        => await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>(
            LSP.Methods.CallHierarchyIncomingCallsName,
            new LSP.CallHierarchyIncomingCallsParams
            {
                Item = item,
            },
            CancellationToken.None) ?? [];
}
