// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeLens;

public class CSharpCodeLensTests : AbstractCodeLensTests
{
    public CSharpCodeLensTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestNoReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestOneReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleReferencesAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
        M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 2);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleReferencesCappedAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 100, isCapped: true);
    }

    [Theory, CombinatorialData]
    public async Task TestClassDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class {|codeLens:A|}
{
    void M(A a)
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Theory, CombinatorialData]
    public async Task TestInterfaceDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"interface {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestEnumDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"enum {|codeLens:A|}
{
    One
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestPropertyDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:I|} { get; set; }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestMethodDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestStructDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"struct {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestConstructorDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public {|codeLens:A|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestRecordDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"record {|codeLens:A|}(int SomeInt)";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotShutdownServerIfCacheEntryMissing(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var documentUri = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        var codeLensParams = new LSP.CodeLensParams
        {
            TextDocument = CreateTextDocumentIdentifier(documentUri)
        };

        var actualCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        var firstCodeLens = actualCodeLenses.First();
        var data = JsonConvert.DeserializeObject<CodeLensResolveData>(firstCodeLens.Data!.ToString());
        AssertEx.NotNull(data);
        var firstResultId = data.ResultId;

        // Verify the code lens item is in the cache.
        var cache = testLspServer.GetRequiredLspService<CodeLensCache>();
        Assert.NotNull(cache.GetCachedEntry(firstResultId));

        // Update the document so the syntax version changes
        await testLspServer.OpenDocumentAsync(documentUri);
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "A"));

        // Execute a few more requests to ensure the first request is removed from the cache.
        await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "B"));
        await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);

        await testLspServer.InsertTextAsync(documentUri, (0, 0, "C"));
        var lastCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        Assert.True(lastCodeLenses.Any());

        // Assert that the first result id is no longer in the cache.
        Assert.Null(cache.GetCachedEntry(firstResultId));

        // Assert that the items missing from the cache are still fetched
        var lensMissingFromCache = await testLspServer.ExecuteRequestAsync<LSP.CodeLens, LSP.CodeLens>(LSP.Methods.CodeLensResolveName, firstCodeLens, CancellationToken.None);
        Assert.Equal(string.Format(FeaturesResources._0_references_unquoted, "-"), lensMissingFromCache.Command.Title);

        // Assert that the server did not shutdown and that we can resolve the latest codelens request we made.
        var lastCodeLens = await testLspServer.ExecuteRequestAsync<LSP.CodeLens, LSP.CodeLens>(LSP.Methods.CodeLensResolveName, lastCodeLenses.First(), CancellationToken.None);
        Assert.NotNull(lastCodeLens?.Command);
    }
}
