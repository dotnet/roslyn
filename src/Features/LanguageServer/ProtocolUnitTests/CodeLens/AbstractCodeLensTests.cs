// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeLens;

public abstract class AbstractCodeLensTests : AbstractLanguageServerProtocolTests
{
    protected AbstractCodeLensTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    private protected static async Task<LSP.CodeLens[]?> GetCodeLensAsync(TestLspServer testLspServer)
    {
        var textDocument = CreateTextDocumentIdentifier(testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI());
        var codeLensParams = new LSP.CodeLensParams
        {
            TextDocument = textDocument
        };

        var actualCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        return actualCodeLenses;
    }

    private protected static async Task VerifyCodeLensAsync(TestLspServer testLspServer, int expectedNumberOfReferences, bool isCapped = false)
    {
        var expectedCodeLens = testLspServer.GetLocations("codeLens").Single();

        var actualCodeLenses = await GetCodeLensAsync(testLspServer);
        AssertEx.NotNull(actualCodeLenses);
        Assert.NotEmpty(actualCodeLenses);

        var matchingCodeLenses = actualCodeLenses.Where(actualCodeLens => actualCodeLens.Range == expectedCodeLens.Range);
        Assert.Single(matchingCodeLenses);

        var matchingCodeLens = matchingCodeLenses.Single();
        Assert.Null(matchingCodeLens.Command);

        var resolvedCodeLens = await testLspServer.ExecuteRequestAsync<LSP.CodeLens, LSP.CodeLens>(LSP.Methods.CodeLensResolveName, matchingCodeLens, CancellationToken.None);
        AssertEx.NotNull(resolvedCodeLens?.Command);

        var expectedReferenceCountString = isCapped ? "99+" : expectedNumberOfReferences.ToString();
        Assert.True(resolvedCodeLens.Command.Title.StartsWith(expectedReferenceCountString));
    }

    private protected static async Task VerifyTestCodeLensAsync(TestLspServer testLspServer, params string[] commandTitles)
    {
        var expectedCodeLens = testLspServer.GetLocations("codeLens").Single();

        var textDocument = CreateTextDocumentIdentifier(testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI());
        var codeLensParams = new LSP.CodeLensParams
        {
            TextDocument = textDocument
        };

        var actualCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        AssertEx.NotNull(actualCodeLenses);
        Assert.NotEmpty(actualCodeLenses);

        var matchingCodeLenses = actualCodeLenses
            .Where(actualCodeLens => actualCodeLens.Range == expectedCodeLens.Range)
            .Where(actualCodeLens => actualCodeLens.Command != null && actualCodeLens.Command.CommandIdentifier == CodeLensHandler.RunTestsCommandIdentifier);
        foreach (var title in commandTitles)
        {
            Assert.Single(matchingCodeLenses, (c) => c.Command!.Title == title);
        }
    }

    private protected static async Task VerifyTestCodeLensMissingAsync(TestLspServer testLspServer)
    {
        var expectedCodeLens = testLspServer.GetLocations("codeLens").Single();

        var textDocument = CreateTextDocumentIdentifier(testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI());
        var codeLensParams = new LSP.CodeLensParams
        {
            TextDocument = textDocument
        };

        var actualCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParams, CancellationToken.None);
        AssertEx.NotNull(actualCodeLenses);
        Assert.NotEmpty(actualCodeLenses);
        Assert.All(actualCodeLenses, actualCodeLens => Assert.NotEqual(CodeLensHandler.RunTestsCommandIdentifier, actualCodeLens.Command?.CommandIdentifier));
    }
}
