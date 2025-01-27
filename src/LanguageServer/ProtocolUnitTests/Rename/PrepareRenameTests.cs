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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Rename;

public sealed class PrepareRenameTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestPrepareRenameValidLocationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|caret:|}{|range:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();

        var results = await RunPrepareRenameAsync(testLspServer, CreatePrepareRenameParams(renameLocation));
        Assert.Equal(testLspServer.GetLocations("range").Single().Range, results);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareRenameAfterMethodNameAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|range:M|}{|caret:|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();

        var results = await RunPrepareRenameAsync(testLspServer, CreatePrepareRenameParams(renameLocation));
        Assert.Equal(testLspServer.GetLocations("range").Single().Range, results);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareRenameWithAtSymbolAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void M()
    {
        var {|caret:|}{|range:@foo|} = 1;
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();

        var results = await RunPrepareRenameAsync(testLspServer, CreatePrepareRenameParams(renameLocation));
        Assert.Equal(testLspServer.GetLocations("range").Single().Range, results);
    }

    [Theory, CombinatorialData]
    public async Task TestPrepareRenameInvalidLocationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    // Cannot rename {|caret:|}inside a comment.
    void M()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();

        var results = await RunPrepareRenameAsync(testLspServer, CreatePrepareRenameParams(renameLocation));
        Assert.Null(results);
    }

    private static LSP.PrepareRenameParams CreatePrepareRenameParams(LSP.Location location)
        => new LSP.PrepareRenameParams()
        {
            Position = location.Range.Start,
            TextDocument = CreateTextDocumentIdentifier(location.Uri)
        };

    private static async Task<LSP.Range?> RunPrepareRenameAsync(TestLspServer testLspServer, LSP.PrepareRenameParams prepareRenameParams)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.PrepareRenameParams, LSP.Range?>(LSP.Methods.TextDocumentPrepareRenameName, prepareRenameParams, CancellationToken.None);
    }
}
