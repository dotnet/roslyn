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
    public CallHierarchyTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_Method(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void {|caret:|}M()
                {
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        AssertEx.NotNull(result);
        Assert.Single(result);
        Assert.Equal("M()", result[0].Name);
        Assert.Equal(LSP.SymbolKind.Method, result[0].Kind);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_Property(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                int {|caret:|}P { get; set; }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        AssertEx.NotNull(result);
        Assert.Single(result);
        Assert.Contains("P", result[0].Name);
        Assert.Equal(LSP.SymbolKind.Property, result[0].Kind);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareCallHierarchy_NoSymbol(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void M()
                {
                    {|caret:|}// comment
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCalls(bool mutatingLspWorkspace)
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
                    M();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        // First prepare the call hierarchy
        var prepareResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        AssertEx.NotNull(prepareResult);
        Assert.Single(prepareResult);

        // Now get incoming calls
        var incomingCallsResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>(
            LSP.Methods.CallHierarchyIncomingCallsName,
            new LSP.CallHierarchyIncomingCallsParams
            {
                Item = prepareResult[0]
            },
            CancellationToken.None);

        AssertEx.NotNull(incomingCallsResult);
        Assert.Equal(2, incomingCallsResult.Length);

        // Caller1 should have 1 call site
        var caller1 = incomingCallsResult.FirstOrDefault(c => c.From.Name.Contains("Caller1"));
        AssertEx.NotNull(caller1);
        Assert.Single(caller1.FromRanges);

        // Caller2 should have 2 call sites
        var caller2 = incomingCallsResult.FirstOrDefault(c => c.From.Name.Contains("Caller2"));
        AssertEx.NotNull(caller2);
        Assert.Equal(2, caller2.FromRanges.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestOutgoingCalls(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                void {|caret:|}M()
                {
                    Helper1();
                    Helper2();
                    Helper2();
                }

                void Helper1() { }
                void Helper2() { }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        // First prepare the call hierarchy
        var prepareResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        AssertEx.NotNull(prepareResult);
        Assert.Single(prepareResult);

        // Now get outgoing calls
        var outgoingCallsResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>(
            LSP.Methods.CallHierarchyOutgoingCallsName,
            new LSP.CallHierarchyOutgoingCallsParams
            {
                Item = prepareResult[0]
            },
            CancellationToken.None);

        AssertEx.NotNull(outgoingCallsResult);
        Assert.Equal(2, outgoingCallsResult.Length);

        // Helper1 should have 1 call site
        var helper1 = outgoingCallsResult.FirstOrDefault(c => c.To.Name.Contains("Helper1"));
        AssertEx.NotNull(helper1);
        Assert.Single(helper1.FromRanges);

        // Helper2 should have 2 call sites
        var helper2 = outgoingCallsResult.FirstOrDefault(c => c.To.Name.Contains("Helper2"));
        AssertEx.NotNull(helper2);
        Assert.Equal(2, helper2.FromRanges.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestIncomingCalls_Constructor(bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            {
                public {|caret:|}C()
                {
                }

                static void Create()
                {
                    var c = new C();
                }
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var caret = testLspServer.GetLocations("caret").Single();

        // First prepare the call hierarchy
        var prepareResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName,
            new LSP.CallHierarchyPrepareParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.DocumentUri),
                Position = caret.Range.Start
            },
            CancellationToken.None);

        AssertEx.NotNull(prepareResult);
        Assert.Single(prepareResult);
        Assert.Equal("C()", prepareResult[0].Name);

        // Now get incoming calls
        var incomingCallsResult = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>(
            LSP.Methods.CallHierarchyIncomingCallsName,
            new LSP.CallHierarchyIncomingCallsParams
            {
                Item = prepareResult[0]
            },
            CancellationToken.None);

        AssertEx.NotNull(incomingCallsResult);
        Assert.Single(incomingCallsResult);
        Assert.Contains("Create", incomingCallsResult[0].From.Name);
    }
}
