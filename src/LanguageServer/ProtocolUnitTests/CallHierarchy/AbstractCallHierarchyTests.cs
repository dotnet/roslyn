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

public abstract class AbstractCallHierarchyTests : AbstractLanguageServerProtocolTests
{
    protected AbstractCallHierarchyTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    private protected static async Task<LSP.CallHierarchyItem[]?> PrepareCallHierarchyAsync(
        TestLspServer testLspServer,
        LSP.Position position)
    {
        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var request = new LSP.CallHierarchyPrepareParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
            Position = position
        };

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyPrepareParams, LSP.CallHierarchyItem[]?>(
            LSP.Methods.PrepareCallHierarchyName, request, CancellationToken.None);

        return result;
    }

    private protected static async Task<LSP.CallHierarchyIncomingCall[]?> GetIncomingCallsAsync(
        TestLspServer testLspServer,
        LSP.CallHierarchyItem item)
    {
        var request = new LSP.CallHierarchyIncomingCallsParams
        {
            Item = item
        };

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyIncomingCallsParams, LSP.CallHierarchyIncomingCall[]?>(
            LSP.Methods.CallHierarchyIncomingCallsName, request, CancellationToken.None);

        return result;
    }

    private protected static async Task<LSP.CallHierarchyOutgoingCall[]?> GetOutgoingCallsAsync(
        TestLspServer testLspServer,
        LSP.CallHierarchyItem item)
    {
        var request = new LSP.CallHierarchyOutgoingCallsParams
        {
            Item = item
        };

        var result = await testLspServer.ExecuteRequestAsync<LSP.CallHierarchyOutgoingCallsParams, LSP.CallHierarchyOutgoingCall[]?>(
            LSP.Methods.CallHierarchyOutgoingCallsName, request, CancellationToken.None);

        return result;
    }
}
